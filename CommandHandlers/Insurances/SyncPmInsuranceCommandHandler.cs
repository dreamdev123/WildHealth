using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Models;
using WildHealth.Fhir.Models.Organizations;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;
using WildHealth.Shared.Data.Repository;
using Microsoft.EntityFrameworkCore;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class SyncPmInsuranceCommandHandler : IRequestHandler<SyncPmInsuranceCommand>
{
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly IGeneralRepository<Insurance> _insurancesRepository;
    private readonly IGeneralRepository<InsuranceIntegration> _insuranceIntegrationsRepository;
    private readonly ILogger<SyncProviderWithOpenPmCommandHandler> _logger;

    public SyncPmInsuranceCommandHandler(
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        IGeneralRepository<Insurance> insurancesRepository,
        IGeneralRepository<InsuranceIntegration> insuranceIntegrationsRepository,
        ILogger<SyncProviderWithOpenPmCommandHandler> logger)
    {
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _insurancesRepository = insurancesRepository;
        _insuranceIntegrationsRepository = insuranceIntegrationsRepository;
        _logger = logger;
    }

    public async Task Handle(SyncPmInsuranceCommand command, CancellationToken cancellationToken)
    {   
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(practiceId: command.PracticeId);
        
        var filter = new Dictionary<string, object>()
        {
            { "active", true },
            { "type", OpenPmConstants.Organization.Types.Insurance },
            { "name", command.PmInsuranceName }
        };

        var organizations = await pmService.QueryOrganizations(command.PracticeId, filter);

        foreach (var organization in organizations)
        {
            var insurance = await GetOrCreateInsurance(organization);

            await CreateIntegration(insurance, organization, pmService);
        }
    }

    private async Task<Insurance> GetOrCreateInsurance(OrganizationModel organization)
    {
        
        var insurance = await _insurancesRepository
            .All()
            .Where(o => o.Name == organization.Name)
            .FirstOrDefaultAsync();
            

        if (insurance is not null)
        {
            return insurance;
        }

        var newInsurance = new Insurance(name: organization.Name);
            
        await _insurancesRepository.AddAsync(newInsurance);

        await _insurancesRepository.SaveAsync();

        return newInsurance;
    }

    private async Task CreateIntegration(Insurance insurance, OrganizationModel organization, IPracticeManagementIntegrationService pmService)
    {
        if (insurance.IsLinkedWithIntegrationSystem(pmService.Vendor))
        {
            return;
        }
        
        var integration = new InsuranceIntegration(
            vendor: pmService.Vendor, 
            purpose: IntegrationPurposes.Insurance.ExternalId,
            value: organization.Id,
            insurance: insurance);

        await _insuranceIntegrationsRepository.AddAsync(integration);

        await _insuranceIntegrationsRepository.SaveAsync();
    }
}