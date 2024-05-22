using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.PharmacyInfo;
using WildHealth.Application.Services.PatientPharmacyInfos;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;
using WildHealth.Application.CommandHandlers.PharmacyInfo.Flows;

namespace WildHealth.Application.CommandHandlers.PharmacyInfo;

public class CreateOrUpdatePharmacyInfoCommandHandler : IRequestHandler<CreateOrUpdatePharmacyInfoCommand, PatientPharmacyInfo>
{
    private readonly IPatientsService _patientsService;
    private readonly IPatientPharmacyInfoService _patientPharmacyInfoService;

    public CreateOrUpdatePharmacyInfoCommandHandler(
        IPatientsService patientsService,
        IPatientPharmacyInfoService patientPharmacyInfoService)
    {
        _patientsService = patientsService;
        _patientPharmacyInfoService = patientPharmacyInfoService;
    }
    
    public async Task<PatientPharmacyInfo> Handle(CreateOrUpdatePharmacyInfoCommand command, CancellationToken cancellationToken)
    {
        var specification = PatientSpecifications.Empty;

        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);

        var flow = new CreateOrUpdatePharmacyInfoFlow(
            patient: patient,
            command: command
        );

        var result = flow.Execute();

        await _patientPharmacyInfoService.CreateOrUpdateAsync(result.PharmacyInfo);

        return result.PharmacyInfo;
    }
}