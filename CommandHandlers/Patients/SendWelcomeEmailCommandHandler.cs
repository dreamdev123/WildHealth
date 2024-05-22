using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.EventHandlers.Patients;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Common.Options;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MediatR;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Models.Subscriptions;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Licensing.Api.Models.Practices;
using WildHealth.Shared.Data.Helpers;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class SendWelcomeEmailCommandHandler : IRequestHandler<SendWelcomeEmailCommand>
    {
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
        
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly IEmailFactory _emailFactory;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly AppOptions _appOptions;
        private readonly ILogger _logger;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IPatientsService _patientService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IOptions<PracticeOptions> _options;

        public SendWelcomeEmailCommandHandler(
            IEmailIntegrationService emailIntegrationService,
            IEmailFactory emailFactory,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IOptions<AppOptions> appOptions,
            ILogger<SendWelcomeEmailOnPatientCreatedEvent> logger,
            IFeatureFlagsService featureFlagsService,
            IPatientsService patientService,
            ISubscriptionService subscriptionService,
            IOptions<PracticeOptions> options)
        {
            _emailIntegrationService = emailIntegrationService;
            _emailFactory = emailFactory;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _appOptions = appOptions.Value;
            _logger = logger;
            _featureFlagsService = featureFlagsService;
            _patientService = patientService;
            _subscriptionService = subscriptionService;
            _options = options;
        }

        public async Task Handle(SendWelcomeEmailCommand command, CancellationToken cancellationToken)
        {
            var patient =
                await _patientService.GetByIdAsync(command.PatientId, PatientSpecifications.NewEnrollmentNotificationSpecification);
            var practiceId = patient.User.PracticeId;
            var subscription = await _subscriptionService.GetAsync(command.SubscriptionId);
            var selectedAddOnIds = command.SelectedAddOnIds;
            var practice = await _practiceService.GetOriginalPractice(practiceId);

            var items = new List<InvoiceItemEmailModel>
            {
                new()
                {
                    Quantity = 1,
                    Name = subscription.PaymentPrice.PaymentPeriod.PaymentPlan.DisplayName,
                    Price = subscription.Price
                },
                new()
                {
                    Quantity = 1,
                    Name = "Startup fee",
                    Price = subscription.StartupFee ?? 0
                }
            };
            items.AddRange(patient.Orders.SelectMany(order => order.Items, (_, item) => item.AddOn)
                .Where(addOn => selectedAddOnIds.Contains(addOn.GetId()))
                .Select(addOn => new InvoiceItemEmailModel {Quantity = 1, Name = addOn.Name, Price = addOn.GetPrice()}));

            var totalPrice = items.Select(x => x.Price).Sum();
            var invoiceEmail = new InvoiceEmailModel
            {
                Date = subscription.CreatedAt,
                OrderNumber = subscription.Id.ToString(),
                Items = items.ToArray(),
                Summary = new InvoiceSummaryEmailModel
                {
                    TotalPrice = totalPrice,
                    SubTotalPrice = totalPrice,
                    TotalTax = 0
                },
                PracticeId = practiceId
            };

            if (practiceId != _options.Value.WildHealth)
            {
                await SendWelcomeEmail(invoiceEmail, practice, patient, subscription);
                return;
            }
            
            if (_featureFlagsService.GetFeatureFlag(FeatureFlags.WelcomeEmailTransactional) && !subscription.IsPremium()) // We don't want to send "Receipt" email to Premium patients
            {
                await SendOnlyInvoice(invoiceEmail, patient);
            }
            else
            {
                await SendWelcomeEmail(invoiceEmail, practice, patient, subscription);
            }
        }

        private async Task SendOnlyInvoice(InvoiceEmailModel invoiceEmail, Patient patient)
        {
            var email = await _emailFactory.Create(new EmailDataModel<InvoiceEmailModel>
            {
                Data = invoiceEmail
            });

            await _emailIntegrationService.SendEmailAndEventAsync(
                to: patient.User.Email,
                subject: "Wild Health Invoice",
                body: email.Html,
                practiceId: patient.User.PracticeId,
                emailTemplateTypeName: nameof(WelcomeEmailModel),
                user: patient.User
            );

            _logger.LogInformation($"Altered Welcome Email (invoice only) for patient with [Id] = {patient.Id} was sent.");
        }

        private async Task SendWelcomeEmail(InvoiceEmailModel invoiceEmail, PracticeModel practice, Patient patient, Subscription subscription)
        {
            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practice.Id);
            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            var questionnaireLink = string.Format(_appOptions.HealthQuestionnaireUrl, applicationUrl, patient.IntakeId);
            var loginLink = string.Format(_appOptions.LoginUrl, applicationUrl);
            var email = await _emailFactory.Create(new EmailDataModel<WelcomeEmailModel>
                {
                    Data = new WelcomeEmailModel
                    {
                        FirstName = patient.User.FirstName,
                        IsTrial = subscription.PaymentPrice.PaymentPeriod.PaymentPlan.IsTrial,
                        PaymentPlan = subscription.PaymentPrice.PaymentPeriod.PaymentPlan.DisplayName,
                        PeriodInMonths = subscription.PaymentPrice.PaymentPeriod.PeriodInMonths,
                        Price = subscription.Price,
                        PaidInFull = subscription.PaymentStrategy == PaymentStrategy.FullPayment,
                        SubscriptionChargeDate = subscription.StartDate,
                        DaysUntilCharge = (int) (subscription.StartDate - DateTime.UtcNow.Date).TotalDays,
                        IntakeFormLink = questionnaireLink,
                        LoginLink = loginLink,
                        Invoice = invoiceEmail,
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
                });

                var subject = practice.Id == (int)PlanPlatform.CrossFit 
                    ? $"Your {practice.Name} receipt" 
                    : $"Welcome to {practice.Name}";;

                await _emailIntegrationService.SendEmailAndEventAsync(
                    to: patient.User.Email,
                    subject: subject,
                    body: email.Html,
                    practiceId: practice.Id,
                    emailTemplateTypeName: nameof(WelcomeEmailModel),
                    user: patient.User
                );

                _logger.LogInformation($"Welcome Email for patient with [Id] = {patient.Id} was sent.");
        }
    }
}