using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Settings;
using MediatR;
using WildHealth.Application.Services.Appointments;

namespace WildHealth.Application.EventHandlers.Appointments
{
    public class SendConfirmationEmailOnAppointmentCreatedEvent : INotificationHandler<AppointmentCreatedEvent>
    {
        private const string Subject = "Appointment Confirmation";
        
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
        
        private readonly ILogger<SendConfirmationEmailOnAppointmentCreatedEvent> _logger;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly IEmailIntegrationService _emailIntegrationService;
        private readonly IEmailFactory _emailFactory;
        private readonly AppOptions _appOptions;
        private readonly IAppointmentsService _appointmentsService;
        
        public SendConfirmationEmailOnAppointmentCreatedEvent(            
            ILogger<SendConfirmationEmailOnAppointmentCreatedEvent> logger,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IOptions<AppOptions> appOptions, 
            IEmailIntegrationService emailIntegrationService, 
            IEmailFactory emailFactory, 
            IAppointmentsService appointmentsService)
        {
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _appOptions = appOptions.Value;
            _emailIntegrationService = emailIntegrationService;
            _emailFactory = emailFactory;
            _appointmentsService = appointmentsService;
            _logger = logger;
        }
        
        public async Task Handle(AppointmentCreatedEvent notification, CancellationToken cancellationToken)
        {
            var appointment = await _appointmentsService.GetByIdAsync(notification.AppointmentId);
            
            var appointmentDomain = AppointmentDomain.Create(appointment);
            
            if (appointment.Patient?.User is null)
            {
                return;
            }
            
            _logger.LogInformation($"Sending appointment confirmation with [Id] = {appointment.Id} for [{appointment.Patient.Id}]  has been started.");

            var practice = await _practiceService.GetOriginalPractice(appointmentDomain.GetPracticeId());

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, appointmentDomain.GetPracticeId());
            
            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            
            var loginLink = string.Format(_appOptions.LoginUrl, applicationUrl);
            
            var messagingLink = string.Format(_appOptions.ConversationUrl, applicationUrl);
            
            var model = new EmailDataModel<AppointmentConfirmationModel>
            {
                Data = new AppointmentConfirmationModel
                {
                    PracticeId = practice.Id,
                    PatientFirstName = appointment.Patient.User.FirstName,
                    JoinLink = appointment.JoinLink,
                    Start = appointmentDomain.GetTimeZoneStartTime(forPatient: true),
                    Timezone = appointmentDomain.GetTimezoneDisplayName(forPatient: true),
                    ClarityUrl = loginLink,
                    AppointmentType = appointment.Name,
                    ClarityMessagingUrl = messagingLink,
                    EmployeeNames = appointmentDomain.GetEmployeeNames(),
                    ProviderFullName = appointmentDomain.GetProviderName(),
                    HealthCoachFullName = appointmentDomain.GetCoachName(),
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
                    PracticeAddress = $"{practice.Address?.Address1} " +
                                      $"{practice.Address?.City} " +
                                      $"{practice.Address?.State} " +
                                      $"{practice.Address?.ZipCode}"
                }
            };
            
            var email = await _emailFactory.Create(model);

            await _emailIntegrationService.SendEmailAndEventAsync(
                to: appointment.Patient.User.Email,
                subject: Subject,
                body: email.Html,
                practiceId: practice.Id,
                emailTemplateTypeName: nameof(AppointmentConfirmationModel),
                user: appointment.Patient.User
            );
            
            _logger.LogInformation($"Sending appointment reminder with [Id] = {appointment.Id} for [{appointment.Patient.Id}] has been successfully finished");
        }
    }
}