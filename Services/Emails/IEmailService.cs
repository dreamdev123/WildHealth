using System.Collections.Generic;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;
using SendGrid;
using System;

namespace WildHealth.Application.Services.Emails
{
    /// <summary>
    /// Provides methods for sending emails
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Send email to single receiver
        /// </summary>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="practiceId"></param>
        /// <param name="customArguments"></param>
        /// <param name="attachments"></param>
        /// <param name="isPlainText"></param>
        /// <param name="sendAt"></param>
        /// <param name="fromEmail"></param>
        /// <param name="fromName"></param>
        /// <returns></returns>
        Task<Response> SendAsync(string to,
            string subject,
            string body,
            int practiceId,
            IDictionary<string, string>? customArguments = null,
            Attachment[]? attachments = null,
            bool isPlainText = false,
            DateTime? sendAt = null,
            string? fromEmail = null,
            string? fromName = null);

        /// <summary>
        /// Send email to multiple receivers
        /// </summary>
        /// <param name="to"></param>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="practiceId"></param>
        /// <param name="customArguments"></param>
        /// <param name="attachments"></param>
        /// <param name="isPlainText"></param>
        /// <param name="showAllRecipients"></param>
        /// <param name="sendAt"></param>
        /// <param name="fromEmail"></param>
        /// <param name="fromName"></param>
        /// <returns></returns>
        Task<Response> BroadcastAsync(IEnumerable<string> to,
            string subject,
            string body,
            int practiceId,
            IDictionary<string, string>? customArguments = null,
            Attachment[]? attachments = null,
            bool isPlainText = false,
            bool showAllRecipients = false,
            DateTime? sendAt = null,
            string? fromEmail = null,
            string? fromName = null);
    }
}