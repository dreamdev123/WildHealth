using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Models.Questionnaires;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;

namespace WildHealth.Application.Services.QuestionnaireResults
{
    public class QuestionnaireResultsService : IQuestionnaireResultsService
    {
        private readonly IGeneralRepository<QuestionnaireResult> _patientQuestionnaireRepository;
        private readonly IGeneralRepository<Questionnaire> _questionnairesRepository;
        private readonly IDateTimeProvider _dateTimeProvider;

        public QuestionnaireResultsService(
            IGeneralRepository<QuestionnaireResult> patientQuestionnaireRepository,
            IGeneralRepository<Questionnaire> questionnairesRepository, 
            IDateTimeProvider dateTimeProvider)
        {
            _patientQuestionnaireRepository = patientQuestionnaireRepository;
            _questionnairesRepository = questionnairesRepository;
            _dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.StartAsync"/>
        /// </summary>
        /// <param name="questionnaireId"></param>
        /// <param name="patientId"></param>
        /// <param name="sequenceNumber"></param>
        /// <param name="appointmentId"></param>
        /// <returns></returns>
        public async Task<QuestionnaireResult> StartAsync(int questionnaireId, int patientId, int? sequenceNumber, int? appointmentId)
        {
            var result = new QuestionnaireResult(
                patientId: patientId, 
                questionnaireId: questionnaireId,
                sequenceNumber: sequenceNumber,
                appointmentId: appointmentId
            );
            
            await _patientQuestionnaireRepository.AddAsync(result);
            
            await _patientQuestionnaireRepository.SaveAsync();

            return result;
        }

        public async Task<IEnumerable<QuestionnaireResult>> GetForPatient(int patientId)
        {
            var results = await _patientQuestionnaireRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeFullQuestionnaire()
                .IncludeAnswers()
                .ToArrayAsync();

            if (!results.Any())
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Questionnaire results for patient do not exist.", exceptionParam);
            }
            
            return results;
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<QuestionnaireResult> GetAsync(int id, int patientId)
        {
            var result = await _patientQuestionnaireRepository
                .All()
                .ById(id)
                .RelatedToPatient(patientId)
                .IncludePatient()
                .IncludeAnswers()
                .FirstAsync();

           await _questionnairesRepository
               .All()
               .ById(result.QuestionnaireId)
               .IncludeQuestions()
               .FirstOrDefaultAsync();
            
            if (result is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Questionnaire results do not exist.", exceptionParam);
            }
            
            return result;
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetMedicalAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<QuestionnaireResult> GetMedicalAsync(int patientId)
        {
            return await this.GetFormType(patientId, QuestionnaireType.HealthForms, QuestionnaireSubType.MedicalHistoryIncomplete);
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetFollowUpFormIdAsync"/>
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns></returns>
        public async Task<int?> GetFollowUpFormIdAsync(int appointmentId)
        {
            return await _patientQuestionnaireRepository
                .All()
                .Where(x => x.AppointmentId == appointmentId)
                .Select(x=> x.Id)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetFollowUpFormsByIdsAsync"/>
        /// </summary>
        /// <param name="appointmentIds"></param>
        /// <returns></returns>
        public async Task<QuestionnaireResult[]> GetFollowUpFormsByIdsAsync(int[] appointmentIds)
        {
            return await _patientQuestionnaireRepository
                .All()
                .ByAppointmentIds(appointmentIds)
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetDetailedAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<QuestionnaireResult> GetDetailedAsync(int patientId)
        {
            return await this.GetFormType(patientId, QuestionnaireType.HealthForms, QuestionnaireSubType.DetailedHistoryIncomplete);
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetGoalsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<QuestionnaireResult> GetGoalsAsync(int patientId)
        {
            return await this.GetFormType(patientId, QuestionnaireType.HealthForms, QuestionnaireSubType.GoalsIncomplete);
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetAllAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<QuestionnaireResult>> GetAllAsync(int patientId)
        {
            var result = await _patientQuestionnaireRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeQuestionnaire()
                .AsNoTracking()
                .ToArrayAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetLatestAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="types"></param>
        /// <returns></returns>
        public async Task<IEnumerable<QuestionnaireResult>> GetLatestAsync(int patientId, QuestionnaireType[] types)
        {
            var result = await _patientQuestionnaireRepository
                .All()
                .RelatedToPatient(patientId)
                .ByQuestionnaireTypes(types)
                .IncludeAnswers()
                .ToArrayAsync();

            var questionnaireIds = result.Select(x => x.QuestionnaireId).Distinct().ToArray();

            await _questionnairesRepository
                .All()
                .ByIds(questionnaireIds)
                .IncludeQuestions()
                .ToListAsync();

            if (!result.Any())
            {
                return Array.Empty<QuestionnaireResult>();
            }
            
            var groups = result.GroupBy(x => x.Questionnaire.Type);
            var latest = groups.Select(x =>
            {
                return x.Any(t => QuestionnaireResultDomain.Create(t).IsSubmitted())
                    ? x
                        .Where(t => QuestionnaireResultDomain.Create(t).IsSubmitted())
                        .OrderBy(t => t.SubmittedAt)
                        .Last()
                    : x.OrderBy(t => t.CreatedAt).Last();
            }).ToArray();
            
            return latest;
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.GetLatestHealthFormsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<QuestionnaireResult>> GetLatestHealthFormsAsync(int patientId)
        {
            var result = await _patientQuestionnaireRepository
                .All()
                .RelatedToPatient(patientId)
                .ByQuestionnaireTypes(new[] { QuestionnaireType.HealthForms} )
                .IncludeAnswers()
                .ToArrayAsync();

            var questionnaireIds = result.Select(x => x.QuestionnaireId).Distinct().ToArray();

            await _questionnairesRepository
                .All()
                .ByIds(questionnaireIds)
                .IncludeQuestions()
                .ToListAsync();

            if (!result.Any())
            {
                return Array.Empty<QuestionnaireResult>();
            }

            var groups = result.GroupBy(x => x.Questionnaire.SubType);
            var latest = groups.Select(x =>
            {
                return x.Any(t => QuestionnaireResultDomain.Create(t).IsSubmitted())
                    ? x
                        .Where(t => QuestionnaireResultDomain.Create(t).IsSubmitted())
                        .OrderBy(t => t.SubmittedAt)
                        .Last()
                    : x.OrderBy(t => t.CreatedAt).Last();
            }).ToArray();

            return latest;
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.SaveAnswersAsync(QuestionnaireResult, IEnumerable{AnswerModel})"/>
        /// </summary>
        /// <param name="questionnaire"></param>
        /// <param name="answers"></param>
        /// <returns></returns>
        public async Task<QuestionnaireResult> SaveAnswersAsync(QuestionnaireResult questionnaire, IEnumerable<AnswerModel> answers)
        {
            foreach (var item in answers)
            {
                var answer = questionnaire.Answers.FirstOrDefault(c => c.Key == item.Key);
                if (answer is null)
                {
                    questionnaire.Answers.Add(new Answer
                    {
                        Key = item.Key,
                        Value = item.Value
                    });

                    continue;
                }

                answer.Value = item.Value;
            }

            await _patientQuestionnaireRepository.SaveAsync();

            return questionnaire;
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.SubmitAsync(QuestionnaireResult, DateTime)"/>
        /// </summary>
        /// <param name="results"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public async Task<QuestionnaireResult> SubmitAsync(QuestionnaireResult results, DateTime dateTime)
        {
            if (results.SubmittedAt.HasValue)
            {
                throw new AppException(HttpStatusCode.BadRequest, "This questionnaire already submitted");
            }

            var questionnaireResultDomain = QuestionnaireResultDomain.Create(results);

            questionnaireResultDomain.Submit(dateTime);

            _patientQuestionnaireRepository.Edit(results);
            await _patientQuestionnaireRepository.SaveAsync();

            return results;
        }

        /// <summary>
        /// <see cref="IQuestionnaireResultsService.RemoveAsync(QuestionnaireResult)"/>
        /// </summary>
        /// <param name="questionnaireResult"></param>
        /// <returns></returns>
        public async Task RemoveAsync(QuestionnaireResult questionnaireResult)
        {
            _patientQuestionnaireRepository.Delete(questionnaireResult);
            
            await _patientQuestionnaireRepository.SaveAsync();
        }

        public async Task RemoveExpiredAsync()
        {
            var expiredQuestionnaireResults = await _patientQuestionnaireRepository
                .All()
                .Where(x => !x.SubmittedAt.HasValue
                    && x.AppointmentId != null
                    && x.Appointment.StartDate <= _dateTimeProvider.UtcNow())
                .ToListAsync();
            
            _patientQuestionnaireRepository.Delete(expiredQuestionnaireResults);
            await _patientQuestionnaireRepository.SaveAsync();
        }


        private async Task<QuestionnaireResult> GetFormType(int patientId, QuestionnaireType questionnaireType, QuestionnaireSubType questionnaireSubType)
        {
            var result = await _patientQuestionnaireRepository
                .All()
                .RelatedToPatient(patientId)
                .ByQuestionnaireTypes(new List<QuestionnaireType> { questionnaireType }.ToArray())
                .ByQuestionnaireSubTypes(new List<QuestionnaireSubType> { questionnaireSubType }.ToArray())
                .OrderByDescending(qr => qr.SubmittedAt).ThenByDescending(qr => qr.CreatedAt)
                .IncludeAnswers()
                .FirstOrDefaultAsync();

            if (result is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Questionnaire results of type: {questionnaireType.ToString()} and subType: {questionnaireSubType.ToString()} does not exist.");
            }
            
            return result;
        }
    }
}
