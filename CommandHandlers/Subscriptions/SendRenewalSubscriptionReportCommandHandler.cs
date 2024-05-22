using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Constants;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Subscriptions
{
    public class SendRenewalSubscriptionReportCommandHandler : IRequestHandler<SendRenewalSubscriptionReportCommand>
    {
        private const string Subject = "Renewal subscription report";

        private static readonly string[] EmailContainerSettings =
        {
            SettingsNames.Emails.AdminEmails,
            SettingsNames.General.ApplicationBaseUrl,
            SettingsNames.Emails.HeaderUrl,
            SettingsNames.Emails.LogoUrl,
            SettingsNames.Emails.WhiteLogoUrl,
            SettingsNames.Emails.WHLinkLogoUrl,
            SettingsNames.Emails.InstagramUrl,
            SettingsNames.Emails.WHInstagramLogoUrl
        };
        
        private readonly IEmailService _emailService;
        private readonly IEmailFactory _emailFactory;
        private readonly ISettingsManager _settingsManager;
        private readonly IPracticeService _practiceService;
        private readonly ILogger<SendRenewalSubscriptionReportCommandHandler> _logger;

        public SendRenewalSubscriptionReportCommandHandler(
            IEmailService emailService, 
            IEmailFactory emailFactory,
            ISettingsManager settingsManager,
            IPracticeService practiceService,
            ILogger<SendRenewalSubscriptionReportCommandHandler> logger)
        {
            _emailService = emailService;
            _emailFactory = emailFactory;
            _settingsManager = settingsManager;
            _practiceService = practiceService;
            _logger = logger;
        }
        
        public async Task Handle(SendRenewalSubscriptionReportCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation($"Start sending renewal subscription report for practice with id {request.PracticeId}");

                var practiceId = request.PracticeId;
                
                var practice = await _practiceService.GetOriginalPractice(practiceId);

                var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

                var email = await _emailFactory.Create(new EmailDataModel<RenewalSubscriptionReportEmailModel>
                {
                    Data = new RenewalSubscriptionReportEmailModel
                    {
                        RenewedSubscriptions = request.RenewedSubscription
                            .Select(c=> new RenewSubscriptionReportEmailModel
                            {
                                Email = c.Email,
                                Period = c.Period,
                                Price = c.Price,
                                Result = c.Result,
                                FirstName = c.FirstName,
                                LastName = c.LastName,
                                PaymentPlan = c.PaymentPlan
                            }),
                        PracticeName = practice.Name,
                        PracticeEmail = practice.Email,
                        PracticePhoneNumber = practice.PhoneNumber,
                        ApplicationUrl = settings[SettingsNames.General.ApplicationBaseUrl],
                        HeaderUrl = settings[SettingsNames.Emails.HeaderUrl],
                        LogoUrl = settings[SettingsNames.Emails.LogoUrl],
                        FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl],
                        WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl],
                        WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl],
                    InstagramUrl = settings[SettingsNames.Emails.InstagramUrl],
                        PracticeAddress = $"{practice.Address.Address1} " +
                                          $"{practice.Address.City} " +
                                          $"{practice.Address.State} " +
                                          $"{practice.Address.ZipCode}",
                        PracticeId = practiceId
                    }
                });

                var receiver = settings[SettingsNames.Emails.AdminEmails];
            
                await _emailService.SendAsync(
                    to: receiver, 
                    subject: Subject, 
                    body: email.Html, 
                    practiceId: request.PracticeId
                );
                
                _logger.LogInformation($"Sending renewal subscription report for practice {request.PracticeId} has been successfully finished");
            }
            catch (Exception e)
            {
                _logger.LogError($"Sending renewal subscription report for practice {request.PracticeId} is failed with [Error]: {e.ToString()}");
            }
        }
    }
}