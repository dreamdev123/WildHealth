using MediatR;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Domain.Entities.Conversations;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class StartSupportConversationCommandHandler : IRequestHandler<StartSupportConversationCommand, Conversation> 
    {
        private readonly IMediator _mediator;
        
        public StartSupportConversationCommandHandler(
            IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<Conversation> Handle(StartSupportConversationCommand command, CancellationToken cancellationToken)
        {
            var createConversationCommand = new CreatePatientSupportConversationCommand(
                practiceId: command.PracticeId,
                locationId: command.LocationId,
                patientId: command.PatientId,
                subject: command.Subject
            );

            var conversation = await _mediator.Send(createConversationCommand, cancellationToken);

            return conversation;
        }
    }
}