using System;
using System.Threading.Tasks;
using WildHealth.Common.Models.Auth;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Services.Auth
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticates user
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="originPractice"></param>
        /// <param name="onBehalfPractice"></param>
        /// <param name="permissions"></param>
        /// <param name="availableLocationsIds"></param>
        /// <param name="defaultLocationId"></param>
        /// <param name="external"></param>
        /// <returns></returns>
        public Task<AuthenticationResultModel> Authenticate(UserIdentity identity,
            Practice originPractice,
            Practice? onBehalfPractice,
            PermissionType[] permissions,
            int[] availableLocationsIds,
            int? defaultLocationId,
            ExternalAuthorization? external);

        /// <summary>
        /// Get identity by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<UserIdentity> GetByIdAsync(int id);

        /// <summary>
        /// Get identity by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<UserIdentity> GetByPatientIdAsync(int patientId);

        /// <summary>
        /// Get identity by employee id
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        Task<UserIdentity> GetByEmployeeIdAsync(int employeeId);

        /// <summary>
        /// Get identity by email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<UserIdentity> GetByEmailAsync(string email);

        /// <summary>
        /// Get identity by email or null if none exists
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<UserIdentity> GetByEmailOrNullAsync(string email);

        /// <summary>
        /// Get universal ID
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<Guid> GetUniversalId(int userId);
        
        /// <summary>
        /// Create a record
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        Task<UserIdentity> CreateAsync(UserIdentity identity);

        /// <summary>
        /// Set new user's password 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task UpdatePassword(UserIdentity user, string password);
        
        /// <summary>
        /// Update user identity
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task UpdateAsync(UserIdentity user);

        /// <summary>
        /// Updates user identity practice
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="practice"></param>
        /// <returns></returns>
        Task UpdatePracticeAsync(int userId, Practice practice);

        /// <summary>
        /// Set new user's password 
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task UpdatePassword(int userId, string password);

        /// <summary>
        /// Delete a record
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeleteAsync(int id);

        /// <summary>
        /// Checks is user with same email exists
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<bool> CheckIfEmailExistsAsync(string email);
        
        /// <summary>
        /// Verify identity
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        Task<UserIdentity> VerifyAsync(UserIdentity identity);


        #region external authorization
        
        /// <summary>
        /// Returns user external authorization
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<ExternalAuthorization> GetUserExternalAuthorizationAsync(int userId);
        
        /// <summary>
        /// Returns external authorization by integration id and provider
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        Task<ExternalAuthorization> GetExternalAuthorizationAsync(string integrationId, AuthorizationProvider provider);
        
        /// <summary>
        /// Returns external authorization by authToken and provider
        /// </summary>
        /// <param name="authToken"></param>
        /// <param name="provider"></param>
        Task<ExternalAuthorization> GetExternalAuthorizationByAuthTokenAsync(string authToken, AuthorizationProvider provider);

        /// <summary>
        /// Creates new external authorization
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        Task<ExternalAuthorization> CreateExternalAuthorizationAsync(ExternalAuthorization authorization);
        
        /// <summary>
        /// Updates external authorization
        /// </summary>
        /// <param name="authorization"></param>
        /// <returns></returns>
        Task<ExternalAuthorization> UpdateExternalAuthorizationAsync(ExternalAuthorization authorization);

        Task<UserIdentity> GetSpecAsync(int id, ISpecification<UserIdentity> specification);     
        #endregion
    }
}