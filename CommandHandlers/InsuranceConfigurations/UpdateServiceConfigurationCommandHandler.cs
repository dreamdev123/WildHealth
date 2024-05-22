using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using WildHealth.Application.Commands.InsuranceConfigurations;
using WildHealth.Application.Services.InsuranceConfigurations;
using WildHealth.Common.Models.InsuranceConfigurations;
using WildHealth.Domain.Entities.InsuranceConfigurations;

namespace WildHealth.Application.CommandHandlers.InsuranceConfigurations;

public class UpdateServiceConfigurationCommandHandler : IRequestHandler<UpdateServiceConfigurationCommand, InsConfigService>
{
    private readonly IMapper _mapper;
    private readonly IInsConfigServicesService _insConfigServicesService;
    private readonly IInsConfigServiceStatesService _insConfigServiceStatesService;
    private readonly IInsConfigInsurancePlansService _insConfigInsurancePlansService;
    
    public UpdateServiceConfigurationCommandHandler(
        IMapper mapper,
        IInsConfigServicesService insConfigServicesService,
        IInsConfigServiceStatesService insConfigServiceStatesService,
        IInsConfigInsurancePlansService insConfigInsurancePlansService)
    {
        _mapper = mapper;
        _insConfigServicesService = insConfigServicesService;
        _insConfigServiceStatesService = insConfigServiceStatesService;
        _insConfigInsurancePlansService = insConfigInsurancePlansService;
    }

    public async Task<InsConfigService> Handle(UpdateServiceConfigurationCommand command, CancellationToken cancellationToken)
    {
        var model = command.Model;
        
        await RemoveServiceStateConfigurations(model);

        await AddServiceStateConfigurations(model);

        await RemoveInsurancePlanConfigurations(model);

        await AddInsurancePlanConfigurations(model);

        var serviceConfiguration = _mapper.Map<InsConfigService>(model);

        var configuration = await _insConfigServicesService.UpdateAsync(serviceConfiguration);

        return configuration;
    }
    
    #region private

    private async Task RemoveServiceStateConfigurations(UpdateInsConfigServiceModel model)
    {
        var serviceStatesConfigurations = await _insConfigServiceStatesService.GetByServiceConfigurationIdAsync(model.Id);
        
        foreach (var serviceStatesConfiguration in serviceStatesConfigurations)
        {
            if (!model.StateIds.Contains(serviceStatesConfiguration.StateId))
            {
                await _insConfigServiceStatesService.DeleteAsync(serviceStatesConfiguration.GetId());
            }
        }
    }

    private async Task AddServiceStateConfigurations(UpdateInsConfigServiceModel model)
    {
        var serviceStatesConfigurations = await _insConfigServiceStatesService.GetByServiceConfigurationIdAsync(model.Id);
        
        foreach (var stateId in model.StateIds)
        {
            if (serviceStatesConfigurations.All(o => o.StateId != stateId))
            {
                var serviceStateConfiguration = new InsConfigServiceState(
                    serviceConfigurationId: model.Id,
                    stateId: stateId);
                
                await _insConfigServiceStatesService.CreateAsync(serviceStateConfiguration);
            }
        }
    }
    
    private async Task RemoveInsurancePlanConfigurations(UpdateInsConfigServiceModel model)
    {
        var insurancePlanConfigs = await _insConfigInsurancePlansService.GetByServiceConfigurationIdAsync(model.Id);
        
        foreach (var insurancePlanConfig in insurancePlanConfigs)
        {
            if (!model.InsurancePlanIds.Contains(insurancePlanConfig.InsurancePlanId))
            {
                await _insConfigInsurancePlansService.DeleteAsync(insurancePlanConfig.GetId());
            }
        }
    }

    private async Task AddInsurancePlanConfigurations(UpdateInsConfigServiceModel model)
    {
        var insurancePlanConfigs = await _insConfigInsurancePlansService.GetByServiceConfigurationIdAsync(model.Id);
        
        foreach (var insurancePlanId in model.InsurancePlanIds)
        {
            if (insurancePlanConfigs.All(o => o.InsurancePlanId != insurancePlanId))
            {
                var insurancePlanConfiguration = new InsConfigInsurancePlan(
                    serviceConfigId: model.Id, 
                    insurancePlanId: insurancePlanId);
                
                await _insConfigInsurancePlansService.CreateAsync(insurancePlanConfiguration);
            }
        }
    }
    
    #endregion
}