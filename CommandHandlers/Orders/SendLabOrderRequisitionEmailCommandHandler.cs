using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.Practices;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Models.Orders;
using WildHealth.Licensing.Api.Models.Practices;
using WildHealth.Common.Constants;
using WildHealth.Application.Services.Orders.Lab;
using System.Text.RegularExpressions;
using WildHealth.Application.Extensions.BlobFiles;
using SendGrid.Helpers.Mail;
using WildHealth.Settings;
using PdfiumViewer;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class SendLabOrderRequisitionEmailCommandHandler : IRequestHandler<SendLabOrderRequisitionEmailCommand>
    {
        private const string Subject = "Lab Requisition and Instructions";
               
        private static readonly string[] EmailContainerSettings =
        {
            SettingsNames.General.ApplicationBaseUrl,
            SettingsNames.Emails.HeaderUrl,
            SettingsNames.Emails.LogoUrl,
            SettingsNames.Emails.WhiteLogoUrl,
            SettingsNames.Emails.WHLinkLogoUrl,
            SettingsNames.Emails.InstagramUrl,
            SettingsNames.Emails.WHInstagramLogoUrl
        };

        private readonly ILabOrdersService _labOrdersService;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly IEmailFactory _emailFactory;
        private readonly IEmailService _emailService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public SendLabOrderRequisitionEmailCommandHandler(
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IEmailFactory emailFactory, 
            IEmailService emailService, 
            IMediator mediator,
            ILabOrdersService labOrdersService,
            ILogger<SendLabOrderRequisitionEmailCommandHandler> logger)
        {
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _emailFactory = emailFactory;
            _emailService = emailService;
            _mediator = mediator;
            _logger = logger;
            _labOrdersService = labOrdersService;
        }

        public async Task Handle(SendLabOrderRequisitionEmailCommand command, CancellationToken cancellationToken)
        {
            var order = command.Order;

            var patient = order.Patient;

            var practiceId = order.PracticeId;

            _logger.LogInformation($"Sending lab order requisition email for order with [Id] = {order.GetId()} has been started.");

            var practice = await _practiceService.GetOriginalPractice(practiceId);

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var attachments = await GetAttachmentsAsync(order);

            await ParseExpectedDateAsync(order, attachments[0]);
            
            var email = order.Provider switch
            {
                AddOnProvider.LabCorp => await CreateLabCorpEmail(order, practice, settings),
                AddOnProvider.Quest => await CreateQuestEmail(order, practice, settings),
                AddOnProvider.Boston => await CreateBostonEmail(order, practice, settings),
                
                _ => throw new ArgumentException("Unsupported add-on provider.")
            };

            await _emailService.SendAsync(
                to: patient.User.Email, 
                subject: Subject, 
                body: email.Html, 
                practiceId: patient.User.PracticeId, 
                attachments: attachments
            );
            
            _logger.LogInformation($"Sending lab order requisition email for order with [Id] = {order.GetId()} has been finished.");
        }
        
        #region private

        /// <summary>
        /// Downloads order requisition file and prepare email attachments array
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private async Task<Attachment[]> GetAttachmentsAsync(LabOrder order)
        {
            var downloadRequisitionCommand = new DownloadLabOrderRequisitionCommand(order.GetId());
            
            var (bytes, filename) = await _mediator.Send(downloadRequisitionCommand);
            
            return new[]
            {
                new Attachment
                {
                    Content = Convert.ToBase64String(bytes),
                    Filename = filename,
                    Type = filename.DeterminateContentType(),
                    Disposition = "attachment"
                }
            };
        }
        
        /// <summary>
        /// Creates and returns lab corp email
        /// </summary>
        /// <param name="order"></param>
        /// <param name="practice"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private async Task<EmailDataResult> CreateLabCorpEmail(LabOrder order, PracticeModel practice, IDictionary<string, string> settings)
        {
            var model = new EmailDataModel<LabCorpRequisitionEmailModel>
            {
                Data = new LabCorpRequisitionEmailModel
                {
                    FirstName = order.Patient.User.FirstName,
                    CollectionDate = (order.ExpectedCollectionDate ?? DateTime.UtcNow).Date,
                    Instructions = GetAddOnsInstructions(order),
                    PracticeName = practice.Name,
                    PracticeEmail = practice.Email,
                    ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl],
                    HeaderUrl = settings[SettingsNames.Emails.HeaderUrl],
                    LogoUrl = settings[SettingsNames.Emails.LogoUrl],
                    FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl],
                    WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl],
                    WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl],
                    InstagramUrl = settings[SettingsNames.Emails.InstagramUrl],
                    PracticePhoneNumber = practice.PhoneNumber,
                    PracticeAddress = $"{practice.Address.Address1} " +
                                      $"{practice.Address.City} " +
                                      $"{practice.Address.State} " +
                                      $"{practice.Address.ZipCode}",
                    PracticeId = practice.Id
                }
            };
            
            return await _emailFactory.Create(model);
        }
        
        /// <summary>
        /// Creates and returns quest email
        /// </summary>
        /// <param name="order"></param>
        /// <param name="practice"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private async Task<EmailDataResult> CreateQuestEmail(LabOrder order, PracticeModel practice, IDictionary<string, string> settings)
        {
            var model = new EmailDataModel<QuestRequisitionEmailModel>
            {
                Data = new QuestRequisitionEmailModel
                {
                    FirstName = order.Patient.User.FirstName,
                    CollectionDate = (order.ExpectedCollectionDate ?? DateTime.UtcNow).Date,
                    Instructions = GetAddOnsInstructions(order),
                    PracticeName = practice.Name,
                    PracticeEmail = practice.Email,
                    ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl],
                    HeaderUrl = settings[SettingsNames.Emails.HeaderUrl],
                    LogoUrl = settings[SettingsNames.Emails.LogoUrl],
                    FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl],
                    WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl],
                    WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl],
                    InstagramUrl = settings[SettingsNames.Emails.InstagramUrl],
                    PracticePhoneNumber = practice.PhoneNumber,
                    PracticeAddress = $"{practice.Address.Address1} " +
                                      $"{practice.Address.City} " +
                                      $"{practice.Address.State} " +
                                      $"{practice.Address.ZipCode}",
                    PracticeId = practice.Id
                }
            };
            
            return await _emailFactory.Create(model);
        }
        
        /// <summary>
        /// Creates and returns boston email
        /// </summary>
        /// <param name="order"></param>
        /// <param name="practice"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private async Task<EmailDataResult> CreateBostonEmail(LabOrder order, PracticeModel practice, IDictionary<string, string> settings)
        {
            var model = new EmailDataModel<BostonRequisitionEmailModel>
            {
                Data = new BostonRequisitionEmailModel
                {
                    PracticeId = practice.Id,
                    FirstName = order.Patient.User.FirstName,
                    Instructions = GetAddOnsInstructions(order),
                    PracticeName = practice.Name,
                    PracticeEmail = practice.Email,
                    ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl],
                    HeaderUrl = settings[SettingsNames.Emails.HeaderUrl],
                    LogoUrl = settings[SettingsNames.Emails.LogoUrl],
                    PracticePhoneNumber = practice.PhoneNumber,
                    FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl],
                    WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl],
                    WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl],
                    InstagramUrl = settings[SettingsNames.Emails.InstagramUrl],
                    PracticeAddress = $"{practice.Address.Address1} " +
                                      $"{practice.Address.City} " +
                                      $"{practice.Address.State} " +
                                      $"{practice.Address.ZipCode}"
                }
            };
            
            return await _emailFactory.Create(model);
        }

        /// <summary>
        /// Parse html file and get from that expected date 
        /// </summary>
        /// <param name="order"></param>
        /// <param name="attachment"></param>
        /// <returns></returns>
        private async Task ParseExpectedDateAsync( LabOrder order, Attachment attachment)
        {
            var orderDomain = OrderDomain.Create(order);
            var expectedDate = attachment.Type switch
            {
                "text/html" => ExtractExpectedDateFromHtmlFile(attachment),
                "application/pdf" => ExtractExpectedDateFromPdfFile(attachment),
                _ => null
            };
            
            orderDomain.SetExpectedCollectionDate(expectedDate ?? order.PlacedAt ?? DateTime.UtcNow);

            await _labOrdersService.UpdateAsync(order);
        }

        private string[] GetAddOnsInstructions(Order order)
        {
            return order.Items.Select(x => x.AddOn.Instructions).Where(x => !string.IsNullOrEmpty(x)).ToArray();
        }

        private DateTime? ExtractExpectedDateFromHtmlFile(Attachment attachment)
        {
            try
            {
                var base64EncodedBytes = Convert.FromBase64String(attachment.Content);
                
                var htmlString = Encoding.UTF8.GetString(base64EncodedBytes);

                const string pattern = @"expected_coll_date: '(\d{1,2}/\d{1,2}/\d{4})'";

                var reg = new Regex(pattern);

                var item = reg.Match(htmlString).Groups[1].Value;

                return DateTime.Parse(item);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Extract expected date from HTML file has failed with [Error]: {e.ToString()}");
                return null;
            }
        }
        
        private DateTime? ExtractExpectedDateFromPdfFile(Attachment attachment)
        {
            try
            {
                var base64EncodedBytes = Convert.FromBase64String(attachment.Content);
                using var stream = new MemoryStream(base64EncodedBytes);
                using var pdfDocument = PdfDocument.Load(stream);

                var sb = new StringBuilder();

                for (var i = 0; i < pdfDocument.PageCount; i++)
                {
                    sb.Append(pdfDocument.GetPdfText(i));
                }
            
                var text = sb.ToString();

                const string pattern = @"Expected: (\d{1,2}/\d{1,2}/\d{4})";
                
                var reg = new Regex(pattern);

                var item = reg.Match(text).Groups[1].Value;

                return DateTime.Parse(item);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Extract expected date from PDF file has failed with [Error]: {e.ToString()}");
                return null;
            }
        }
        
        #endregion
    }
}