using WildHealth.Application.Commands.PharmacyInfo;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.CommandHandlers.PharmacyInfo.Flows;

public class CreateOrUpdatePharmacyInfoFlow
{
    private readonly Patient _patient;
    private readonly CreateOrUpdatePharmacyInfoCommand _command;

    public CreateOrUpdatePharmacyInfoFlow(
        Patient patient, 
        CreateOrUpdatePharmacyInfoCommand command)
    {
        _patient = patient;
        _command = command;
    }

    public CreateOrUpdatePharmacyInfoFlowResult Execute()
    {
        var pharmacyInfo = new PatientPharmacyInfo(
            patient: _patient, 
            streetAddress: _command.Address,
            city: _command.City,
            zipCode: _command.ZipCode,
            state: _command.State,
            country: _command.Country,
            name: _command.Name,
            phone: _command.Phone
        );

        return new CreateOrUpdatePharmacyInfoFlowResult(pharmacyInfo);
    }
}

public record CreateOrUpdatePharmacyInfoFlowResult(PatientPharmacyInfo PharmacyInfo);