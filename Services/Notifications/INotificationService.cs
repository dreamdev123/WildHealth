using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Notifications;
using WildHealth.Domain.Entities.Notifications.Abstracts;

namespace WildHealth.Application.Services.Notifications
{
    /// <summary>
    /// Provides method for working with notifications
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Get all notifications for selected users
        /// </summary>
        /// <param name="receiversIds"></param>
        /// <returns></returns>
        Task<ICollection<Notification>> GetNotificationsAsync(IEnumerable<int?> receiversIds);

        /// <summary>
        /// Get last notifications for selected users by date
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="lastNotificationId"></param>
        /// <returns></returns>
        Task<ICollection<Notification>> GetLastNotificationsAsync(int userId, int? lastNotificationId);

        /// <summary>
        /// New notification will be immediately sent to active users in browser and send email 
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task CreateNotificationAsync(IBaseNotification model);

        /// <summary>
        /// Delete all user notifications.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task DeleteUserNotificationsAsync(int userId);
        
        /// <summary>
        /// Delete user notifications by ids.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task DeleteUserNotificationsAsync(int userId, int[] ids);

        /// <summary>
        /// Delete user notifications by notification id
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="notificationId"></param>
        /// <returns></returns>
        Task DeleteUserNotificationAsync(int userId, int notificationId);
    }
}
