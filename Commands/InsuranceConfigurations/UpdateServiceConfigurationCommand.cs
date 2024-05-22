using MediatR;
using WildHealth.Common.Models.InsuranceConfigurations;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.Commands.InsuranceConfigurations;

public class UpdateServiceConfigurationCommand : IRequest<InsConfigService>
{
    public UpdateInsConfigServiceModel Model { get; }

    public UpdateServiceConfigurationCommand(UpdateInsConfigServiceModel model)
    {
        Model = model;
    }
}