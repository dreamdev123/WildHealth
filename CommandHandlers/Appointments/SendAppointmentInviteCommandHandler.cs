using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Application.Services.Practices;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Appointments
{
    public class SendAppointmentInviteCommandHandler : IRequestHandler<SendAppointmentInviteCommand>
    {
        private const string Subject = "Meeting Invite";
        
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
        private readonly IAppointmentsService _appointmentsService;
        private readonly ILogger<SendAppointmentInviteCommandHandler> _logger;
        
        public SendAppointmentInviteCommandHandler(
            IEmailFactory emailFactory,
            IEmailIntegrationService emailIntegrationService,
            IPracticeService practiceService,
            ISettingsManager settingsManager,
            IAppointmentsService appointmentsService,
            ILogger<SendAppointmentInviteCommandHandler> logger)
        {
            _logger = logger;
            _emailFactory = emailFactory;
            _emailIntegrationService = emailIntegrationService;
            _practiceService = practiceService;
            _settingsManager = settingsManager;
            _appointmentsService = appointmentsService;
        }

       
        public async Task Handle(SendAppointmentInviteCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Sending appointment link for [Email] = {request.Email} has been started.");
            
            var appointment = await _appointmentsService.GetByIdAsync(request.AppointmentId);

            var appointmentDomain = AppointmentDomain.Create(appointment);
            
            var practice = await _practiceService.GetOriginalPractice(appointmentDomain.GetPracticeId());

            var settings = await _settingsManager.GetSettings(EmailContainerSettings, appointmentDomain.GetPracticeId());
            
            var model = new EmailDataModel<AppointmentInviteModel>
            {
                Data = new AppointmentInviteModel
                {
                    PracticeId = practice.Id,
                    JoinLink = appointment.JoinLink,
                    Start = appointmentDomain.GetTimeZoneStartTime(),
                    TimeZone = GetTimeZoneString(appointment.TimeZoneId),
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
                to: request.Email,
                subject: Subject,
                body: email.Html,
                practiceId: practice.Id,
                emailTemplateTypeName: nameof(AppointmentInviteModel)
            );
            
            _logger.LogInformation($"Sending appointment link for [Email] = {request.Email} has been successfully finished.");
        }

        #region private

        private string GetTimeZoneString(string timeZoneId)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId).DisplayName;
        }
        
        #endregion
    }
}