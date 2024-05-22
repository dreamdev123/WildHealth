using System;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Services.SMS;
using WildHealth.Common.Constants;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class SendSMSNotificationForConversationReminderCommandHandler : MessagingBaseService, IRequestHandler<SendSMSNotificationForConversationReminderCommand>
    {
        private readonly IPatientsService _patientsService;
        private readonly ISMSService _smsService;
        private readonly ILogger _logger;

        public SendSMSNotificationForConversationReminderCommandHandler(
            IMediator mediator,
            IPatientsService patientsService,
            IFeatureFlagsService featureFlagsService,
            ISMSService smsService,
            ILogger<SendSMSNotificationForConversationReminderCommandHandler> logger,
             ISettingsManager settingsManager) : base(settingsManager)
        {
            _patientsService = patientsService;
            _smsService = smsService;
            _logger = logger;
        }

        public async Task Handle(SendSMSNotificationForConversationReminderCommand command, CancellationToken cancellationToken)
        {
            try
            {
                if (command.Patient == null || command.Patient?.User == null)
                {
                    // This is to avoid spam sentry,from an unwanted backGround job (still in research).
                    _logger.LogInformation($"Calling this SMS Command from unwanted source");
                    return;
                }
                _logger.LogInformation($"Calling Send SMS Notification Reminder for User with  [Id] : {command.Patient?.User?.Id} and [PhoneNumber]: {command.Patient?.User?.PhoneNumber}");
                await SendSmsTo(command);
            }
            catch (Exception e)
            {
                _logger.LogError($"Error sending SMS reminder to patient with [Id]: {command.Patient?.GetId()} failed with [Error]:{e.ToString()}");
            }
        }
        
        #region private

        private async Task SendSmsTo(SendSMSNotificationForConversationReminderCommand command)
        {
            var practiceId = command.Patient.User.PracticeId;
            var phoneNumber = command.Patient.User.PhoneNumber;
            var universalId = command.Patient.User.UniversalId.ToString();
            await _smsService.SendAsync(
                messagingServiceSidType: SettingsNames.Twilio.MessagingServiceSid,
                to: phoneNumber, 
                body: command.Body, 
                universalId: universalId, 
                practiceId: practiceId);
        }
        
        #endregion
    }
}
