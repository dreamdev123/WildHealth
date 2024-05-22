using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using WildHealth.Application.Services.Users;
using SendGrid;
using System;
using System.Net;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.Services.Emails
{
    public class EmailService : IEmailService
    {
        private static readonly string[] SettingsKeys =
        {
            SettingsNames.Emails.ApiKey,
            SettingsNames.Emails.FromEmail,
            SettingsNames.Emails.FromName,
            SettingsNames.Emails.CarbonCopy
        };
        
        private readonly ISettingsManager _settingsManager;
        private readonly IUsersService _usersService;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            ISettingsManager settingsManager,
            IUsersService usersService,
            ILogger<EmailService> logger
            )
        {
            _settingsManager = settingsManager;
            _usersService = usersService;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IEmailService.SendAsync"/>
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
        public Task<Response> SendAsync(string to,
            string subject,
            string body,
            int practiceId,
            IDictionary<string, string>? customArguments = null,
            Attachment[]? attachments = null,
            bool isPlainText = false,
            DateTime? sendAt = null,
            string? fromEmail = null,
            string? fromName = null)
        {
            var emails = new [] { to };
            
            return BroadcastAsync(
                to: emails, 
                subject: subject, 
                body: body, 
                practiceId: practiceId, 
                customArguments: customArguments,
                attachments: attachments, 
                isPlainText: isPlainText,
                showAllRecipients: isPlainText,
                sendAt: sendAt,
                fromEmail: fromEmail, fromName: fromName);

        }

        /// <summary>
        /// <see cref="IEmailService.BroadcastAsync"/>
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
        public async Task<Response> BroadcastAsync(IEnumerable<string> to,
            string subject,
            string body,
            int practiceId,
            IDictionary<string, string>? customArguments = null,
            Attachment[]? attachments = null,
            bool isPlainText = false,
            bool showAllRecipients = false,
            DateTime? sendAt = null,
            string? fromEmail = null,
            string? fromName = null)

        {
            var settings = await _settingsManager.GetSettings(SettingsKeys, practiceId);
            var senderEmail = fromEmail ?? settings[SettingsNames.Emails.FromEmail];
            var senderName = fromName ?? settings[SettingsNames.Emails.FromName];
            var apiKey = settings[SettingsNames.Emails.ApiKey];

            var from = new EmailAddress(senderEmail, senderName);
            
            var tos = to.Select(x => new EmailAddress(x)).ToList();
            var firstTo = to.First();

            var toUser = await _usersService.GetByEmailAsync(firstTo);
            var toUniversalId = toUser?.UniversalId;
            
            var client = new SendGridClient(apiKey);

            var message = MailHelper.CreateSingleEmailToMultipleRecipients(
                from: from, 
                tos: tos, 
                subject: subject,
                plainTextContent: isPlainText ? body : null,
                htmlContent:isPlainText ? null : body,
                showAllRecipients: showAllRecipients
            );
            
            // Standard custom arguments we always set
            message.AddCustomArg("Subject", subject);

            if(toUniversalId.HasValue)
            {
                message.AddCustomArg("toUniversalId", toUniversalId.Value.ToString());
            }

            // Add custom arguements passed in
            if(customArguments is not null)
            {
                foreach(var customArgument in customArguments)
                {
                    message.AddCustomArg(customArgument.Key, customArgument.Value);
                }
            }
            
            if (settings.ContainsKey(SettingsNames.Emails.CarbonCopy))
            {
                message.AddBcc(settings[SettingsNames.Emails.CarbonCopy]);
            }

            if (attachments != null && attachments.Any())
            {
                foreach (var attachment in attachments)
                {
                    message.AddAttachment(attachment);
                }
            }

            if (sendAt.HasValue)
            {
                message.SendAt = new DateTimeOffset(sendAt.Value).ToUnixTimeSeconds();
            }

            var response = await client.SendEmailAsync(message);
            if (response.StatusCode != HttpStatusCode.Accepted)
            {
                var getBody= await response.Body.ReadAsStringAsync().ToTry();
                var error = getBody.IsSuccess() ? getBody.SuccessValue() : String.Empty;
                var errorMessage = $"The response from sendgrid indicates a problem: {response.StatusCode} {error}";
                _logger.LogWarning(errorMessage);
            }
            return response;
        }
    }
}