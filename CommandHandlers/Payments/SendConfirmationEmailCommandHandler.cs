using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Application.Services.Practices;
using PracticeModel = WildHealth.Licensing.Api.Models.Practices.PracticeModel;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MediatR;
using WildHealth.Domain.Models.Subscriptions;

namespace WildHealth.Application.CommandHandlers.Payments
{
    public class SendConfirmationEmailCommandHandler : IRequestHandler<SendConfirmationEmailCommand>
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
        
        private readonly IEmailFactory _emailFactory;
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly AppOptions _appOptions;
        private readonly ILogger<SendConfirmationEmailCommandHandler> _logger;

        public SendConfirmationEmailCommandHandler(
            IEmailFactory emailFactory,
            IEmailIntegrationService emailIntegrationService,
            IPracticeService practiceService, 
            ISettingsManager settingsManager,
            IOptions<AppOptions> appOptions,
            ILogger<SendConfirmationEmailCommandHandler> logger)
        {
            _emailFactory = emailFactory;
            _emailIntegrationService = emailIntegrationService;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _appOptions = appOptions.Value;
            _logger = logger;
        }

        public async Task Handle(SendConfirmationEmailCommand command, CancellationToken cancellationToken)
        {
            var practiceId = command.Patient.User.PracticeId;
            
            var practice = await _practiceService.GetOriginalPractice(command.Patient.User.PracticeId);

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var items = new List<InvoiceItemEmailModel>
            {
                new()
                {
                    Quantity = 1,
                    Name = command.NewSubscription.PaymentPrice.PaymentPeriod.PaymentPlan.DisplayName,
                    Price = command.NewSubscription.Price
                },
                new()
                {
                    Quantity = 1,
                    Name = "Startup fee",
                    Price = command.NewSubscription.PaymentPrice.GetStartupFee()
                }
            };

            var orders = command.Patient.Orders
                .SelectMany(x => x.Items)
                .Where(x => command.PatientAddOnIds.Contains(x.AddOnId))
                .Select(x => x.Order);
            
            foreach (var order in orders)
            {
                foreach (var item in order.Items)
                {
                    var addOn = item.AddOn;
                 
                    items.Add(new InvoiceItemEmailModel
                    {
                        Quantity = 1,
                        Name = addOn.Name,
                        Price = addOn.GetPrice()
                    });
                }
            }

            // We don't want to send "Receipt" email to Premium patients
            if(command.PreviousSubscription.IsPremium())
                return;
            
            var emailInstance = GetEmailDataInstance(command.PreviousSubscription);

            if (emailInstance is null)
            {
                _logger.LogError($"Plan confirmation Email for patient with [Id] = {command.Patient.Id} was failed.");
                return;
            }

            var email = await _emailFactory.Create(new EmailDataModel<PlanConfirmationBaseModel>
            {
                Data = FillEmailData(
                    model: emailInstance,
                    patient: command.Patient,
                    practice: practice,
                    settings: settings,
                    subscription: command.NewSubscription,
                    items: items.ToArray()),
            });

            var subject = GetSubject(command.PreviousSubscription, practice);

            await _emailIntegrationService.SendEmailAndEventAsync(
                command.Patient.User.Email, 
                subject, 
                email.Html, 
                command.Patient.User.PracticeId,
                nameof(PlanConfirmationBaseModel),
                command.Patient.User
            );

            _logger.LogInformation($"Plan confirmation Email for patient with [Id] = {command.Patient.Id} was sent.");
        }

        private bool IsNewSubscription(Subscription subscription)
        {
            return subscription is null ||
                subscription.PaymentPrice.PaymentPeriod.PaymentPlan.IsTrial ||
                subscription.PaymentPrice.IsNotIntegratedPlan();
        }

        private string GetSubject(Subscription subscription, PracticeModel practice)
        {
            if (IsNewSubscription(subscription))
            {
                //return "Clarity Plan Confirmation and Receipt";
                return $"${practice.Name} Plan Confirmation and Receipt";
            }
            
            return $"{practice.Name} Renewal Confirmation";
        }

        private PlanConfirmationBaseModel GetEmailDataInstance(Subscription subscription)
        {
            if (subscription is null ||
                subscription.PaymentPrice.PaymentPeriod.PaymentPlan.IsTrial)
            {
                return new PlanConfirmationAfterFreeTrialEmailModel();
            }

            if (subscription.PaymentPrice.IsNotIntegratedPlan())
            {
                return new PlanConfirmationAfterNonClarityPlanEmailModel();
            }

            return new RenewalConfirmationEmailModel();
        }

        private PlanConfirmationBaseModel FillEmailData(
            PlanConfirmationBaseModel model, 
            Patient patient,
            PracticeModel practice,
            IDictionary<string, string> settings,
            Subscription subscription,
            InvoiceItemEmailModel[] items)
        {
            var appUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            var totalPrice = items.Select(x => x.Price).Sum();
            var questionnaireLink = string.Format(_appOptions.HealthQuestionnaireUrl, appUrl, patient.IntakeId);

            model.ApplicationUrl = appUrl;
            model.HeaderUrl = settings[SettingsNames.Emails.HeaderUrl];
            model.LogoUrl = settings[SettingsNames.Emails.LogoUrl];
            model.FooterLogoUrl = settings[SettingsNames.Emails.WhiteLogoUrl];
            model.WHLinkLogoUrl = settings[SettingsNames.Emails.WHLinkLogoUrl];
            model.WHInstagramLogoUrl = settings[SettingsNames.Emails.WHInstagramLogoUrl];
            model.PracticeName = practice.Name;
            model.PracticeEmail = practice.Email;
            model.PracticePhoneNumber = practice.PhoneNumber;
            model.PracticeAddress = $"{practice.Address.Address1} " +
                                    $"{practice.Address.City} " +
                                    $"{practice.Address.State} " +
                                    $"{practice.Address.ZipCode}";
            
            model.PracticeId = practice.Id;
            model.FirstName = patient.User.FirstName;
            model.IsTrial = subscription.PaymentPrice.PaymentPeriod.PaymentPlan.IsTrial;
            model.PaymentPlan = subscription.PaymentPrice.PaymentPeriod.PaymentPlan.DisplayName;
            model.PeriodInMonths = subscription.PaymentPrice.PaymentPeriod.PeriodInMonths;
            model.Price = subscription.Price;
            model.PaidInFull = subscription.PaymentStrategy == PaymentStrategy.FullPayment;
            model.SubscriptionChargeDate = subscription.StartDate;
            model.DaysUntilCharge = (int)(subscription.StartDate - DateTime.UtcNow.Date).TotalDays;
            model.IntakeFormLink = questionnaireLink;
            model.Invoice = new InvoiceEmailModel
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
                PracticeId = practice.Id
            };

            return model;
        }
    }
}
