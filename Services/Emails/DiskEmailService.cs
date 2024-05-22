using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SendGrid.Helpers.Mail;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using SendGrid;
using System;
using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace WildHealth.Application.Services.Emails
{
    /// <summary>
    /// Implements IEmailService, but never actually sends the emails.
    /// Instead, it writes them to disk in configured path.
    /// </summary>
    public class DiskEmailService : IEmailService
    {

        public ISettingsManager SettingsManager { get; }
        public string Path { get; }

        private static readonly string[] SettingsKeys =
        {
            SettingsNames.Emails.ApiKey,
            SettingsNames.Emails.FromEmail,
            SettingsNames.Emails.FromName,
            SettingsNames.Emails.CarbonCopy
        };
        
        public DiskEmailService (ISettingsManager settingsManager, string path) 
        {
            SettingsManager = settingsManager;
            Path = path;
        }

        public Task<Response> BroadcastAsync(IEnumerable<string> to,
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
            var tempname = System.IO.Path.GetRandomFileName();
            var tempfile = $"{subject.Replace(" ", "-").Replace(":", "-").ToLower()}-{practiceId}-{tempname}";
            var sep = System.IO.Path.DirectorySeparatorChar;
            var fullPath = $"{Path}{sep}{tempfile}";
            System.IO.Directory.CreateDirectory(fullPath);

            var settings = SettingsManager.GetSettings(SettingsKeys, practiceId).Result;
            var senderEmail = fromEmail ?? settings[SettingsNames.Emails.FromEmail];
            var senderName = fromName ?? settings[SettingsNames.Emails.FromName];

            var bodyPath =  $"{fullPath}{sep}emailBody.html";

            IEnumerable<string> attachs = attachments == null ? Enumerable.Empty<string>() : attachments.Select(a => a.Filename);

            var emailInfo = new {
                To = String.Join(", ", to),
                From = $"{senderName} ({senderEmail})",
                Subject = $"{subject}",
                PracticeId = practiceId,
                IsPlainText = isPlainText,
                ShowAllRecipients = showAllRecipients,
                ContentsPath = bodyPath,
                Attachments = attachs
            };
            var infoStr = JsonConvert.SerializeObject(emailInfo, Formatting.Indented);
            var infoPath = $"{fullPath}{sep}info.json";

            var t = Task.Run(() => 
            {
                File.WriteAllText(infoPath, infoStr);
                File.WriteAllText(bodyPath, body);
                if(attachments != null) 
                {
                    foreach(var a in attachments) {
                        var bytes = Encoding.UTF8.GetBytes(a.Content);
                        if(String.IsNullOrEmpty(a.Filename)) {
                            File.WriteAllBytes($"{fullPath}{sep}{System.IO.Path.GetRandomFileName()}", bytes);
                        } else {
                            File.WriteAllBytes($"{fullPath}{sep}{a.Filename}", bytes);
                        }
                    }
                }
                var resBody = new System.Net.Http.StringContent($"{fullPath}");
                //var headers = new System.Net.Http.Headers.HttpResponseHeaders();
                return new Response(System.Net.HttpStatusCode.OK, resBody, null);
            });
            return t;
        }

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
                fromEmail: fromEmail,
                fromName: fromName
            );
        }
    }
}