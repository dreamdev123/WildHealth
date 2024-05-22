using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Notes;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Notes;
using WildHealth.IntegrationEvents.Notes.Payloads;
using WildHealth.Infrastructure.Communication.MessageBus.Provider;
using MediatR;

namespace WildHealth.Application.EventHandlers.Notes
{
    public class SendIntegrationEventOnNoteCreatedEvent : INotificationHandler<NoteCreatedEvent>
    {
        private readonly IEventBus _eventBus;

        public SendIntegrationEventOnNoteCreatedEvent(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public async Task Handle(NoteCreatedEvent notification, CancellationToken cancellationToken)
        {
            var note = notification.Note;
            var patient = note.Patient;

            await _eventBus.Publish(new NotesIntegrationEvent(
                payload: new NoteCompletedPayload(
                    name: note.Name,
                    type: note.Type.ToString(),
                    order: patient.Notes.Count.ToString()
                ),
                patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                eventDate: note.CreatedAt), cancellationToken);
        }
    }
}