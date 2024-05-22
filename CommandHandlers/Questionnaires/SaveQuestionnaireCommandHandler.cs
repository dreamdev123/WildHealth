using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Questionnaires;
using WildHealth.Application.Events.Questionnaires;
using WildHealth.Application.Extensions.Questionnaire;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.QuestionnaireParser;
using WildHealth.Application.Services.QuestionnaireResults;
using WildHealth.Application.Services.Vitals;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Common.Models.Vitals;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Domain.Models.Questionnaires;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Application.Commands.Alerts;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Commands.PharmacyInfo;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.Vitals;
using WildHealth.Shared.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Questionnaires
{
    public class SaveQuestionnaireCommandHandler : IRequestHandler<SaveQuestionnaireCommand, QuestionnaireResult>
    {
        private readonly IPatientsService _patientsService;
        private readonly IQuestionnaireResultsService _questionnaireResultsService;
        private readonly IInputsService _inputsService;
        private readonly IVitalService _vitalsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IQuestionnaireParser _questionnaireParser;
        private readonly IMediator _mediator;
        private readonly ILogger<SaveQuestionnaireCommandHandler> _logger;

        public SaveQuestionnaireCommandHandler(
            IPatientsService patientsService,
            IQuestionnaireResultsService questionnaireResultsService,
            IInputsService inputsService,
            IVitalService vitalsService,
            ITransactionManager transactionManager,
            IQuestionnaireParser questionnaireParser,
            IMediator mediator,
            ILogger<SaveQuestionnaireCommandHandler> logger)
        {
            _patientsService = patientsService;
            _questionnaireResultsService = questionnaireResultsService;
            _inputsService = inputsService;
            _vitalsService = vitalsService;
            _transactionManager = transactionManager;
            _questionnaireParser = questionnaireParser;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<QuestionnaireResult> Handle(SaveQuestionnaireCommand command, CancellationToken cancellationToken)
        {
            var patient = await GetPatientAsync(command.PatientIntakeId, command.PatientId);

            if (patient is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Patient is not found");
            }

            var result = await _questionnaireResultsService.GetAsync(command.QuestionnaireResultId, patient.GetId());

            AssertResultSubmission(result);

            await using var transaction = _transactionManager.BeginTransaction();

            var questionnaireResultDomain = QuestionnaireResultDomain.Create(result);
            try
            {
                var alreadySent = questionnaireResultDomain.IsSubmitted();
                await UpdateResultsAsync(result, command.Answers);

                await UpdateSubmittedStatusAsync(result, command.SubmittedAt);

                if (questionnaireResultDomain.IsSubmitted())
                {
                    var specification = PatientSpecifications.PatientForSendingIntegrationEventSpecification;
                    
                    patient = await _patientsService.GetByIdAsync(patient.GetId(), specification);
                    
                    await UpdateGeneralInputsAsync(patient.GetId(), result);
                    
                    await UpdateHealthSummaryAsync(patient, result);

                    await SavePharmacyInfo(patient, result);
                    
                    await SaveAlertsAsync(patient, result);
                    
                    await RemoveUnfinishedQuestionnaires(patient.GetId(), result, result.Questionnaire.Type);

                    await PublishQuestionnaireCompletedEvent(result, patient, cancellationToken, alreadySent);
                    
                    //  If you add something, please update the vitals last so as to prevent an ORM error.  
                    //  See:
                    //  https://wildhealth.atlassian.net/browse/CLAR-7483
                    //  https://wildhealth.atlassian.net/browse/CLAR-7490
                    //  The proper fix is a refactor of Vital and VitalValue keys, but in the
                    //  meantime, this is a fine way to handle the situation.
                    await UpdateVitalsAsync(patient.GetId(), result);
                }

                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch(Exception e)
            {
                _logger.LogError($"Saving health questionnaire for patient {patient.GetId()} was failed. with [Error]: {e.ToString()}");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private void AssertResultSubmission(QuestionnaireResult result)
        {
            var questionnaireResultDomain = QuestionnaireResultDomain.Create(result);
            if (questionnaireResultDomain.IsSubmitted() && result.Questionnaire.Type != QuestionnaireType.HealthForms)
            {
                throw new AppException(HttpStatusCode.BadRequest, "This questionnaire is already submitted");
            }
        }

        private async Task RemoveUnfinishedQuestionnaires(int patientId, QuestionnaireResult currentQuestionnaireResult, QuestionnaireType questionnaireType)
        {
            if (questionnaireType == QuestionnaireType.HealthForms)
            {
                return;
            }

            bool Predicate(QuestionnaireResult x) { return x.QuestionnaireId == currentQuestionnaireResult.QuestionnaireId && !QuestionnaireResultDomain.Create(x).IsSubmitted() && x.CreatedAt <= currentQuestionnaireResult.CreatedAt && x.Id != currentQuestionnaireResult.Id; }

            var questionnaireResults = (await _questionnaireResultsService.GetAllAsync(patientId)).Where(Predicate);

            foreach (var questionnaireResult in questionnaireResults)
            {
                await _questionnaireResultsService.RemoveAsync(questionnaireResult);
            }
        }

        private async Task UpdateSubmittedStatusAsync(QuestionnaireResult result, DateTime? submittedAt)
        {
            if (submittedAt.HasValue)
            {
                await _questionnaireResultsService.SubmitAsync(result, submittedAt.Value);
            }
        }

        private Task SavePharmacyInfo(Patient patient, QuestionnaireResult result)
        {
            var command = new ParsePharmacyInfoFromQuestionnaireCommand(
                patientId: patient.GetId(),
                questionnaireResultId: result.GetId()
            );

            return _mediator.Send(command);
        }

        private Task SaveAlertsAsync(Patient patient, QuestionnaireResult result)
        {
            var command = new ParseAlertsFromQuestionnaireCommand(
                patientId: patient.GetId(),
                questionnaireResultId: result.GetId()
            );

            return _mediator.Send(command);
        }

        private async Task UpdateResultsAsync(QuestionnaireResult results, IEnumerable<AnswerModel> answers)
        {
            await _questionnaireResultsService.SaveAnswersAsync(results, answers);
        }

        private async Task UpdateVitalsAsync(int patientId, QuestionnaireResult results)
        {
            var date = DateTime.UtcNow;

            try
            {
                await _vitalsService.AssertDateAsync(patientId, date);
            }
            catch (AppException e) when(e.StatusCode == HttpStatusCode.NotAcceptable)
            {
                return;
            }
            
            var vitalsValueSource = results.Questionnaire.Type switch
            {
                QuestionnaireType.HealthForms => VitalValueSourceType.HealthForms,
                QuestionnaireType.FollowUpCallForms => VitalValueSourceType.FollowUpCoachingForm,
                _ => VitalValueSourceType.None
            };
            
            if (results.Questionnaire.Type == QuestionnaireType.HealthForms)
            {
                vitalsValueSource = VitalValueSourceType.HealthForms;
            }

            if (results.Questionnaire.Type == QuestionnaireType.FollowUpCallForms)
            {
                vitalsValueSource = VitalValueSourceType.FollowUpCoachingForm;
            }

            var weightValue = results.Answers.FirstOrDefault(x => x.Key == QuestionKey.Weight)?.Value;

            var heightValue = results.Answers.FirstOrDefault(x => x.Key == QuestionKey.Height)?.Value;
            
            var waistValue = results.Answers.FirstOrDefault(x => x.Key == QuestionKey.Waist)?.Value;

            var systolicBloodPressureValue = results.Answers.FirstOrDefault(x => x.Key == QuestionKey.SystolicBloodPressure)?.Value;

            var diastolicBloodPressureValue = results.Answers.FirstOrDefault(x => x.Key == QuestionKey.DiastolicBloodPressure)?.Value;

            var vitals = new List<CreateVitalModel>();
            
            if (decimal.TryParse(weightValue, out var weight))
            {
                vitals.Add(new CreateVitalModel
                {
                    Name = VitalNames.Weight.Name,
                    Value = weight,
                    DateTime = date,
                    SourceType = vitalsValueSource
                });
            }
            
            if (decimal.TryParse(heightValue, out var height))
            {
                vitals.Add(new CreateVitalModel
                {
                    Name = VitalNames.Height.Name,
                    Value = height,
                    DateTime = date,
                    SourceType = vitalsValueSource
                });
            }

            if (decimal.TryParse(waistValue, out var waist))
            {
                vitals.Add(new CreateVitalModel
                {
                    Name = VitalNames.Waist.Name,
                    Value = waist,
                    DateTime = date,
                    SourceType = vitalsValueSource
                });
            }

            if (decimal.TryParse(systolicBloodPressureValue, out var systolicBloodPressure))
            {
                vitals.Add(new CreateVitalModel
                {
                    Name = VitalNames.SystolicBloodPressure.Name,
                    Value = systolicBloodPressure,
                    DateTime = date,
                    SourceType = vitalsValueSource
                });
            }

            if (decimal.TryParse(diastolicBloodPressureValue, out var diastolicBloodPressure))
            {
                vitals.Add(new CreateVitalModel
                {
                    Name = VitalNames.DiastolicBloodPressure.Name,
                    Value = diastolicBloodPressure,
                    DateTime = date,
                    SourceType = vitalsValueSource
                });
            }

            if (vitals.Any())
            {
                await _vitalsService.CreateAsync(patientId, vitals);
            }         
        }

        private async Task UpdateGeneralInputsAsync(int patientId, QuestionnaireResult results)
        {
            var generalInputs = await _inputsService.GetGeneralInputsAsync(patientId);

            generalInputs = _questionnaireParser.Parse(generalInputs, results);

            await _inputsService.UpdateGeneralInputsAsync(generalInputs, patientId);
        }

        private async Task UpdateHealthSummaryAsync(Patient patient, QuestionnaireResult result)
        {
            var command = new ParseQuestionnaireToHealthSummaryCommand(patient, result);

            await _mediator.Send(command);
        }

        private async Task<Patient> GetPatientAsync(Guid? intakeId, int? id)
        {
            if (id.HasValue)
            {
                return await _patientsService.GetByIdAsync(id.Value, PatientSpecifications.PatientUserSpecification);
            }

            if (intakeId.HasValue)
            {
                return await _patientsService.GetByIntakeIdAsync(intakeId.Value);
            }
            
            throw new AppException(HttpStatusCode.BadRequest, "Cannot find patient");
        }

        private async Task PublishQuestionnaireCompletedEvent(QuestionnaireResult result, Patient patient, CancellationToken cancellationToken, bool alreadySent)
        {
            var questionnaireResultDomain = QuestionnaireResultDomain.Create(result);
            if (questionnaireResultDomain.IsSubmitted())
            {
                if (alreadySent)
                {
                    var questionnaireUpdatedEvent = new QuestionnaireUpdatedEvent(result, patient);

                    await _mediator.Publish(questionnaireUpdatedEvent, cancellationToken);
                }
                else
                {
                    var questionnaireCompletedEvent = new QuestionnaireCompletedEvent(result, patient);

                    await _mediator.Publish(questionnaireCompletedEvent, cancellationToken);
                }
            }
        }
    }
}
