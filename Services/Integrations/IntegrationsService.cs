using System;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Shared.Data.Repository;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Models.Exceptions;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Integrations
{
    /// <summary>
    /// <see cref="IIntegrationsService"/>
    /// </summary>
    public class IntegrationsService : IIntegrationsService
    {
        private readonly IGeneralRepository<WildHealth.Domain.Entities.Integrations.Integration> _integrationsRepository;
        private readonly IGeneralRepository<Note> _notesRepository;

        public IntegrationsService(
            IGeneralRepository<WildHealth.Domain.Entities.Integrations.Integration> integrationsRepository, 
            IGeneralRepository<Note> notesRepository)
        {
            _integrationsRepository = integrationsRepository;
            _notesRepository = notesRepository;
        }

        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(UserIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<UserIntegration> CreateAsync(UserIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }

        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(UserIntegration)"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <returns></returns>
        public async Task<UserIntegration?> GetUserIntegrationAsync(int userId, IntegrationVendor vendor, string purpose)
        {
            var integrations = await _integrationsRepository
                .All()
                .Include(i => i.UserIntegration)
                .Where(i => i.Vendor == vendor)
                .Where(i => i.UserIntegration.UserId == userId)
                .Where(i => i.Purpose == purpose)
                .ToListAsync();

            if (integrations.Count() > 1)
            {
                throw new AppException(HttpStatusCode.Ambiguous,
                    $"User {userId} has more than one {vendor} {purpose} integration.");
            }

            var integration = integrations.FirstOrDefault();
            return integration?.UserIntegration;
        }


        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<OrderInvoiceIntegration> CreateAsync(OrderInvoiceIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }

        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(PatientIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<PatientIntegration> CreateAsync(PatientIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }
        
        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(OrderIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<OrderIntegration> CreateAsync(OrderIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);
            
            await _integrationsRepository.SaveAsync();

            return integration;
        }
        
        
        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(InsuranceIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<InsuranceIntegration> CreateAsync(InsuranceIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }
        
        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(EmployeeIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<EmployeeIntegration> CreateAsync(EmployeeIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }
        
        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(AppointmentIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<AppointmentIntegration> CreateAsync(AppointmentIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }
        
        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(SyncRecordIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<SyncRecordIntegration> CreateAsync(SyncRecordIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }
        
        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(ClaimIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<ClaimIntegration> CreateAsync(ClaimIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }
        
        /// <summary>
        /// <see cref="IIntegrationsService.CreateAsync(CoverageIntegration)"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<CoverageIntegration> CreateAsync(CoverageIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }

        /// <summary>
        /// Create automated document source item integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<AutomatedDocumentSourceItemIntegration> CreateAsync(
            AutomatedDocumentSourceItemIntegration integration)
        {
            await _integrationsRepository.AddRelatedEntity(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }

        /// <summary>
        /// <see cref="IIntegrationsService.UpdateAsync"/>
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        public async Task<WildHealth.Domain.Entities.Integrations.Integration> UpdateAsync(WildHealth.Domain.Entities.Integrations.Integration integration)
        {
            _integrationsRepository.Edit(integration);

            await _integrationsRepository.SaveAsync();

            return integration;
        }

        /// <summary>
        /// Returns patient by external vendor and id
        /// </summary>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <param name="externalId"></param>
        /// <returns></returns>
        public async Task<WildHealth.Domain.Entities.Integrations.Integration?> GetAsync(IntegrationVendor vendor, string purpose, string externalId)
        {
            var integration = await _integrationsRepository
                .All()
                .IncludePatient()
                .Where(o => o.Purpose == IntegrationPurposes.User.GetPurpose[purpose] && o.Vendor == vendor && o.Value == externalId)
                .FirstOrDefaultAsync();

            return integration;
        }

        public async Task<WildHealth.Domain.Entities.Integrations.Integration> GetForAutomatedDocumentSourceItemAsync(IntegrationVendor vendor, string externalId)
        {
            var integration = await _integrationsRepository
                .All()
                .Include(i => i.AutomatedDocumentSourceItemIntegration)
                .ThenInclude(ai => ai.AutomatedDocumentSourceItem)
                .ThenInclude(o => o.AutomatedDocumentSource)
                .Where(i => 
                    i.Value == externalId && 
                    i.Vendor == vendor)
                .FindAsync();

            return integration;
        }
        
        public async Task<WildHealth.Domain.Entities.Integrations.Integration> GetAsync(IntegrationVendor vendor, string externalId)
        {
            var integration = await _integrationsRepository
                .All()
                .Include(i => i.PaymentIssues)
                .ThenInclude(pi => pi.Patient)
                .ThenInclude(p => p.User)
                .Include(i => i.OrderInvoiceIntegration)
                .ThenInclude(o => o.Order)
                .ThenInclude(o => o.Patient)
                .ThenInclude(o => o.User)
                .Include(i => i.OrderInvoiceIntegration)
                .ThenInclude(o => o.Order)
                .ThenInclude(o => o.Items)
                .Include(i => i.SubscriptionIntegration)
                .ThenInclude(s => s.Subscription)
                .ThenInclude(o => o.Patient)
                .ThenInclude(o => o.User)
                .Include(i => i.ClaimIntegration)
                .ThenInclude(ci => ci.Claim)
                .Include(x => x.PaymentScheduleItemIntegration)
                .ThenInclude(x => x.PaymentScheduleItem)
                .ThenInclude(x => x.Payment)
                .ThenInclude(x => x.Subscriptions)
                .ThenInclude(x => x.Patient)
                .ThenInclude(o => o.User)
                .Where(i => 
                    i.Value == externalId && 
                    i.Vendor == vendor &&
                    (i.OrderInvoiceIntegration != null && (i.OrderInvoiceIntegration.Order.Type == OrderType.Lab || i.OrderInvoiceIntegration.Order.Type == OrderType.Epigenetic) || 
                     i.SubscriptionIntegration != null || 
                     i.PaymentScheduleItemIntegration != null))
                .FindAsync();

            return integration;
        }

        public async Task<int> GetPatientIdByClaimUniversalId(Guid? claimUniversalId)
        {
            if (claimUniversalId is null) 
                throw new EntityNotFoundException("ClaimUniversalId is not provided");

            return await _notesRepository.All()
                .Where(n => n.UniversalId == claimUniversalId.Value) // not obvious but ClaimUniversalId is actually Note.UniversalId
                .Select(n => n.PatientId)
                .FindAsync();
        }
    }
}