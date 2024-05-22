using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using MediatR;
using WildHealth.Common.Constants;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class SendPaymentPlanOfferEmailCommandHandler : IRequestHandler<SendPaymentPlanOfferEmailCommand>
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

        public SendPaymentPlanOfferEmailCommandHandler(
            IEmailFactory emailFactory, 
            IEmailIntegrationService emailIntegrationService,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IOptions<AppOptions> appOptions)
        {
            _emailFactory = emailFactory;
            _emailIntegrationService = emailIntegrationService;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _appOptions = appOptions.Value;
        }

        public async Task Handle(SendPaymentPlanOfferEmailCommand command, CancellationToken cancellationToken)
        {
            var patient = command.Patient;
            var paymentPlan = command.PaymentPlan;
            var paymentPeriod = command.PaymentPeriod;
            var practiceId = command.PracticeId;
           
            bool ActivePrice(PaymentPrice x) => x.IsActive && !x.IsExclusive();
            
            var discount = paymentPeriod
                .Prices
                .Where(ActivePrice)
                .FirstOrDefault(x => x.Strategy == PaymentStrategy.FullPayment)?.GetDiscount() ?? 0;

            var startPrice = paymentPeriod
                .Prices
                .Where(ActivePrice)
                .Select(x => x.GetPrice())
                .Min();

            var practice = await _practiceService.GetOriginalPractice(command.PracticeId);

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, command.PracticeId);

            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            
            var offerUrl = string.Format(_appOptions.OfferPaymentPlanUrl, applicationUrl, paymentPlan.Id, paymentPeriod.Id);

            var email = await _emailFactory.Create(new EmailDataModel<OfferPaymentPlanEmailModel>
            {
                Data = new OfferPaymentPlanEmailModel
                {                    
                    OfferUrl = offerUrl,
                    FirstName = patient.User.FirstName,
                    PlanName = paymentPlan.DisplayName,
                    PeriodInMonths = paymentPeriod.PeriodInMonths,
                    Benefits = paymentPeriod.Benefits.Select(x => x.Title + x.Text).ToArray(),
                    StartPrice = startPrice,
                    Discount = discount,
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
                    PracticeId = practiceId

                },
            });

            var subject = $"{practice.Name} Plan Confirmation";

            await _emailIntegrationService.SendEmailAndEventAsync(
                patient.User.Email, 
                subject, 
                email.Html, 
                practiceId,
                nameof(OfferPaymentPlanEmailModel),
                patient.User
            );
        }
    }
}