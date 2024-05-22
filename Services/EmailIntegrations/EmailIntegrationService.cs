using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Events.EmailIntegrations;
using WildHealth.Domain.Entities.Users;
using MediatR;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace WildHealth.Application.Services.EmailIntegrations
{
    public class EmailIntegrationService : IEmailIntegrationService
    {
        private readonly IEmailService _emailService; 
        private readonly IMediator _mediator;
        
        public EmailIntegrationService(
            IEmailService emailService,
            IMediator mediator)
        {
            _emailService = emailService;
            _mediator = mediator;
        }


        /// <summary>
        /// <see cref="IEmailIntegrationService.SendEmailAndEventAsync"/>
        /// </summary>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="practiceId"></param>
        /// <param name="emailTemplateTypeName"></param>
        /// <param name="user"></param>
        /// <param name="attachments"></param>
        /// <param name="isPlainText"></param>
        /// <returns></returns>
        public async Task<Response> SendEmailAndEventAsync(string to,
            string subject,
            string body,
            int practiceId,
            string emailTemplateTypeName,
            User? user,
            Attachment[]? attachments = null,
            bool isPlainText = false)
        {
            if (user is not null)
            {
                await _mediator.Publish(new EmailSentEvent(
                    user: user,
                    subject: subject,
                    emailTemplateTypeName: emailTemplateTypeName
                ));
            }

            return await _emailService.SendAsync(
                to: to, 
                subject: subject, 
                body: body, 
                practiceId: practiceId, 
                customArguments: new Dictionary<string, string>() { 
                    {"emailTemplateType", emailTemplateTypeName}
                },
                attachments: attachments, 
                isPlainText: isPlainText
            );
        }

        /// <summary>
        /// <see cref="IEmailIntegrationService.BroadcastEmailAndEventAsync"/>
        /// </summary>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="practiceId"></param>
        /// <param name="emailTemplateTypeName"></param>
        /// <param name="users"></param>
        /// <param name="customArguments"></param>
        /// <param name="attachments"></param>
        /// <param name="isPlainText"></param>
        /// <returns></returns>
        public async Task<Response> BroadcastEmailAndEventAsync(IEnumerable<string> to,
            string subject,
            string body,
            int practiceId,
            string emailTemplateTypeName,
            IEnumerable<User> users,
            IDictionary<string, string> customArguments,
            Attachment[]? attachments = null,
            bool isPlainText = false)
        {
            foreach(var user in users)
            {
                if (user is null)
                {
                    continue;
                }
                
                await _mediator.Publish(new EmailSentEvent(
                    user: user,
                    subject: subject,
                    emailTemplateTypeName: emailTemplateTypeName
                ));
            }

            return await _emailService.BroadcastAsync(
                to: to, 
                subject: subject, 
                body: body, 
                practiceId: practiceId, 
                customArguments: customArguments,
                attachments: attachments, 
                isPlainText: isPlainText
            );
        }        
    }
}