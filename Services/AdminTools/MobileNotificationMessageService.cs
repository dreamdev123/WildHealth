using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.AdminTool;
using WildHealth.Application.Services.AdminTools.Base;
using WildHealth.Application.Services.AdminTools.Helpers;
using WildHealth.Application.Services.InternalMessagesService;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Enums.Messages;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using WildHealth.IntegrationEvents.Message;
using WildHealth.IntegrationEvents.Message.Payloads;
using WildHealth.Application.Extensions;

namespace WildHealth.Application.Services.AdminTools;

public class MobileNotificationMessageService : MessageServiceBase
{
    private readonly int _messageChunkSize = 500;
    private readonly IInternalMessagesService _messagesService;
    private readonly ILogger _logger;
    private readonly IEventBus _eventBus;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IMediator _mediator;
    private readonly IRecipientService _recipientService;

    public MobileNotificationMessageService(
        IInternalMessagesService messagesService, 
        ILogger<MobileNotificationMessageService> logger, 
        IDateTimeProvider dateTimeProvider, 
        IMediator mediator, 
        IRecipientService recipientService)
    {
        _messagesService = messagesService;
        _logger = logger;
        _eventBus = EventBusProvider.Get();
        _dateTimeProvider = dateTimeProvider;
        _mediator = mediator;
        _recipientService = recipientService;
    }

    public override async Task SendMessageAsync(int messageId, MyPatientsFilterModel filterModel)
    {
        _logger.LogInformation($"Sending message with id: {messageId} has been started.");

        var message = await _messagesService.GetByIdAsync(messageId);
        
        var allUsers = await _recipientService.GetAllRecipientsByFilter(filterModel);

        var allUserTokens = new List<string>();
        
        foreach (var user in allUsers)
        {
            foreach (var device in user.Devices)
            {
                allUserTokens.Add(device.DeviceToken);
            }
        }

        if (!allUserTokens.Any())
        {
            await _mediator.Publish(new MarketingMessageStatusChangedEvent(message.GetId(), MessageStatus.Delivered));
            return;
        }

        try
        {
            foreach (var chunk in allUserTokens.Chunk(_messageChunkSize))
            {
                await _eventBus.Publish(
                    new MobileNotificationIntegrationEvent(
                        new NotificationPushedPayload(
                            message.Subject,
                            message.Body,
                            chunk
                        ), _dateTimeProvider.UtcNow()));
            }
        }
        catch (Exception ex)
        {
            await _mediator.Publish(new MarketingMessageStatusChangedEvent(message.GetId(), MessageStatus.Failed));
            _logger.LogError("Failure sending marketing message {Ex}", ex);
            return;
        }
        
        await _mediator.Publish(new MarketingMessageStatusChangedEvent(message.GetId(), MessageStatus.Delivered));
        
        _logger.LogInformation($"Sending message with id: {message.Id} has been finished.");
    }
}