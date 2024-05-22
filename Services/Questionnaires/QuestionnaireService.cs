using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Domain.Models.Questionnaires;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.QuestionnaireResults;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Enums.Appointments;
using AutoMapper;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Models.Patient;

namespace WildHealth.Application.Services.Questionnaires
{
    /// <summary>
    /// <see cref="IQuestionnairesService"/>
    /// </summary>
    public class QuestionnairesService : IQuestionnairesService
    {
        private readonly IGeneralRepository<Questionnaire> _questionnairesRepository;
        private readonly IGeneralRepository<QuestionnaireResult> _resultsRepository;
        private readonly IGeneralRepository<Patient> _patientRepository;
        private readonly IGeneralRepository<BannerMessage> _bannerMessage;
        private readonly IGeneralRepository<Appointment> _appointmentRepository;
        private readonly IQuestionnaireResultsService _questionnaireResultsService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IMapper _mapper;
        private readonly IDateTimeProvider _dateTimeProvider;

        private readonly Dictionary<QuestionnaireType, string> _featureFlags = new()
        {
            {QuestionnaireType.FollowUpCallForms, "WH-ALL-FollowUpForms"}
        };

        public QuestionnairesService(
            IGeneralRepository<Questionnaire> questionnairesRepository,
            IGeneralRepository<QuestionnaireResult> resultsRepository,
            IGeneralRepository<Patient> patientRepository,
            IGeneralRepository<BannerMessage> bannerMessage,
            IQuestionnaireResultsService questionnaireResultsService,
            IFeatureFlagsService featureFlagsService,
            IMapper mapper, 
            IGeneralRepository<Appointment> appointmentRepository, IDateTimeProvider dateTimeProvider)
        {
            _questionnairesRepository = questionnairesRepository;
            _resultsRepository = resultsRepository;
            _patientRepository = patientRepository;
            _bannerMessage = bannerMessage;
            _questionnaireResultsService = questionnaireResultsService;
            _mapper = mapper;
            _appointmentRepository = appointmentRepository;
            _dateTimeProvider = dateTimeProvider;
            _featureFlagsService = featureFlagsService;
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.GetRemindedAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Questionnaire>> GetRemindedAsync()
        {
            return await _questionnairesRepository
                .All()
                .IncludeScheduler()
                .Reminded()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.IsAvailableAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<bool> IsAvailableAsync(int id, int patientId)
        {
            var availableQuestionnaires = await GetAvailableAsync(patientId);
            return availableQuestionnaires.Any(x => x.Questionnaire.Id == id);
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.GetNewAppointmentQuestionnairesAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<AppointmentQuestionnaire>> GetNewAppointmentQuestionnairesAsync()
        {
            var questionnaires = await _questionnairesRepository
                .All()
                .Active()
                .IncludeScheduler()
                .Where(x => x.Scheduler.DaysBeforeAppointment.HasValue)
                .ToArrayAsync();

            var date = _dateTimeProvider.UtcNow();
            const int queryPeriodMinutes = 30;
            var newQuestionnaires = new List<AppointmentQuestionnaire>();
            var activeQuestionnaires = ApplyFeatureFlagFilter(questionnaires);
            foreach (var questionnaire in activeQuestionnaires)
            {
                
                var scheduler = questionnaire.Scheduler;
                var appointments = await _appointmentRepository
                    .All()
                    .IncludePatientWithQuestionnaireResults()
                    .Where(x => scheduler.AppointmentPurposes.Contains(x.Purpose)
                                && x.Status == AppointmentStatus.Submitted
                                && x.StartDate <= date.AddDays(scheduler.DaysBeforeAppointment!.Value)
                                && x.StartDate >= date.AddDays(scheduler.DaysBeforeAppointment.Value).AddMinutes(-queryPeriodMinutes))
                    .ToListAsync();

                foreach (var appointment in appointments)
                {
                    newQuestionnaires.Add(new AppointmentQuestionnaire(questionnaire, appointment));
                }
            }
            return newQuestionnaires;
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.GetAvailableAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AppointmentQuestionnaire>> GetAvailableAsync(int patientId)
        {
            var patient = await _patientRepository
                .All()
                .ById(patientId)
                .IncludePaymentPrice()
                .AsNoTracking()
                .FirstAsync();

            var patientDomain = PatientDomain.Create(patient);
            
            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist.", exceptionParam);
            }

            var questionnaires = await _questionnairesRepository
                .All()
                .Active()
                .IncludeQuestions()
                .IncludeScheduler()
                .AsNoTracking()
                .ToArrayAsync();

            var results = await _resultsRepository
                .All()
                .RelatedToPatient(patientId)
                .Include(x => x.Appointment)
                .AsNoTracking()
                .ToListAsync();

            var date = _dateTimeProvider.UtcNow();

            var availableQuestionnaires = new List<AppointmentQuestionnaire>();

            foreach(var questionnaire in questionnaires)
            {
                var correspondingResults = results
                    .Where(x => x.QuestionnaireId == questionnaire.GetId())
                    .ToList();
                
                if (questionnaire.Type == QuestionnaireType.HealthForms)
                {
                    continue;
                }

                if (correspondingResults.Any(x => !QuestionnaireResultDomain.Create(x).IsSubmitted()))
                {
                    continue;
                }

                var scheduler = questionnaire.Scheduler;

                var questionnaireSchedulerDomain = QuestionnaireSchedulerDomain.Create(scheduler);

                var resultsCount = correspondingResults.Count;

                if (!questionnaireSchedulerDomain.IsUnlimited && resultsCount >= scheduler.MaxCount)
                {
                    continue;
                }

                if (scheduler.DaysBeforeAppointment.HasValue)
                {
                    var appointments = await _appointmentRepository
                        .All()
                        .RelatedToPatient(patientId)
                        .Where(x => scheduler.AppointmentPurposes.Contains(x.Purpose)
                                    && scheduler.AppointmentWithTypes.Contains(x.WithType)
                                    && x.Status == AppointmentStatus.Submitted
                                    && x.StartDate >= date
                                    && date.AddDays(scheduler.DaysBeforeAppointment.Value) >= x.StartDate)
                        .ToListAsync();

                    foreach (var appointment in appointments)
                    {
                        if (correspondingResults.Any(x => x.AppointmentId.HasValue 
                                                          && (x.AppointmentId == appointment.GetId()
                                                          || x.Appointment.Purpose == appointment.Purpose &&
                                                          x.Appointment.WithType == appointment.WithType &&
                                                          x.SubmittedAt.HasValue && 
                                                          x.SubmittedAt.Value.AddDays(scheduler.DaysBeforeAppointment.Value) > date)))
                        {
                            continue;
                        }
                        availableQuestionnaires.Add(
                            new AppointmentQuestionnaire(questionnaire, appointment));
                    }
                    continue;
                }

                var signUpDate = patientDomain.GetSignUpDate() ?? _dateTimeProvider.UtcNow();

                if (resultsCount == 0)
                {
                    if (date.AddDays(-scheduler.DaysAfterSignUp).Date >= signUpDate.Date)
                    {
                        if (questionnaireSchedulerDomain.IsCountable)
                        {
                            questionnaire.Name = $"{questionnaire.Name} 1";
                        }
                        availableQuestionnaires.Add(
                            new AppointmentQuestionnaire(questionnaire));
                    }
                    continue;
                }

                var daysAfterSignUp = scheduler.DaysAfterSignUp + (resultsCount * (scheduler.IntervalInDays ?? 0));
                if (date.AddDays(-daysAfterSignUp).Date >= signUpDate.Date)
                {
                    questionnaire.Description =
                        $"This should be completed approximately {daysAfterSignUp.ToString()} days from sign up.";

                    if (questionnaireSchedulerDomain.IsCountable)
                    {
                        questionnaire.Name = $"{questionnaire.Name} {resultsCount + 1}";
                    }

                    correspondingResults.Sort((x, y) => Nullable.Compare(x.SequenceNumber, y.SequenceNumber));

                    var lastResultSequenceNumber = correspondingResults.LastOrDefault()?.SequenceNumber;

                    if (questionnaireSchedulerDomain.IsCountable && lastResultSequenceNumber < resultsCount + 1)
                    {
                        availableQuestionnaires.Add(
                            new AppointmentQuestionnaire(questionnaire));
                    }
                }
            }
            
            return ApplyFeatureFlagFilter(availableQuestionnaires);
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.GetByTypeAsync"/>
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Questionnaire> GetByTypeAsync(QuestionnaireType type)
        {
            var questionnaire = await _questionnairesRepository
                .All()
                .Active()
                .ByType(type)
                .IncludeScheduler()
                .IncludeQuestions()
                .FirstOrDefaultAsync();

            if (questionnaire is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Health Questionnaire with type {type} does not exist ");
            }

            return questionnaire;
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.GetBySubTypeAsync"/>
        /// </summary>
        /// <param name="subType"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Questionnaire> GetBySubTypeAsync(QuestionnaireSubType subType)
        {
            var questionnaire = await _questionnairesRepository
                .All()
                .Active()
                .BySubType(subType)
                .IncludeScheduler()
                .IncludeQuestions()
                .FirstOrDefaultAsync();

            if (questionnaire is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Health Questionnaire with sub type {subType} does not exist ");
            }

            return questionnaire;
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Questionnaire> GetByIdAsync(int id)
        {
            var questionnaire = await _questionnairesRepository
                .All()
                .ById(id)
                .IncludeScheduler()
                .IncludeQuestions()
                .FirstOrDefaultAsync();

            if (questionnaire is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Health Questionnaire does not exist");
            }

            return questionnaire;
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.GetAvailableHealthFormsAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<HealthFormsModel> GetAvailableHealthFormsAsync(int patientId)
        {
            var questionnaires = await _questionnairesRepository
                .All()
                .Active()
                .ByType(QuestionnaireType.HealthForms)
                .AsNoTracking()
                .ToArrayAsync();

            var allResults = await _resultsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeQuestionnaire()
                .AsNoTracking()
                .ToArrayAsync();
            
            var healthFormsResult = new List<HealthFormModel>();
            var healthFormsStatuses = new Dictionary<QuestionnaireSubType, QuestionnaireStatusType>();

            foreach (var questionnaire in questionnaires)
            {
                var results = allResults
                    .Where(x => x.QuestionnaireId == questionnaire.Id)
                    .ToArray();
                
                var questionnaireStatus = GetStatusQuestionnaireByPatientResults(questionnaire, results);

                if (questionnaireStatus != QuestionnaireStatusType.None)
                {
                    healthFormsStatuses.Add(questionnaire.SubType, questionnaireStatus);
                }
                
                var lastActiveResult = results
                    .Where(x => !QuestionnaireResultDomain.Create(x).IsSubmitted())
                    .MaxBy(x => x.CreatedAt);

                var lastSubmittedResult = results
                    .Where(x => QuestionnaireResultDomain.Create(x).IsSubmitted())
                    .MaxBy(x => x.SubmittedAt);

                var healthFormsModel = _mapper.Map<HealthFormModel>(questionnaire);

                healthFormsModel.QuestionnaireStatusType = questionnaireStatus;

                if (questionnaireStatus == QuestionnaireStatusType.Completed)
                {
                    healthFormsModel.ResultId = lastSubmittedResult?.GetId();
                }

                if (questionnaireStatus == QuestionnaireStatusType.InCompleted)
                {
                    healthFormsModel.ResultId = lastActiveResult?.GetId();
                }

                healthFormsResult.Add(healthFormsModel);
            }

            var banner = await GetBannerAsync(healthFormsStatuses);
            
            return new HealthFormsModel
            {
                Forms = healthFormsResult.ToArray(),
                Banner = _mapper.Map<HealthFormBannerModel>(banner)
            };
        }

        /// <summary>
        /// <see cref="IQuestionnairesService.AnyAvailableAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<bool> AnyAvailableAsync(int patientId)
        {
            var results = await _questionnaireResultsService.GetAllAsync(patientId);

            var completedResults = results.Where(x => x.SubmittedAt != null).ToArray();

            var goals = completedResults.Any(x => x.Questionnaire.Type == QuestionnaireType.HealthForms && x.Questionnaire.SubType == QuestionnaireSubType.GoalsIncomplete);
            var medical = completedResults.Any(x => x.Questionnaire.Type == QuestionnaireType.HealthForms && x.Questionnaire.SubType == QuestionnaireSubType.MedicalHistoryIncomplete);
            var detailed = completedResults.Any(x => x.Questionnaire.Type == QuestionnaireType.HealthForms && x.Questionnaire.SubType == QuestionnaireSubType.DetailedHistoryIncomplete);

            var availableHealthLog = await GetAvailableAsync(patientId);

            var healthLog = availableHealthLog.All(x => x.Questionnaire.Type != QuestionnaireType.HealthLog);

            return !(healthLog && goals && medical && detailed);
        }

        #region private

        private IEnumerable<AppointmentQuestionnaire> ApplyFeatureFlagFilter(IEnumerable<AppointmentQuestionnaire> questionnaires)
        {
            var excludedTypes = _featureFlags
                .Select(featureFlag =>
                    new {featureFlag, isFlag = _featureFlagsService.GetFeatureFlag(featureFlag.Value)})
                .Where(x => !x.isFlag)
                .Select(x => x.featureFlag.Key);

            return questionnaires.Where(x => !excludedTypes.Contains(x.Questionnaire.Type)).ToArray();
        }
        
        private IEnumerable<Questionnaire> ApplyFeatureFlagFilter(IEnumerable<Questionnaire> questionnaires)
        {
            var excludedTypes = _featureFlags
                .Select(featureFlag =>
                    new {featureFlag, isFlag = _featureFlagsService.GetFeatureFlag(featureFlag.Value)})
                .Where(x => !x.isFlag)
                .Select(x => x.featureFlag.Key);

            return questionnaires.Where(x => !excludedTypes.Contains(x.Type));
        }
        
        private async Task<BannerMessage?> GetBannerAsync(IDictionary<QuestionnaireSubType, QuestionnaireStatusType> statuses)
        {
            var bannerMessages = await _bannerMessage
                .All()
                .AsNoTracking()
                .ToArrayAsync();

            var resultBannerType = QuestionnaireSubType.None;

            if (statuses[QuestionnaireSubType.GoalsIncomplete].Equals(QuestionnaireStatusType.New) 
                || statuses[QuestionnaireSubType.GoalsIncomplete].Equals(QuestionnaireStatusType.InCompleted))
            {
                resultBannerType = QuestionnaireSubType.GoalsIncomplete;
            }
            else if (statuses[QuestionnaireSubType.GoalsIncomplete].Equals(QuestionnaireStatusType.Completed) 
                     && (statuses[QuestionnaireSubType.MedicalHistoryIncomplete].Equals(QuestionnaireStatusType.New) 
                         || statuses[QuestionnaireSubType.MedicalHistoryIncomplete].Equals(QuestionnaireStatusType.InCompleted)))
            {
                resultBannerType = QuestionnaireSubType.MedicalHistoryIncomplete;
            }
            // TODO: UNCOMMENT WHEN DETAILED QUESTIONNAIRE WILL BE READY
            // else if (statuses[QuestionnaireSubType.GoalsIncomplete].Equals(QuestionnaireStatusType.Completed) 
            //     && statuses[QuestionnaireSubType.MedicalHistoryIncomplete].Equals(QuestionnaireStatusType.Completed)
            //     && (statuses[QuestionnaireSubType.DetailedHistoryIncomplete].Equals(QuestionnaireStatusType.New) 
            //     || statuses[QuestionnaireSubType.DetailedHistoryIncomplete].Equals(QuestionnaireStatusType.InCompleted)))
            // {
            //     // resultBannerType = QuestionnaireSubType.DetailedHistoryIncomplete;
            // }

            return statuses.Count(x => x.Value == QuestionnaireStatusType.None) == 3 
                ? bannerMessages.FirstOrDefault(x => x.Type == QuestionnaireSubType.GoalsIncomplete) 
                : bannerMessages.FirstOrDefault(x=> x.Type.Equals(resultBannerType));
        }
        
        /// <summary>
        /// Returns status of questionnaire by questionnaire result
        /// </summary>
        /// <param name="questionnaire"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        
        private QuestionnaireStatusType GetStatusQuestionnaireByPatientResults(Questionnaire questionnaire, QuestionnaireResult[] results)
        {
            if (questionnaire.SubType.Equals(QuestionnaireSubType.None))
            {
                return QuestionnaireStatusType.None;
            }

            var correspondingResult = results
                .Where(x => x.Questionnaire.Type == questionnaire.Type && x.Questionnaire.SubType == questionnaire.SubType)
                .ToArray();
            
            if (!correspondingResult.Any())
            {
                return QuestionnaireStatusType.New;
            }

            return correspondingResult.All(x => QuestionnaireResultDomain.Create(x).IsSubmitted())
                ? QuestionnaireStatusType.Completed
                : QuestionnaireStatusType.InCompleted;
        }

        #endregion
    }
}
