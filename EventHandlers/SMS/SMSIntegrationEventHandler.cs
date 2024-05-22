using System;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Application.Commands.Sms;
using WildHealth.Application.Domain.AppointmentReminder;
using WildHealth.Domain.Entities.Sms;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.SMS;
using WildHealth.IntegrationEvents.SMS.Payloads;

namespace WildHealth.Application.EventHandlers.SMS
{
    public class SMSIntegrationEventHandler : IEventHandler<SMSIntegrationEvent>
    {
        private const string StopType = "STOP";
        private const string StartType = "START";
        private const string UnsubscribedUserErrorCode = "21610";
        
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        
        public SMSIntegrationEventHandler(
            IMediator mediator,
            ILogger<SMSIntegrationEventHandler> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(SMSIntegrationEvent @event)
        {
            if (@event.PayloadType != nameof(SMSIntegrationPayload))
                return;
            
            var payload = @event.Payload as SMSIntegrationPayload ?? @event.DeserializePayload<SMSIntegrationPayload>();
            var payloadString = JsonConvert.SerializeObject(payload);

            if (payload.Source != SMSMessagingSource.Undefined)
                await _mediator.Publish(new ChatbotMessageReceivedEvent(payload.Source, payload.From, payload.To, payload.Body));
            
            await HandleErrorCodeAsync(payload, @event.User, payloadString);
            
            if (string.IsNullOrWhiteSpace(payload.OptOutType))
            {
                //we don't care about it.
                _logger.LogInformation("The payload does not specify an opt out type, so there's nothing to do");
                return;
            }

            _ = payload.OptOutType switch
            {
                StopType => await RevokeSmsConsentAsync(payload, @event.User, payloadString),
                StartType => await ProvideSmsConsentAsync(payload, @event.User, payloadString),
                _ => null
            };
        }

        private async Task HandleErrorCodeAsync(SMSIntegrationPayload payload, UserMetadataModel eventUser, string payloadString)
        {
            if (UnsubscribedUserErrorCode.Equals(payload.ErrorCode))
            {
                _logger.LogInformation($"Received Twilio 21610 for {eventUser.UniversalId}. Revoking SMS Consent.");
                //In the case of a status up date, the recipient is the To field.
                var command = new RevokeSmsConsentCommand(
                    RecipientPhoneNumber: payload.To,
                    SenderPhoneNumber: payload.From,
                    PhoneUserIdentity: eventUser.UniversalId == null ? Guid.Empty : Guid.Parse(eventUser.UniversalId),
                    IntegrationEventJson: payloadString, 
                    MessagingServiceSid: payload.MessagingServiceSid);
                
               var consent = await _mediator.Send<SmsConsent>(command);
            }
        }

        private async Task<SmsConsent> ProvideSmsConsentAsync(SMSIntegrationPayload smsIntegrationPayload, UserMetadataModel user, string json)
        {
            //In the case of a status a STOP or START response, the recipient is the From field.
            var command = new ProvideSmsConsentCommand(
                RecipientPhoneNumber: smsIntegrationPayload.From,
                SenderPhoneNumber: smsIntegrationPayload.To,
                PhoneUserIdentity: user.UniversalId == null ? Guid.Empty : Guid.Parse(user.UniversalId),
                IntegrationEventJson: json, 
                MessagingServiceSid: smsIntegrationPayload.MessagingServiceSid);
            
           var consent = await _mediator.Send<SmsConsent>(command);
           return consent;
        }
        
        private async Task<SmsConsent> RevokeSmsConsentAsync(SMSIntegrationPayload smsIntegrationPayload, UserMetadataModel user, string json)
        {
            //In the case of a status a STOP or START response, the recipient is the From field.
            var command = new RevokeSmsConsentCommand(
                RecipientPhoneNumber: smsIntegrationPayload.From,
                SenderPhoneNumber: smsIntegrationPayload.To,
                PhoneUserIdentity: user.UniversalId == null ? Guid.Empty : Guid.Parse(user.UniversalId),
                IntegrationEventJson: json, 
                MessagingServiceSid: smsIntegrationPayload.MessagingServiceSid);
            
           var consent = await _mediator.Send<SmsConsent>(command);
           return consent;
        }
    }
}