using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Events.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Domain.Models.Conversation;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.EventHandlers.Conversations;

public class UpdateParticipantsOnConversationSettingsUpdatedEvent : INotificationHandler<ConversationSettingsUpdatedEvent>
{
    private readonly IConversationsService _conversationsService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger _logger;
    private readonly IMediator _mediator;

    public UpdateParticipantsOnConversationSettingsUpdatedEvent(
        IConversationsService conversationsService,
        IEmployeeService employeeService,
        ILogger<UpdateParticipantsOnConversationSettingsUpdatedEvent> logger,
        IMediator mediator)
    {
        _conversationsService = conversationsService;
        _employeeService = employeeService;
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Handle(ConversationSettingsUpdatedEvent notification, CancellationToken cancellationToken)
    {
        var previousForwardEmployeeId = notification.PreviousForwardEmployeeId;
        var settingsOwnerEmployeeId = notification.EmployeeId;

        // Checking to see if forward employee has changed to remove the prior employee from conversations
        if (notification.ShouldRemoveDelegatedEmployee)
        {
            await RemoveDelegatedEmployeeAsync(
                employeeId: settingsOwnerEmployeeId,
                forwardToEmployeeId: previousForwardEmployeeId
            );
        }
    }

    private async Task RemoveDelegatedEmployeeAsync(int employeeId, int forwardToEmployeeId)
    {
        var conversations = await _conversationsService.GetDelegatedConversationsByEmployeeAsync(
            delegatedTo: forwardToEmployeeId,
            delegatedBy: employeeId
        );

        var employee = await _employeeService.GetByIdAsync(forwardToEmployeeId);

        foreach (var conversation in conversations)
        {
            var conversationDomain = ConversationDomain.Create(conversation);
            if (!conversationDomain.HasEmployeeParticipant(forwardToEmployeeId))
            {
                return;
            }

            var command = new RemoveEmployeeParticipantFromConversationCommand(
                conversationId: conversation.GetId(),
                userId: employee.UserId
            );

            var result = await _mediator.Send(command).ToTry();
            if (result.IsError())
            {
                // Log error, then continue
                var ex = result.Exception();
                _logger.LogError(ex,
                    "There was a problem attempting to remove [UserId] = {UserId} from [ConversationId] = {ConversationId}, [Message]: {Message}",
                    employee.UserId, conversation.GetId(), ex.Message);
            }
        }
    }
}