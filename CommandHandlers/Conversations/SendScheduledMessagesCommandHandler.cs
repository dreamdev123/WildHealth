using System;
using MediatR;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.WebClient;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Twilio.Clients.Models.Media;
using System.Net.Mime;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class SendScheduledMessagesCommandHandler : MessagingBaseService, IRequestHandler<SendScheduledMessagesCommand>
    {
        private const string Container = AzureBlobContainers.Attachments;
        private readonly IScheduledMessagesService _scheduledMessagesService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly ITwilioWebClient _twilioWebClient;
        private readonly ITwilioMediaWebClient _twilioMediaWebClient;
        private readonly IAttachmentsService _attachmentsService;
        private readonly IConversationsService _conversationsService;
        private readonly ILogger<SendScheduledMessagesCommandHandler> _logger;
        private readonly IMediator _mediator;
        
        public SendScheduledMessagesCommandHandler(
            ISettingsManager settingsManager,
            ITwilioWebClient twilioWebClient,
            ITwilioMediaWebClient twilioMediaWebClient,
            IFeatureFlagsService featureFlagsService,
            IScheduledMessagesService scheduledMessagesService,
            IAttachmentsService attachmentsService,
            ILogger<SendScheduledMessagesCommandHandler> logger,
            IConversationsService conversationsService,
            IMediator mediator
            ) : base(settingsManager)
        {
            _scheduledMessagesService = scheduledMessagesService;
            _featureFlagsService = featureFlagsService;
            _twilioWebClient = twilioWebClient;
            _twilioMediaWebClient = twilioMediaWebClient;
            _attachmentsService = attachmentsService;
            _logger = logger;
            _conversationsService = conversationsService;
            _mediator = mediator;

        }

        public async Task Handle(SendScheduledMessagesCommand request, CancellationToken cancellationToken)
        {
            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.ConversationsBackgroundJobs))
            {
                return;
            }
            
            var scheduledMessages = await _scheduledMessagesService.GetMessagesToSendAsync(System.DateTime.UtcNow);

            if (scheduledMessages.Count() == 0)
            {
                _logger.LogInformation(" [SendScheduledMessagesCommand] there are no messages to send");
            }
            
            foreach (var scheduledMessage in scheduledMessages)
            {
                try
                {
                    var credentials = await GetMessagingCredentialsAsync(scheduledMessage.Conversation.PracticeId);

                    _twilioMediaWebClient.Initialize(credentials);

                    _twilioWebClient.Initialize(credentials);

                    var attachments =
                        await _attachmentsService.GetByTypeAttachmentAsync(AttachmentType.ScheduledMessageAttachment,
                            scheduledMessage.GetId());

                    var author = scheduledMessage.ConversationParticipantEmployee.VendorExternalIdentity;
                    var conversation = scheduledMessage.Conversation;
                   
                    // Always send text
                    await SendMessage(
                        conversation: conversation,
                        author: author,
                        body: scheduledMessage.Message,
                        media: null
                    );

                    // Create media in Twilio
                    foreach (var attachment in attachments)
                    {
                        string[] fileNameArray = attachment.Name.Split(' ');
                        if (fileNameArray.Count() > 1) 
                        {
                            // Check the incorrect Attach File and Delete Attachment 
                            await _attachmentsService.DeleteAttachmentAsync(attachment.GetId());
                        } 
                        else 
                        {
                            _logger.LogInformation($"Loading attachment: {attachment.GetId()} from container {Container}");
                            var bytes = await _attachmentsService.GetFileByPathAsync(attachment.Path);

                            var extension = attachment.Name.Substring(attachment.Name.LastIndexOf(".", StringComparison.Ordinal));

                            string contentType = MediaTypeNames.Application.Pdf;

                            if (extension != ".pdf") {
                                contentType = MediaTypeNames.Image.Jpeg;
                            }

                            var fileProperties = await _attachmentsService.GetFilePropertiesByIdAsync(attachment.GetId());

                            var mediaModel = await _twilioMediaWebClient.UploadMediaResourceAsync(credentials.ChatServiceId, contentType, bytes);

                            // Always send a single message with media in it
                            await SendMessage(
                                conversation: conversation,
                                author: author,
                                body: attachment.Name,
                                media: mediaModel
                            );
                        }
                    }

                    scheduledMessage.SetSentState(true);
                    await _scheduledMessagesService.UpdateAsync(scheduledMessage);
                    _logger.LogInformation(
                        $"[SendScheduledMessagesCommand] message sent properly in [conversationID]: {scheduledMessage.Conversation.VendorExternalId}");
                }
                catch(Exception ex)
                {
                    _logger.LogError($"[SendScheduledMessagesCommand] error during sending scheduled message with error: {ex.ToString()}");
                }
            }
        }

        private async Task SendMessage(Conversation conversation, string author, string body, MediaUploadModel? media) 
        {
            var sendMessageCommand = new SendMessageCommand(conversation, author, body, media);

            await _mediator.Send(sendMessageCommand);
        }
    }
}
