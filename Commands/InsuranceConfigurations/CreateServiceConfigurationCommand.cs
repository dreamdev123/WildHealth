using MediatR;
using WildHealth.Common.Models.InsuranceConfigurations;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.Commands.InsuranceConfigurations;

public class CreateServiceConfigurationCommand : IRequest<InsConfigService>
{
    public CreateInsConfigServiceModel Model { get; }

    public CreateServiceConfigurationCommand(CreateInsConfigServiceModel model)
    {
        Model = model;
    }
}