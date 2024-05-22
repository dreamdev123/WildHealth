using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Application.Services.Notifications;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using MediatR;

namespace WildHealth.Application.EventHandlers.Conversations
{
    public class SendNewSupportTicketNotificationOnConversationCreatedEvent : INotificationHandler<ConversationCreatedEvent>
    {
        private readonly IEmployeeService _employeeService;
        private readonly INotificationService _notificationService;

        public SendNewSupportTicketNotificationOnConversationCreatedEvent(
            IEmployeeService employeeService, 
            INotificationService notificationService)
        {
            _employeeService = employeeService;
            _notificationService = notificationService;
        }

        public async Task Handle(ConversationCreatedEvent @event, CancellationToken cancellationToken)
        {
            var conversation = @event.Conversation;

            if (conversation.Type != ConversationType.Support)
            {
                // Only conversation with support type should be processes
                return;
            }

            var employees = await GetEmployeesAsync(conversation);
            
            var users = employees.Select(x => x.User);
            
            var notification = new NewSupportTicketNotification(users, conversation);

            await _notificationService.CreateNotificationAsync(notification);
        }
        
        #region private

        /// <summary>
        /// Returns employees 
        /// </summary>
        /// <param name="conversation"></param>
        /// <returns></returns>
        private async Task<Employee[]> GetEmployeesAsync(Conversation conversation)
        {
            var practiceId = conversation.PracticeId;
            var patient = conversation.PatientParticipants.First().Patient;
            var locationId = patient.LocationId;

            var employees = await _employeeService.GetByRoleIdsAsync(
                practiceId: practiceId,
                new[] {locationId},
                roleIds: new []{ Roles.StaffId, Roles.AdminId, Roles.LocationDirectorId}
            );

            return employees.ToArray();
        }
        
        #endregion
    }
}