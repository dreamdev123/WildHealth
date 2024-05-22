using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Common.Constants;
using WildHealth.Settings;
using MediatR;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Application.Utils.Timezones;
using WildHealth.Common.Models.Conversations;
using WildHealth.Domain.Models.Patient;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class CheckUnreadMessagesInAllConversationsCommandHandler : MessagingBaseService, IRequestHandler<CheckUnreadMessagesInAllConversationsCommand>
    {
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IConversationsService _conversationsService;
        private readonly IOptions<SchedulerOptions> _schedulerOptions;
        private readonly IConversationParticipantPatientService _conversationParticipantPatientsService;
        private readonly IConversationParticipantMessageReadIndexService _conversationParticipantMessageReadIndexService;
        private readonly IMediator _mediator;
        private readonly ILogger<CheckUnreadMessagesInAllConversationsCommandHandler> _logger;
        
        public CheckUnreadMessagesInAllConversationsCommandHandler(
            IFeatureFlagsService featureFlagsService,
            IConversationsService conversationsService,
            IConversationParticipantPatientService conversationParticipantPatientsService,
            IOptions<SchedulerOptions> schedulerOptions,
            ISettingsManager settingsManager,
            IConversationParticipantMessageReadIndexService conversationParticipantMessageReadIndexService,
            IMediator mediator,
            ILogger<CheckUnreadMessagesInAllConversationsCommandHandler> logger) : base(settingsManager)
        {
            _mediator = mediator;
            _featureFlagsService = featureFlagsService;
            _conversationsService = conversationsService;
            _conversationParticipantPatientsService = conversationParticipantPatientsService;
            _schedulerOptions = schedulerOptions;
            _conversationParticipantMessageReadIndexService = conversationParticipantMessageReadIndexService;
            _logger = logger;
        }

        public async Task Handle(CheckUnreadMessagesInAllConversationsCommand request, CancellationToken cancellationToken)
        {
            var checkTime = request.Runtime ?? DateTime.UtcNow;

            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.ConversationsBackgroundJobs))
            {
                _logger.LogInformation("[CheckUnreadMessagesInAllConversationsCommand] Feature flag disabled: [WH-All-Conversations-BackgroundJobs]");
                return;
            }

            var results = await _conversationParticipantMessageReadIndexService.GetUnreadConversationParticipantIndexesWithoutNotifications();

            if (results.Count() == 0)
            {
                _logger.LogInformation("[CheckUnreadMessagesInAllConversationsCommand] No messages to send at this time");
                return;
            }
            
            
            foreach(var result in results)
            {
                try
                {
                    var conversation = await _conversationsService
                        .GetByExternalVendorIdAsync(result.ConversationVendorExternalId);

                    // If this is null it's a good chance that this unread entry is an employee
                    var participantPatient = await _conversationParticipantPatientsService.GetByVendorExternalIdentityAndConversationId(result.ParticipantVendorExternalId, conversation.GetId());

                    if (participantPatient is null || !ShouldGenerateNotification(result, checkTime)) {
                        _logger.LogInformation(" [CheckUnreadMessagesInAllConversationsCommand] Participant Patient is null or still not the time to send events");
                        continue;
                    }

                    var patient = participantPatient.Patient;
                    
                    var patientDomain = PatientDomain.Create(patient);

                    var practice = patientDomain.GetPractice();

                    var lastReadMessageIndex = result.LastReadIndex;
                
                    var unreadMessagesCount = conversation.Index - lastReadMessageIndex;

                    var lastMessageSentDateForPatientTimeZone = GetDateTimeForPatient(patient, result.LastMessageSentDate);

                    var lastMessageSentDateFormatted = FormatLastMessageSentDate(lastMessageSentDateForPatientTimeZone);

                    var messageLocationText = GetMessageLocationText(conversation.Type, practice);

                    var command = new CreateConversationMessageUnreadNotificationCommand(
                        sentAt: checkTime,
                        unreadMessageCount: Math.Max(0, unreadMessagesCount),
                        lastReadMessageIndex: lastReadMessageIndex,
                        userId: participantPatient.Patient.UserId,
                        conversationId: conversation.GetId(),
                        conversationVendorExternalIdentity: conversation.VendorExternalId,
                        participantVendorExternalIdentity: result.ParticipantVendorExternalId
                    );
                    
                    await _mediator.Send(command, cancellationToken);
                    
                    var @event = new ConversationEmailUnreadNotificationEvent(
                        patient: participantPatient.Patient,
                        unreadMessageCount: unreadMessagesCount,
                        practice: patientDomain.GetPractice(),
                        lastMessageSentDate: lastMessageSentDateForPatientTimeZone,
                        lastMessageSentDateFormatted: lastMessageSentDateFormatted,
                        messageLocationText: messageLocationText,
                        conversationType: conversation.Type
                    );

                    await _mediator.Publish(@event, cancellationToken);
                    
                    _logger.LogInformation("[CheckUnreadMessagesInAllConversationsCommand] events sent properly");
                }
                catch(Exception e)
                {
                    // ignore
                    _logger.LogWarning($"[CheckUnreadMessagesInAllConversationsCommand] Check unread messages has failed with [Error]: {e.ToString()}");
                }
            }
        }
        
        #region private

        private DateTime? GetDateTimeForPatient(Patient patient, DateTime? inUtc) 
        {
            if (!inUtc.HasValue)
            {
                return null;
            }

            try
            {
                return TimezoneHelper.ConvertToTimeZone(patient.TimeZone, inUtc.Value);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private string FormatLastMessageSentDate(DateTime? inLocalTime) {
            
            if (!inLocalTime.HasValue)
            {
                return "Unknown time";
            }
            
            var time = inLocalTime.Value.ToString("hh:mm tt");
            var date = inLocalTime.Value.ToString("MM/dd/yyyy");

            return $"{time} on {date}";
        }

        private string GetMessageLocationText(ConversationType type, Practice practice) {
            if(type == ConversationType.HealthCare) {
                return $"{practice.Name} Portal";
            } else if(type == ConversationType.Support) {
                return $"{practice.Name} Portal, staff conversation";
            }
            else {
                throw new AppException(System.Net.HttpStatusCode.BadRequest, $"Unable to generate an unread message notification from a conversation that is not of type HealthCare or Support");
            }
        }
        
        private bool ShouldGenerateNotification(ConversationParticipantMessageUnreadModel model, DateTime time)
        {
            var dateToUse = model.ModifiedAt ?? model.CreatedAt;

            // TODO: review  this condition could take the double of business requirement time
            if (time.Subtract(dateToUse).TotalMinutes > _schedulerOptions.Value.CheckUnreadMessagesPeriodInMinutes) 
            {
                return true;
            }

            return false;
        }
        
        #endregion
    }
}