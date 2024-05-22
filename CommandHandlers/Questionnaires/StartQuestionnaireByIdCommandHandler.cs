using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Questionnaires;
using WildHealth.Application.Events.Questionnaires;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.QuestionnaireResults;
using WildHealth.Application.Services.Questionnaires;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Models.Questionnaires;
using MediatR;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Questionnaires
{
    public class StartQuestionnaireByIdCommandHandler : IRequestHandler<StartQuestionnaireByIdCommand, QuestionnaireResult>
    {
        private readonly IPatientsService _patientsService;
        private readonly IQuestionnairesService _questionnairesService;
        private readonly IQuestionnaireResultsService _questionnaireResultsService;
        private readonly IMediator _mediator;

        public StartQuestionnaireByIdCommandHandler(
            IPatientsService patientsService, 
            IQuestionnairesService questionnairesService, 
            IQuestionnaireResultsService questionnaireResultsService,
            IMediator mediator)
        {
            _patientsService = patientsService;
            _questionnairesService = questionnairesService;
            _questionnaireResultsService = questionnaireResultsService;
            _mediator = mediator;
        }

        public async Task<QuestionnaireResult> Handle(StartQuestionnaireByIdCommand command, CancellationToken cancellationToken)
        {
            var existingResults = await FetchExistingResultsAsync(command.PatientId);
            
            var existingResult = TryGetExistingResult(existingResults, command.QuestionnaireId);
            
            if (existingResult is not null)
            {
                return existingResult;
            }

            var questionnaire = await _questionnairesService.GetByIdAsync(command.QuestionnaireId);

            await AssertQuestionnaireAvailableAsync(questionnaire, command.PatientId, existingResults);
            
            var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientForSendingIntegrationEventSpecification);

            var sequenceNumber = GetSequenceNumber(existingResults, questionnaire);

            var result = await _questionnaireResultsService.StartAsync(
                questionnaireId: questionnaire.GetId(),
                patientId: patient.GetId(),
                sequenceNumber: sequenceNumber,
                appointmentId: command.AppointmentId
            );

            var questionnaireStartedEvent = new QuestionnaireStartedEvent(result, patient);

            await _mediator.Publish(questionnaireStartedEvent , cancellationToken);
            
            return result;
        }
        
        #region private

        /// <summary>
        /// Fetches and returns existing questionnaire results
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        private async Task<QuestionnaireResult[]> FetchExistingResultsAsync(int patientId)
        {
            var existingResults = await _questionnaireResultsService.GetAllAsync(patientId);

            return existingResults.ToArray();
        }
        
        /// <summary>
        /// Try fetch and returns existing not submitted questionnaire result
        /// </summary>
        /// <param name="existingResults"></param>
        /// <param name="questionnaireId"></param>
        /// <returns></returns>
        private QuestionnaireResult? TryGetExistingResult(IEnumerable<QuestionnaireResult> existingResults, int questionnaireId)
        {
            return existingResults.FirstOrDefault(x => x.QuestionnaireId == questionnaireId && !QuestionnaireResultDomain.Create(x).IsSubmitted());
        }

        /// <summary>
        /// Returns Sequence Number
        /// </summary>
        /// <param name="existingResults"></param>
        /// <param name="questionnaire"></param>
        /// <returns></returns>
        private int? GetSequenceNumber(IEnumerable<QuestionnaireResult> existingResults, Questionnaire questionnaire)
        {
            var questionnaireSchedulerDomain = QuestionnaireSchedulerDomain.Create(questionnaire.Scheduler);

            return questionnaireSchedulerDomain.IsCountable
                ? existingResults.Count(x => x.QuestionnaireId == questionnaire.GetId()) + 1
                : new int?();
        }

        /// <summary>
        /// Asserts questionnaire is available
        /// </summary>
        /// <param name="questionnaire"></param>
        /// <param name="patientId"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task AssertQuestionnaireAvailableAsync(Questionnaire questionnaire, int patientId, QuestionnaireResult[] results)
        {
            var isAvailable = await _questionnairesService.IsAvailableAsync(
                id: questionnaire.GetId(),
                patientId: patientId);

            if (questionnaire.Type == QuestionnaireType.HealthForms)
            {
                return;
            }

            if (!isAvailable)
            {
                if (results.Any(x => x.QuestionnaireId == questionnaire.GetId()))
                {
                    throw new AppException(HttpStatusCode.BadRequest, "Questionnaire has been completed already.");
                }
                
                throw new AppException(HttpStatusCode.BadRequest, "Questionnaire is not available.");
            }
        }
        
        #endregion
    }
}