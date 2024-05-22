using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Alerts;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.QuestionnaireResults;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using MediatR;
using WildHealth.Application.CommandHandlers.Alerts.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Utils.DateTimes;

namespace WildHealth.Application.CommandHandlers.Alerts;

public class ParseAlertsFromQuestionnaireCommandHandler : IRequestHandler<ParseAlertsFromQuestionnaireCommand, Unit>
{
    private readonly IPatientsService _patientsService;
    private readonly IQuestionnaireResultsService _questionnaireResultsService;
    private readonly IFeatureFlagsService _featureFlagsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly MaterializeFlow _materializeFlow;
    private readonly IPatientProfileService _patientProfileService;

    public ParseAlertsFromQuestionnaireCommandHandler(
        IPatientsService patientsService, 
        IQuestionnaireResultsService questionnaireResultsService, 
        IFeatureFlagsService featureFlagsService, 
        IDateTimeProvider dateTimeProvider, 
        MaterializeFlow materializeFlow, 
        IPatientProfileService patientProfileService)
    {
        _patientsService = patientsService;
        _questionnaireResultsService = questionnaireResultsService;
        _featureFlagsService = featureFlagsService;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
        _patientProfileService = patientProfileService;
    }

    public async Task<Unit> Handle(ParseAlertsFromQuestionnaireCommand command, CancellationToken cancellationToken)
    {
        if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.Phq2))
        {
            return Unit.Value;
        }
        
        var specification = PatientSpecifications.WithEmployees;

        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);

        var questionnaireResult = await _questionnaireResultsService.GetAsync(id: command.QuestionnaireResultId, patientId: command.PatientId);

        var patientUrl = await _patientProfileService.GetProfileLink(command.PatientId, patient.User.PracticeId);
        
        var flow = new ParseAlertsFromQuestionnaireFlow(
            patient: patient,
            result: questionnaireResult,
            patientUrl: patientUrl,
            utcNow: _dateTimeProvider.UtcNow()
        );

        await flow.Materialize(_materializeFlow);
        
        return Unit.Value;
    }
}