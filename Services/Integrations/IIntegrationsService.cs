using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.Services.Integrations
{
    /// <summary>
    /// Provides methods for working with integrations
    /// </summary>
    public interface IIntegrationsService
    {
        /// <summary>
        /// Creates user integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<UserIntegration> CreateAsync(UserIntegration integration);


        /// <summary>
        /// Returns the user integration for userid and vendor.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <returns></returns>
        Task<UserIntegration?> GetUserIntegrationAsync(int userId, IntegrationVendor vendor, string purpose);
        
        /// <summary>
        /// Creates order invoice integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<OrderInvoiceIntegration> CreateAsync(OrderInvoiceIntegration integration);

        /// <summary>
        /// Updates userintegration
        /// Creates patient integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<PatientIntegration> CreateAsync(PatientIntegration integration);
        
        /// <summary>
        /// Creates insurance integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<InsuranceIntegration> CreateAsync(InsuranceIntegration integration);

        /// <summary>
        /// Creates order integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<OrderIntegration> CreateAsync(OrderIntegration integration);

        /// <summary>
        /// Creates employee integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<EmployeeIntegration> CreateAsync(EmployeeIntegration integration);
        
        /// <summary>
        /// Creates appointment integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<AppointmentIntegration> CreateAsync(AppointmentIntegration integration);
        
        /// <summary>
        /// Create sync record integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<SyncRecordIntegration> CreateAsync(SyncRecordIntegration integration);
        
        /// <summary>
        /// Create claim integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<ClaimIntegration> CreateAsync(ClaimIntegration integration);

        /// <summary>
        /// Create coverage integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<CoverageIntegration> CreateAsync(CoverageIntegration integration);

        /// <summary>
        /// Create automated document source item integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<AutomatedDocumentSourceItemIntegration> CreateAsync(AutomatedDocumentSourceItemIntegration integration);
        
        /// <summary>
        /// Updates user integration
        /// </summary>
        /// <param name="integration"></param>
        /// <returns></returns>
        Task<WildHealth.Domain.Entities.Integrations.Integration> UpdateAsync(WildHealth.Domain.Entities.Integrations.Integration integration);

        /// <summary>
        /// Returns integration by external vendor, purpose and id
        /// </summary>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <param name="externalId"></param>
        /// <returns></returns>
        Task<WildHealth.Domain.Entities.Integrations.Integration?> GetAsync(IntegrationVendor vendor, string purpose, string externalId);

        Task<WildHealth.Domain.Entities.Integrations.Integration> GetAsync(IntegrationVendor vendor, string externalId);

        Task<WildHealth.Domain.Entities.Integrations.Integration> GetForAutomatedDocumentSourceItemAsync(
            IntegrationVendor vendor, string externalId);
        
        Task<int> GetPatientIdByClaimUniversalId(Guid? claimUniversalId);
    }
}