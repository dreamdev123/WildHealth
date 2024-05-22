using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.PharmacyInfo;
using WildHealth.Application.Services.PatientPharmacyInfos;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.QuestionnaireResults;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;
using WildHealth.Application.CommandHandlers.PharmacyInfo.Flows;

namespace WildHealth.Application.CommandHandlers.PharmacyInfo;

public class ParsePharmacyInfoFromQuestionnaireCommandHandler : IRequestHandler<ParsePharmacyInfoFromQuestionnaireCommand, Unit>
{
    private readonly IPatientsService _patientsService;
    private readonly IQuestionnaireResultsService _questionnaireResultsService;
    private readonly IPatientPharmacyInfoService _patientPharmacyInfoService;

    public ParsePharmacyInfoFromQuestionnaireCommandHandler(
        IPatientsService patientsService, 
        IQuestionnaireResultsService questionnaireResultsService, 
        IPatientPharmacyInfoService patientPharmacyInfoService)
    {
        _patientsService = patientsService;
        _questionnaireResultsService = questionnaireResultsService;
        _patientPharmacyInfoService = patientPharmacyInfoService;
    }

    public async Task<Unit> Handle(ParsePharmacyInfoFromQuestionnaireCommand command, CancellationToken cancellationToken)
    {
        var specification = PatientSpecifications.Empty;

        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);

        var questionnaireResult = await _questionnaireResultsService.GetAsync(
            id: command.QuestionnaireResultId,
            patientId: command.PatientId
        );

        var flow = new ParsePharmacyInfoFromQuestionnaireFlow(
            patient: patient,
            result: questionnaireResult
        );

        var result = flow.Execute();

        if (result.PharmacyInfo is not null)
        {
            await _patientPharmacyInfoService.CreateOrUpdateAsync(result.PharmacyInfo);
        }
        
        return Unit.Value;
    }
}