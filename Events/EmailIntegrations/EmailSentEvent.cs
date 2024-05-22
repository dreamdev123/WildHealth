using MediatR;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Events.EmailIntegrations
{
    public class EmailSentEvent : INotification
    {
        public User User { get; }
        public string Subject { get; }
        public string EmailTemplateTypeName { get; }

        public EmailSentEvent(User user, string subject, string emailTemplateTypeName)
        {
            User = user;
            Subject = subject;
            EmailTemplateTypeName = emailTemplateTypeName;
        }
    }
}