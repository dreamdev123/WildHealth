using MediatR;
using WildHealth.Common.Models.InsuranceConfigurations;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.Commands.InsuranceConfigurations;

public class UpdateInsuranceConfigurationCommand : IRequest<InsuranceConfig>
{
    public UpdateInsuranceConfigModel Model;

    public UpdateInsuranceConfigurationCommand(UpdateInsuranceConfigModel model)
    {
        Model = model;
    }
}