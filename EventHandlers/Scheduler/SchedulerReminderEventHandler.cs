using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Scheduler;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.SMS;
using WildHealth.Common.Constants;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Licensing.Api.Models.Practices;
using WildHealth.Application.Domain.AppointmentReminder;
using WildHealth.Application.Domain.AppointmentReminder.Bot;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Links;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PhoneLookupRecords;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Chatbots;
using WildHealth.Domain.Entities.Users;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.EventHandlers.Scheduler
{
    public class SchedulerReminderEventHandler : INotificationHandler<SchedulerReminderEvent>
    {
        private const string Subject = "Upcoming Appointment";
        
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
        private readonly ILogger<SchedulerReminderEventHandler> _logger;
        private readonly IAppointmentsService _appointmentsService;
        private readonly IPracticeService _practiceService;
        private readonly ISettingsManager _settingsManager;
        private readonly IEmailFactory _emailFactory;
        private readonly ISMSService _smsService;
        private readonly ILinkShortenService _linkShortenService;
        private readonly IAppointmentReminderService _appointmentReminderService;
        private readonly IFlowMaterialization _materializer;
        private readonly IPatientProfileService _patientProfileService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IUsersService _usersService;
        private readonly IPhoneLookupRecordService _lookupRecordService;

        public SchedulerReminderEventHandler(
            IEmailIntegrationService emailIntegrationService, 
            ILogger<SchedulerReminderEventHandler> logger,
            IAppointmentsService appointmentsService,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            ISMSService smsService,
            IEmailFactory emailFactory,
            ILinkShortenService linkShortenService, 
            IAppointmentReminderService appointmentReminderService, 
            IFlowMaterialization materializer, 
            IPatientProfileService patientProfileService, 
            IDateTimeProvider dateTimeProvider,
            IUsersService usersService, 
            IPhoneLookupRecordService lookupRecordService)
        {
            _emailIntegrationService = emailIntegrationService;
            _appointmentsService = appointmentsService;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _emailFactory = emailFactory;
            _smsService = smsService;
            _logger = logger;
            _linkShortenService = linkShortenService;
            _appointmentReminderService = appointmentReminderService;
            _materializer = materializer;
            _patientProfileService = patientProfileService;
            _dateTimeProvider = dateTimeProvider;
            _usersService = usersService;
            _lookupRecordService = lookupRecordService;
        }
        
        public async Task Handle(SchedulerReminderEvent notification, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Sending appointment reminder with [SchedulerId] = {notification.SchedulerBookingId} event received.");
            
            var appointment = await _appointmentsService.GetBySchedulerSystemIdAsync(notification.SchedulerBookingId);
            if (appointment is null)
            {
                _logger.LogInformation($"Sending appointment reminder with [SchedulerId] = {notification.SchedulerBookingId} was skipped. Appointment was not found");
                return;
            }

            var appointmentDomain = AppointmentDomain.Create(appointment);

            if (!IsReminderTimeCorrect(appointment, notification.ReminderType))
            {
                _logger.LogInformation($"Sending appointment reminder with [SchedulerId] = {notification.SchedulerBookingId} was skipped. Wrong reminder time [Type] = {notification.ReminderType}");
                return;
            }

            if (appointment.Patient is null)
            {
                _logger.LogInformation($"Sending appointment reminder with [SchedulerId] = {notification.SchedulerBookingId} was skipped. Appointment does not include any patient.");
                return;
            }
            
            _logger.LogInformation($"Sending appointment reminder with [Type] = {notification.ReminderType} [Id] = {appointment.Id} for [PatientId] = {appointment.Patient.Id}] has been started.");

            var practice = await _practiceService.GetOriginalPractice(appointmentDomain.GetPracticeId());

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, appointmentDomain.GetPracticeId());

            var botInteractivityOk = await PhoneNumberIsUniqueAsync(appointment.Patient.User);
            
            if (notification.ReminderType == AppointmentReminderType.TwoDays && botInteractivityOk)
            {
                // get all upcoming apps and their reminder bots
                // create the new reminder bot based on the context 
                var activeReminderBots = await _appointmentReminderService.GetAllAsync(appointment.PatientId!.Value, ChatbotType.AppointmentReminder);
                var dashboardLink = await _patientProfileService.GetDashboardLink(appointmentDomain.GetPracticeId());
                var bot = new AppointmentReminderBotPool(activeReminderBots, 48, _dateTimeProvider.UtcNow(), dashboardLink).CreateNew(appointment);  
                await _materializer.Materialize(bot.Tell());
            }
            
            if(notification.ReminderType != AppointmentReminderType.TwoDays) 
            {
                await SendEmailAsync(notification, appointment, practice, settings);
                await SendSmsAsync(appointment, notification.ReminderType);
            }
           
            _logger.LogInformation($"Sending appointment reminder with [Type] = {notification.ReminderType} [Id] = {appointment.Id} for [{appointment.Patient.Id}] has been successfully finished");
        }

        private async Task<bool> PhoneNumberIsUniqueAsync(User user)
        {
            //We need the E164 formatted phone number, and this service will get that for us.
            var lookup = await _lookupRecordService.GetOrCreateLookupAsync(user.UniversalId, user.PhoneNumber);
            
            if (lookup?.E164PhoneNumber is null)
            {
                //There's nothing to do here.
                //This can happen if the phone number is something like 111-111-1111.
                return false;
            }

            //Now we trace back from the E164 formatted number to users.
            var users = await _usersService.GetByPhoneAsync(lookup.E164PhoneNumber);
            var ok = users.Count() == 1;

            if (!ok)
            {
                //Unlikely.
                _logger.LogInformation($"The phone number {lookup.E164PhoneNumber} is shared by {users.Count()} user records.");
            }
            return ok;
        }

        private async Task SendSmsAsync(Appointment appointment, AppointmentReminderType reminderType)
        {
            var appointmentDomain = AppointmentDomain.Create(appointment);
            var phone = appointment.Patient.User.PhoneNumber;
            var universalId = appointment.Patient.User.UniversalId.ToString();
            var practiceId = appointmentDomain.GetPracticeId();
            var date = appointmentDomain.GetTimeZoneStartTime(true);

            var message = GetSMSMessage(reminderType);

            if (message is null)
            {
                return;
            }
        
            await _smsService.SendAsync(
                messagingServiceSidType: SettingsNames.Twilio.MessagingServiceSid,
                to: phone,
                body: await ParseMessageAsync(message, appointment, date),
                universalId: universalId,
                practiceId: practiceId);
        }

        #region private

        private async Task SendEmailAsync(SchedulerReminderEvent notification, Appointment appointment,
            PracticeModel practice, IDictionary<string, string> settings)
        {
            var (reminderText, cancellationText) = GetEmailContent(notification.ReminderType);

            var appointmentDomain = AppointmentDomain.Create(appointment);
            
            var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
            
            var loginLink = string.Format(applicationUrl, applicationUrl);
            
            var messagingLink = string.Format(applicationUrl, applicationUrl);

            if (notification.ReminderType == AppointmentReminderType.Week)
            {
                var model = new EmailDataModel<AppointmentReminderWeekModel>
                {
                    Data = new AppointmentReminderWeekModel
                    {
                        PracticeId = practice.Id,
                        PatientFirstName = appointment.Patient.User.FirstName,
                        JoinLink = appointment.JoinLink,
                        Start = appointmentDomain.GetTimeZoneStartTime(forPatient: true),
                        TimeZone = appointmentDomain.GetTimezoneDisplayName(forPatient: true),
                        Duration = appointment.Duration,
                        Location = appointment.LocationType.ToString(),
                        AppointmentName = appointment.Name,
                        EmployeeName = appointmentDomain.GetEmployeeNames(),
                        ProviderFullName = appointmentDomain.GetProviderName(),
                        HealthCoachFullName = appointmentDomain.GetCoachName(),
                        EmployeeType = AppointmentWithTypesTypes.WithTypes[appointment.WithType],
                        ReminderText = reminderText,
                        ClarityUrl = loginLink,
                        ClarityMessagingUrl = messagingLink,
                        CancellationText = cancellationText,
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
                                          $"{practice.Address.ZipCode}"
                    }
                };

                var email = await _emailFactory.Create(model);

                await _emailIntegrationService.SendEmailAndEventAsync(
                    to: appointment.Patient.User.Email,
                    subject: Subject,
                    body: email.Html,
                    practiceId: practice.Id,
                    emailTemplateTypeName: model.Type,
                    user: appointment.Patient.User
                );
            }
            else if (notification.ReminderType == AppointmentReminderType.Day)
            {
                var model = new EmailDataModel<AppointmentReminderDayModel>
                {
                    Data = new AppointmentReminderDayModel
                    {
                        PracticeId = practice.Id,
                        PatientFirstName = appointment.Patient.User.FirstName,
                        JoinLink = appointment.JoinLink,
                        Start = appointmentDomain.GetTimeZoneStartTime(forPatient: true),
                        TimeZone = appointmentDomain.GetTimezoneDisplayName(forPatient: true),
                        Duration = appointment.Duration,
                        Location = appointment.LocationType.ToString(),
                        AppointmentName = appointment.Name,
                        EmployeeName = appointmentDomain.GetEmployeeNames(),
                        ProviderFullName = appointmentDomain.GetProviderName(),
                        HealthCoachFullName = appointmentDomain.GetCoachName(),
                        EmployeeType = AppointmentWithTypesTypes.WithTypes[appointment.WithType],
                        ReminderText = reminderText,
                        ClarityUrl = loginLink,
                        ClarityMessagingUrl = messagingLink,
                        CancellationText = cancellationText,
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
                                          $"{practice.Address.ZipCode}"
                    }
                };

                var email = await _emailFactory.Create(model);

                await _emailIntegrationService.SendEmailAndEventAsync(
                    to: appointment.Patient.User.Email,
                    subject: Subject,
                    body: email.Html,
                    practiceId: practice.Id,
                    emailTemplateTypeName: model.Type,
                    user: appointment.Patient.User
                );
            }
            else if (notification.ReminderType == AppointmentReminderType.Hour)
            {
                var model = new EmailDataModel<AppointmentReminderHourModel>
                {
                    Data = new AppointmentReminderHourModel
                    {
                        PracticeId = practice.Id,
                        PatientFirstName = appointment.Patient.User.FirstName,
                        JoinLink = appointment.JoinLink,
                        Start = appointmentDomain.GetTimeZoneStartTime(forPatient: true),
                        TimeZone = appointmentDomain.GetTimezoneDisplayName(forPatient: true),
                        Duration = appointment.Duration,
                        Location = appointment.LocationType.ToString(),
                        AppointmentName = appointment.Name,
                        EmployeeName = appointmentDomain.GetEmployeeNames(),
                        ProviderFullName = appointmentDomain.GetProviderName(),
                        HealthCoachFullName = appointmentDomain.GetCoachName(),
                        EmployeeType = AppointmentWithTypesTypes.WithTypes[appointment.WithType],
                        ReminderText = reminderText,
                        ClarityUrl = loginLink,
                        ClarityMessagingUrl = messagingLink,
                        CancellationText = cancellationText,
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
                                          $"{practice.Address.ZipCode}"
                    }
                };

                var email = await _emailFactory.Create(model);

                await _emailIntegrationService.SendEmailAndEventAsync(
                    to: appointment.Patient.User.Email,
                    subject: Subject,
                    body: email.Html,
                    practiceId: practice.Id,
                    emailTemplateTypeName: model.Type,
                    user: appointment.Patient.User
                );
            } 
        }

        /// <summary>
        /// Check if a reminder required is necessary for handling bad requests about reminders.
        /// Scheduler systems can send reminders at the wrong time.
        /// </summary>
        /// <param name="appointment"></param>
        /// <param name="reminderType"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private bool IsReminderTimeCorrect(Appointment appointment, AppointmentReminderType reminderType)
        { 
            const int minDiapason = 10;
            var start = appointment.StartDate;

            bool CheckTimeDiapason(DateTime time)
            {
                return start >= time.AddMinutes(-minDiapason) && start <= time.AddMinutes(minDiapason);
            }

            return reminderType switch
            {
                AppointmentReminderType.Hour => CheckTimeDiapason(DateTime.UtcNow.AddHours(1)),
                AppointmentReminderType.Day => CheckTimeDiapason(DateTime.UtcNow.AddDays(1)),
                AppointmentReminderType.TwoDays => CheckTimeDiapason(DateTime.UtcNow.AddDays(2)),
                AppointmentReminderType.Week => CheckTimeDiapason(DateTime.UtcNow.AddDays(7)),
                _ => throw new ArgumentOutOfRangeException(nameof(reminderType), reminderType, null)
            };
        }
        
        private (string, string) GetEmailContent(AppointmentReminderType notificationReminderType)
        {
            return notificationReminderType switch
            {
                AppointmentReminderType.Hour => (
                    "We look forward to seeing you at your appointment in 1 hour! All your details are below:",
                    ""),
                AppointmentReminderType.Day => (
                    "Get ready! Your appointment is tomorrow. Here are the details:",
                    ""),
                AppointmentReminderType.Week => (
                    "Good news! Your appointment is one week away. Here’s everything you need to know: ",
                    "If you need to cancel or reschedule your appointment, we ask that you do so at least 72-hours prior to your scheduled time. Changes can be made directly within the {{ClarityPatientPortal}}. Please {{contactSupport}} via messaging if you need any assistance."
                    ),
                _ => throw new ArgumentOutOfRangeException(nameof(notificationReminderType), notificationReminderType,
                    null)
            };
        }

        private string? GetSMSMessage(AppointmentReminderType notificationReminderType)
        {
            return notificationReminderType switch
            {
                AppointmentReminderType.Hour => 
                    "Just a reminder that your appt with {provider} is in 1 hour! Link to join: {zoom}",
                AppointmentReminderType.Day => 
                    "Your appt is tomorrow at {time} {timezone}. Link to join: {zoom}. To make changes, log in to Clarity: {dashboardLink}",
                AppointmentReminderType.Week => null,
                _ => throw new ArgumentOutOfRangeException(nameof(notificationReminderType), notificationReminderType, null)
            };
        }

        private async Task<string> ParseMessageAsync(string templateSource, Appointment appointment, DateTime dateAndTime)
        {
            // var template = Handlebars.Compile(templateSource);
            var apptLink = appointment.JoinLink;
            var dashboardTinyLink = WebUrls.Clarity.DashboardSMS;
            
            try
            {
                var tags = new[] {"appointment"};
                apptLink = await _linkShortenService.ShortenAsync(apptLink, tags);
                
            }
            catch (Exception e)
            {
                _logger.LogWarning($"The URL shortening failed: {e.ToString()}.  Using the long link instead.");
            }
            try
            {
                var tags = new[] {"SMS"};
                dashboardTinyLink = await _linkShortenService.ShortenAsync(dashboardTinyLink, tags);
                
            }
            catch (Exception e)
            {
                _logger.LogWarning($"The URL shortening failed: {e.ToString()}.  Using the long link instead.");
            }

            var appointmentDomain = AppointmentDomain.Create(appointment)!;
            
            var parameters = new Dictionary<string, string?>
            {
                { "time", appointmentDomain.GetTimeZoneStartTime(forPatient: true).ToString("h:mm tt") },
                { "provider", appointmentDomain.GetMeetingOwner()?.User?.FirstName },
                { "date", dateAndTime.ToShortDateString() },
                { "zoom", apptLink },
                { "dashboardLink", dashboardTinyLink },
                { "timezone", appointmentDomain.GetTimezoneDisplayName(forPatient: true) }
            };

            foreach (var kvp in parameters)
            {
                templateSource = templateSource.Replace($"{{{kvp.Key}}}", kvp.Value);
            }

            return templateSource;
        }

        #endregion
    }
}