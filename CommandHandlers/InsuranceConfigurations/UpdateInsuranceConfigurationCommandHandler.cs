using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using WildHealth.Application.Commands.InsuranceConfigurations;
using WildHealth.Application.Services.InsuranceConfigurations;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.CommandHandlers.InsuranceConfigurations;

public class UpdateInsuranceConfigurationCommandHandler : IRequestHandler<UpdateInsuranceConfigurationCommand, InsuranceConfig>
{
     private readonly IMapper _mapper;
    private readonly IInsuranceConfigsService _insuranceConfigsService;
    
    public UpdateInsuranceConfigurationCommandHandler(IMapper mapper, IInsuranceConfigsService insuranceConfigsService)
    {
        _mapper = mapper;
        _insuranceConfigsService = insuranceConfigsService;
    }

    public async Task<InsuranceConfig> Handle(UpdateInsuranceConfigurationCommand command, CancellationToken cancellationToken)
    {
        var insuranceConfiguration = _mapper.Map<InsuranceConfig>(command.Model);
        
        var configuration = await _insuranceConfigsService.UpdateAsync(insuranceConfiguration);

        return configuration;
    }
}