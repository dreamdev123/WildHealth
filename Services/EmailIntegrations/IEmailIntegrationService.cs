using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Users;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace WildHealth.Application.Services.EmailIntegrations
{
    /// <summary>
    /// Provides method to send email and publish integration event on send
    /// </summary>
    public interface IEmailIntegrationService
    {
        /// <summary>
        /// Send email to single receiver and sends integration event
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
        Task<Response> SendEmailAndEventAsync(string to,
            string subject,
            string body,
            int practiceId,
            string emailTemplateTypeName,
            User? user = null,
            Attachment[]? attachments = null,
            bool isPlainText = false);

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
        Task<Response> BroadcastEmailAndEventAsync(IEnumerable<string> to,
            string subject,
            string body,
            int practiceId,
            string emailTemplateTypeName,
            IEnumerable<User> users,
            IDictionary<string, string> customArguments,
            Attachment[]? attachments = null,
            bool isPlainText = false);
    }
}