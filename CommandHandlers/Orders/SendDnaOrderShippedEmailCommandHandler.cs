using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Practices;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class SendDnaOrderShippedEmailCommandHandler : IRequestHandler<SendDnaOrderShippedEmailCommand>
    {
        private const string Subject = "Your DNA kit has shipped";
        
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

        private readonly IPatientsService _patientService;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly IEmailFactory _emailFactory;
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly ILogger _logger;

        public SendDnaOrderShippedEmailCommandHandler(
            IPatientsService patientService, 
            IPracticeService practiceService, 
            ISettingsManager settingsManager,
            IEmailFactory emailFactory, 
            IEmailIntegrationService emailIntegrationService, 
            ILogger<SendDnaOrderShippedEmailCommandHandler> logger)
        {
            _patientService = patientService;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _emailFactory = emailFactory;
            _emailIntegrationService = emailIntegrationService;
            _logger = logger;
        }

        public async Task Handle(SendDnaOrderShippedEmailCommand command, CancellationToken cancellationToken)
        {
            var order = command.Order;
            
            _logger.LogInformation($"Sending of DnaOrderShipped email for order with id {order.Id} has been started.");

            var practiceId = order.PracticeId;
            var patient = await _patientService.GetByIdAsync(order.PatientId);
            var practice = await _practiceService.GetOriginalPractice(practiceId);
            var settings = await _settingsManager.GetSettings(EmailContainerSettings, practiceId);

            var email = await _emailFactory.Create(new EmailDataModel<DnaOrderShippedEmailModel>
            {
                Data = new DnaOrderShippedEmailModel
                {
                    PracticeId = practice.Id,
                    PatientShippingNumber = order.PatientShippingNumber,
                    PatientFirstName = patient.User.FirstName,
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
                                      $"{practice.Address.ZipCode}"
                }
            });

            await _emailIntegrationService.SendEmailAndEventAsync(
                patient.User.Email, 
                Subject, 
                email.Html, 
                practice.Id,
                nameof(DnaOrderShippedEmailModel),
                patient.User
            );
                
            _logger.LogInformation($"Sending of DnaOrderShipped email for order with id {order.Id} has been finished.");
        }
    }
}