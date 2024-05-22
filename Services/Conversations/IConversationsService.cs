using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Infrastructure.Data.Queries.CustomSql.Models;
using WildHealth.Shared.Data.Helpers;

namespace WildHealth.Application.Services.Conversations
{
    public interface IConversationsService
    {
        /// <summary>
        /// Get unread message instances for all employees in a practice
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<EmployeeUnreadMessagesModel[]> EmployeeUnreadMessages(int practiceId);
        
        /// <summary>
        /// Returns all conversations where the provider is active but the patient does NOT have an active subscription
        /// </summary>
        /// <returns></returns>
        Task<Conversation[]> HealthConversationsWithProviderAndPatientCancelled();

        /// <summary>
        /// Returns all conversations where provider is active but health conversation is stale for XXX days and provider has read last message index
        /// </summary>
        /// <param name="daysStale"></param>
        /// <returns></returns>
        Task<Conversation[]> HealthConversationsWithProviderStaleForDays(int daysStale);

        /// <summary>
        /// Returns all conversations where provider is active but support conversation is stale for XXX days and provider has read last message index
        /// </summary>
        /// <param name="daysStale"></param>
        /// <returns></returns>
        Task<Conversation[]> SupportConversationsWithProviderStaleForDays(int daysStale);
        
        /// <summary>
        /// Returns all active conversation 
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetAllActiveAsync();

        /// <summary>
        /// <see cref="IConversationsService.GetByParticipantEmail(string)"/>
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<Conversation[]> GetByParticipantEmail(string email);
        
        /// <summary>
        /// Returns all active support conversation 
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetAllActiveSupportAsync();

        /// <summary>
        /// <see cref="IConversationsService.GetByParticipantIdentity"/>
        /// </summary>
        /// <param name="participantIdentity"></param>
        /// <returns></returns>
        Task<Conversation[]> GetByParticipantIdentity(string participantIdentity);
        
        /// <summary>
        /// Returns conversation by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Conversation> GetByIdAsync(int id);
        
        /// <summary>
        /// Returns conversation by external vendor conversation id
        /// </summary>
        /// <param name="externalVendorId"></param>
        /// <param name="isTracking"></param>
        /// <returns></returns>
        Task<Conversation> GetByExternalVendorIdAsync(string externalVendorId, bool isTracking=false);

        /// <summary>
        /// Returns conversation by external vendor conversation id
        /// </summary>
        /// <param name="vendorExternalId"></param>
        /// <returns></returns>
        Task<Conversation> GetByExternalVendorIdTrackAsync(string vendorExternalId);
        
        /// <summary>
        /// Returns conversation by external vendor conversation id using specification
        /// </summary>
        /// <param name="vendorExternalId"></param>
        /// <returns></returns
        Task<Conversation> GetByExternalVendorIdSpecAsync(string vendorExternalId, ISpecification<Conversation> spec);
        
        /// <summary>
        /// Returns all health conversations by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<Conversation> GetHealthConversationByPatientAsync(int patientId);

        /// <summary>
        /// Get all patient's conversations
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<List<Conversation>> GetAllConversationByPatientAsync(int patientId);

        /// <summary>
        /// Returns all support conversation by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetSupportConversationsByPatientAsync(int patientId);

        /// <summary>
        /// Returns all conversations by employee id
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="isActive"></param>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetConversationsByEmployeeAsync(int employeeId, bool isActive = false);

        /// <summary>
        /// Returns all conversations by employee id
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="isActive"></param>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetConversationsByEmployeeAsync(int employeeId, ISpecification<Conversation> specification, bool isActive = false);
        
        /// <summary>
        /// Returns all delegated conversations by employee id
        /// </summary>
        /// <param name="delegatedTo"></param>
        /// <param name="delegatedBy"></param>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetDelegatedConversationsByEmployeeAsync(int delegatedTo, int delegatedBy);

        /// <summary>
        /// Updates Conversation
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        Task<Conversation> UpdateConversationAsync(Conversation conversation);
     
        /// <summary>
        /// Creates conversation
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        Task<Conversation> CreateConversationAsync(Conversation conversation);

        /// <summary>
        /// Returns conversations which are support and does not have stuff as participant
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetSupportSubmissionsAsync(int[] locationIds);

        /// <summary>
        /// Adding user to conversation.
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="employee"></param>
        /// <returns></returns>
        Task<Conversation> AddParticipantAsync(Conversation conversation, ConversationParticipantEmployee employee);

        /// <summary>
        /// Removing user from conversation.
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="employee"></param>
        /// <returns></returns>
        Task<Conversation> RemoveParticipantAsync(Conversation conversation, ConversationParticipantEmployee employee);

        /// <summary>
        /// <see cref="IConversationsService.GetAllActiveHealthAsync"/>
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetAllActiveHealthAsync();

        /// <summary>
        /// All health conversations with a message sent since the given date
        /// </summary>
        /// <param name="since"></param>
        /// <returns></returns>
        Task<IEnumerable<Conversation>> GetAllActiveWithMessageSentSince(DateTime since);
        
        /// <summary>
        /// Returns conversation participants
        /// </summary>
        /// <returns></returns>
        Task<(IEnumerable<ConversationParticipantEmployee>, IEnumerable<ConversationParticipantPatient>)> GetConversationParticipants(int conversationId);

        /// <summary>
        /// Returns unread message information about any conversations that the employee should be responsible for based on conversation settings 
        /// </summary>
        /// <param name="forwardingToEmployeeId"></param>
        /// <returns></returns>
        Task<IEnumerable<ForwardingConversationWithUnreadMessageModel>> GetForwardingConversationsWithUnreadMessages(int forwardingToEmployeeId);
    }
}
