using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using WildHealth.Application.Commands.InsuranceConfigurations;
using WildHealth.Application.Services.InsuranceConfigurations;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.CommandHandlers.InsuranceConfigurations;

public class CreateServiceConfigurationCommandHandler : IRequestHandler<CreateServiceConfigurationCommand, InsConfigService>
{
    private readonly IMapper _mapper;
    private readonly IInsConfigServicesService _insConfigServicesService;
    
    public CreateServiceConfigurationCommandHandler(
        IMapper mapper,
        IInsConfigServicesService insConfigServicesService)
    {
        _mapper = mapper;
        _insConfigServicesService = insConfigServicesService;
    }

    public async Task<InsConfigService> Handle(CreateServiceConfigurationCommand command, CancellationToken cancellationToken)
    {
        var serviceConfiguration = _mapper.Map<InsConfigService>(command.Model);
        
        serviceConfiguration.ServiceStateConfigs = new List<InsConfigServiceState>();

        serviceConfiguration.InsurancePlanConfigs = new List<InsConfigInsurancePlan>();

        foreach (var stateId in command.Model.StateIds)
        {
            serviceConfiguration.ServiceStateConfigs.Add(new InsConfigServiceState(
                serviceConfiguration: serviceConfiguration,
                stateId: stateId));
        }
        
        foreach (var insurancePlanId in command.Model.InsurancePlanIds)
        {
            serviceConfiguration.InsurancePlanConfigs.Add(new InsConfigInsurancePlan( 
                serviceConfiguration: serviceConfiguration,
                insurancePlanId: insurancePlanId
            ));
        }
        
        var configuration = await _insConfigServicesService.CreateAsync(serviceConfiguration);
        
        return configuration;
    }
}