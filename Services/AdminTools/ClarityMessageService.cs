using System;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.AdminTool;
using WildHealth.Application.Services.AdminTools.Base;
using WildHealth.Application.Services.AdminTools.Helpers;
using WildHealth.Application.Services.InternalMessagesService;
using WildHealth.Application.Services.Notifications;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Enums.Messages;

namespace WildHealth.Application.Services.AdminTools;

public class ClarityMessageService: MessageServiceBase
{
    
    private readonly INotificationService _notificationService;
    private readonly IInternalMessagesService _messagesService;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;
    private readonly IRecipientService _recipientService;

    public ClarityMessageService( 
        INotificationService notificationService, 
        ILogger<ClarityMessageService> logger,
        IMediator mediator, 
        IRecipientService recipientService, 
        IInternalMessagesService messagesService)
    {
        _notificationService = notificationService;
        _logger = logger;
        _mediator = mediator;
        _recipientService = recipientService;
        _messagesService = messagesService;
    }

    public override async Task SendMessageAsync(int messageId, MyPatientsFilterModel filterModel)
    {
        _logger.LogInformation($"Sending message with id: {messageId} has been started.");

        var message = await _messagesService.GetByIdAsync(messageId);

        var allUsers = await _recipientService.GetAllRecipientsByFilter(filterModel);

        if (!allUsers.Any())
        {
            await _mediator.Publish(new MarketingMessageStatusChangedEvent(message.GetId(), MessageStatus.Delivered));
            return;
        }
        
        var notification = new MessageAdminToolClarityNotification(message: message, users: allUsers);

        try
        {
            await _notificationService.CreateNotificationAsync(notification);
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