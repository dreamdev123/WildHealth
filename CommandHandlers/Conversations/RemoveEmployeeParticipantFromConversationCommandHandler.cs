using MediatR;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Messaging.Conversations;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Shared.Exceptions;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Twilio.Clients.Exceptions;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class RemoveEmployeeParticipantFromConversationCommandHandler : IRequestHandler<RemoveEmployeeParticipantFromConversationCommand, Conversation>
    {
        private readonly IMessagingConversationService _messagingConversationService;
        private readonly IMediator _mediator;
        private readonly ILogger<RemoveEmployeeParticipantFromConversationCommandHandler> _logger;
        private readonly IEmployeeService _employeeService;
        private readonly IConversationsService _conversationsService;

        public RemoveEmployeeParticipantFromConversationCommandHandler(
            IMessagingConversationService messagingConversationService,
            IMediator mediator,
            ILogger<RemoveEmployeeParticipantFromConversationCommandHandler> logger,
            IEmployeeService employeeService,
            IConversationsService conversationsService)
        {
            _messagingConversationService = messagingConversationService;
            _mediator = mediator;
            _logger = logger;
            _employeeService = employeeService;
            _conversationsService = conversationsService;
        }

        public async Task<Conversation> Handle(RemoveEmployeeParticipantFromConversationCommand command, CancellationToken cancellationToken)
        {
            var conversation = await _conversationsService.GetByIdAsync(command.ConversationId);
            var employee = await _employeeService.GetByUserIdAsync(command.UserId, EmployeeSpecifications.WithUser);

            var targetEmployee = conversation.EmployeeParticipants.FirstOrDefault(x => x.EmployeeId == employee.Id);
            if (targetEmployee is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, $"User {employee.User.FirstName} {employee.User.LastName} does not exist in conversation.");
            }

            var vendorExternalId = targetEmployee.GetCurrentVendorExternalId();

            if (!string.IsNullOrEmpty(vendorExternalId))
            {
                try
                {
                    await _messagingConversationService.RemoveConversationParticipantAsync(
                        practiceId: employee.User.PracticeId,
                        conversation: conversation,
                        vendorExternalId: vendorExternalId
                    );
                }
                catch (TwilioException e)
                {
                    if (e.StatusCode == HttpStatusCode.NotFound)
                    {
                        //We want to just warn and proceed in this situation.
                        _logger.LogWarning($"The conversation {conversation.VendorExternalId} was not found, or participant {vendorExternalId} was not found in the conversation.");
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            await _mediator.Send(new RemoveEmployeeNotificationFromConversationCommand(command.ConversationId, command.UserId), cancellationToken);

            // If we don't check this, then we will get a validation error when creating the command because it requires s vendorExternalId
            if (!string.IsNullOrEmpty(vendorExternalId))
            {
                try
                {
                    await _mediator.Send(new PublishConversationParticipantRemovedEventCommand(
                        conversationVendorExternalId: conversation.VendorExternalId,
                        conversationId: conversation.GetId(),
                        subject: conversation.Subject,
                        state: conversation.State,
                        participantVendorExternalId: vendorExternalId,
                        employeeUniversalId: targetEmployee.Employee.User.UserId()));
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }
            }

            await _conversationsService.RemoveParticipantAsync(conversation, targetEmployee);

            await _mediator.Send(new CheckConversationSignOffCommand(conversationId: conversation.GetId()),
                cancellationToken);

            return conversation;
        }
    }
}
