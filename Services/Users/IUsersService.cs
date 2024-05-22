using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Enums;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Shared.Data.Helpers;


namespace WildHealth.Application.Services.Users
{
    /// <summary>
    /// Provides methods for working with users
    /// </summary>
    public interface IUsersService
    {
        /// <summary>
        /// Returns users
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<User>> GetAllAsync();
        
        /// <summary>
        /// Returns the user with the given id
        /// </summary>
        /// <returns></returns>
        Task<User?> GetByIdAsync(int id);
        
        /// <summary>
        /// Returns the users with the given E164 formatted phone number.
        /// See https://www.twilio.com/docs/glossary/what-e164
        /// </summary>
        /// <param name="phone">e.g. +12345678901</param>
        /// <returns></returns>
        Task<IEnumerable<User>> GetByPhoneAsync(string phone);

        /// <summary>
        /// Returns user
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<User> GetAsync(int id);
        
        /// <summary>
        /// Returns user by specification
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<User> GetAsync(int id, ISpecification<User> specification);

        /// <summary>
        /// Updates user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        Task<User> UpdateAsync(User user);

        /// <summary>
        /// Get users by permission
        /// </summary>
        /// <param name="permission"></param>
        /// <returns></returns>
        Task<IEnumerable<User>> GetUsersByPermissionAsync(PermissionType permission);

        /// <summary>
        /// Returns user by email, null if not exists
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<User?> GetByEmailAsync(string email);

        /// <summary>
        /// Get user by email with specification
        /// </summary>
        /// <param name="email"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<User> GetByEmailAsync(string email, ISpecification<User> specification);

        /// <summary>
        /// Returns user by conversation identity, null if not exists
        /// </summary>
        /// <param name="conversationIdentity"></param>
        /// <returns></returns>
        Task<User?> GetByConversationIdentityAsync(string conversationIdentity);
        
        /// <summary>
        /// Returns user Role from a given identity    
        /// </summary>
        int? GetRoleId(UserIdentity identity);
            
        /// <summary>
        /// Returns user Subscription status from a given identity    
        /// </summary>
        SubscriptionStatus? DetermineSubscriptionStatus(UserIdentity identity);

        /// <summary>
        /// Returns user Subscription from a given identity    
        /// </summary>
        Subscription? GetMostRecentSubscription(UserIdentity identity);

        /// <summary>
        /// Returns Subscription Expiration Date from a given identity    
        /// </summary>
        long? GetSubscriptionExpirationDate(UserIdentity identity);

        /// <summary>
        /// Returns Tracking identifier from a given identity    
        /// </summary>
        string? GetTrackingIdentifier(UserIdentity identity);

        /// <summary>
        /// Returns Assigned pod from a given identity    
        /// </summary>
        string? GetAssignedPod(UserIdentity identity);

        /// <summary>
        /// Returns Assigned provider from a given identity    
        /// </summary>
        string? GetAssignedProvider(UserIdentity identity);

        /// <summary>
        /// Returns if all agreements are confirmed
        /// </summary>
        bool IsAgreementsConfirmed(UserIdentity identity);

        /// <summary>
        /// Returns the assigned Health coach of a given identity
        /// </summary>
        string? GetAssignedHealthCoach(UserIdentity identity);


        /// <summary>
        /// Returns a name of the user reflected by the userId
        /// </summary>
        Task<string> GetUserName(int? userId);

        /// <summary>
        /// Returns the user formatted for Mobile api gateway
        /// </summary>
        Task<UserGetMeModel> mapToUserGetMe(UserIdentity identity, string token);

        Task<Guid> GetUserUniversalId(int patientId);
        Task<User> GetByPatientIdAsync(int patientId);
        Task<string> GetEmailAsync(int patientId);
    } 
}
