using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Common.Models.Conversations;

namespace WildHealth.Application.Services.Conversations
{
    public interface IScheduledMessagesService
    {
        /// <summary>
        /// Create scheduled message 
        /// </summary>
        /// <param name="scheduledMessage"></param>
        /// <returns></returns>
        Task<ScheduledMessage> CreateAsync(ScheduledMessage scheduledMessage);

        /// <summary>
        /// Return all active scheduled messages which did not sent for now 
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        Task<IEnumerable<ScheduledMessage>> GetMessagesToSendAsync(DateTime currentTime);

        /// <summary>
        /// Return schedule message by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ScheduledMessage?> GetByIdAsync(int id);

        /// <summary>
        /// Get scheduled messages by participantEmployee id
        /// </summary>
        /// <param name="participantEmployeeId"></param>
        /// <returns></returns>
        Task<IEnumerable<ScheduledMessage>> GetByEmployeeAsync(int participantEmployeeId);

        /// <summary>
        /// Update scheduled messages
        /// </summary>
        /// <param name="scheduledMessage"></param>
        /// <returns></returns>
        Task<ScheduledMessage> UpdateAsync(ScheduledMessage scheduledMessage);

        /// <summary>
        /// Delete scheduled messages
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeleteAsync(int id);

        /// <summary>
        /// Get scheduled messages by participantEmployee id
        /// </summary>
        /// <param name="participantEmployeeId"></param>
        /// <returns></returns>
        Task<ScheduledMessageModel[]> GetMessagesByEmployeeAsync(int participantEmployeeId);
    }
}
