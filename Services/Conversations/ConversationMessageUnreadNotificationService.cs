using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Conversations
{
    /// <summary>
    /// <see cref="IConversationMessageUnreadNotificationService"/>
    /// </summary>
    public class ConversationMessageUnreadNotificationService : IConversationMessageUnreadNotificationService
    {
        private readonly IGeneralRepository<ConversationMessageUnreadNotification> _conversationMessageUnreadNotificationRepository;

        public ConversationMessageUnreadNotificationService(IGeneralRepository<ConversationMessageUnreadNotification> conversationMessageUnreadNotificationRepository)
        {
            _conversationMessageUnreadNotificationRepository = conversationMessageUnreadNotificationRepository;
        }

        /// <summary>
        /// <see cref="IConversationMessageUnreadNotificationService.GetLastByUserConversationAsync"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public async Task<ConversationMessageUnreadNotification> GetLastByUserConversationAsync(int userId, int conversationId)
        {
            var lastNotification = await _conversationMessageUnreadNotificationRepository
                .All()
                .ByConversationId(conversationId)
                .ByUserId(userId)
                .OrderBy(x => x.SentAt)
                .IncludeRelations()
                .LastOrDefaultAsync();

            if (lastNotification is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Last unread notification does not exist.");
            }
            
            return lastNotification;
        }

        /// <summary>
        /// <see cref="IConversationMessageUnreadNotificationService.CreateAsync"/>
        /// </summary>
        /// <param name="conversationMessageUnreadNotification"></param>
        /// <returns></returns>
        public async Task<ConversationMessageUnreadNotification> CreateAsync(ConversationMessageUnreadNotification conversationMessageUnreadNotification)
        {
            await _conversationMessageUnreadNotificationRepository.AddAsync(conversationMessageUnreadNotification);

            await _conversationMessageUnreadNotificationRepository.SaveAsync();

            return conversationMessageUnreadNotification;
        }

        /// <summary>
        /// <see cref="IConversationMessageUnreadNotificationService.UpdateAsync(ConversationMessageUnreadNotification)"/>
        /// </summary>
        /// <param name="conversationMessageUnreadNotification"></param>
        /// <returns></returns>
        public async Task<ConversationMessageUnreadNotification> UpdateAsync(ConversationMessageUnreadNotification conversationMessageUnreadNotification)
        {
            _conversationMessageUnreadNotificationRepository.Edit(conversationMessageUnreadNotification);
            
            await _conversationMessageUnreadNotificationRepository.SaveAsync();

            return conversationMessageUnreadNotification;
        }

        /// <summary>
        /// <see cref="IConversationMessageUnreadNotificationService.GetByUserConversationAsync(int,int)"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="conversationId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ConversationMessageUnreadNotification>> GetByUserConversationAsync(int userId, int conversationId)
        {
            var conversationMessageUnreadNotifications = await _conversationMessageUnreadNotificationRepository
                .All()
                .ByConversationId(conversationId)
                .ByUserId(userId)
                .IncludeRelations()
                .ToArrayAsync();
            
            return conversationMessageUnreadNotifications;
        }

        /// <summary>
        /// <see cref="IConversationMessageUnreadNotificationService.GetOutstandingNotificationsAsync"/>
        /// </summary>
        /// <param name="conversationVendorExternalId"></param>
        /// <param name="participantVendorExternalId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ConversationMessageUnreadNotification>> GetOutstandingNotificationsAsync(string conversationVendorExternalId, string participantVendorExternalId) 
        {
            return await _conversationMessageUnreadNotificationRepository
                .All()
                .ByConversationVendorExternalId(conversationVendorExternalId)
                .ByParticipantVendorExternalId(participantVendorExternalId)
                .IsNotRead()
                .IncludeRelations()
                .ToArrayAsync();
        }
    }
}
