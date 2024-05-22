using MediatR;
using WildHealth.Common.Models.InsuranceConfigurations;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.Commands.InsuranceConfigurations;

public class CreateInsuranceConfigurationCommand : IRequest<InsuranceConfig>
{
    public CreateInsuranceConfigModel Model { get; }

    public CreateInsuranceConfigurationCommand(CreateInsuranceConfigModel model)
    {
        Model = model;
    }
}