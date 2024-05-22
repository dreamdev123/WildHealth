using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using MediatR;
using WildHealth.Application.Services.AppointmentsOptions;
using WildHealth.Domain.Entities.Notes;

namespace WildHealth.Application.CommandHandlers.Notes
{
    public class SaveNoteCommandHandler : IRequestHandler<SaveNoteCommand, Note>
    {
        private readonly IMediator _mediator;
        private readonly IAppointmentOptionsService _appointmentOptionsService;

        public SaveNoteCommandHandler(IMediator mediator, IAppointmentOptionsService appointmentOptionsService)
        {
            _mediator = mediator;
            _appointmentOptionsService = appointmentOptionsService;
        }

        public async Task<Note> Handle(SaveNoteCommand command, CancellationToken cancellationToken)
        {
            Note note;
            if (command.Id.HasValue && command.Id != 0)
            {
                note = await _mediator.Send(new UpdateNoteCommand(
                    id: command.Id.Value,
                    name: command.Name,
                    title: command.Title,
                    visitDate: command.VisitDate,
                    appointmentId: command.AppointmentId,
                    content: command.Content,
                    internalContent: command.InternalContent,
                    logs: command.Logs,
                    isCompleted: command.IsCompleted), cancellationToken);
            }
            else
            {
                note = await _mediator.Send(new CreateNoteCommand(
                    name: command.Name,
                    title: command.Title,
                    type: command.Type,
                    visitDate: command.VisitDate,
                    patientId: command.PatientId,
                    employeeId: command.EmployeeId,
                    appointmentId: command.AppointmentId,
                    content: command.Content,
                    internalContent: command.InternalContent,
                    logs: command.Logs,
                    isCompleted: command.IsCompleted,
                    originalNoteId: command.OriginalNoteId), cancellationToken);
            }

            await SetAppointmentOptionsAsync(command, note);
            return note;
        }
        private async Task SetAppointmentOptionsAsync(SaveNoteCommand command, Note note)
        {
            if (command.NextProviderAppointmentDate.HasValue)
            {
                await _appointmentOptionsService.UpdateProviderAppointmentDateAsync(
                    patientId: note.PatientId,
                    nextAppointmentDate: command.NextProviderAppointmentDate.Value
                );
            }

            if (command.NextCoachAppointmentDate.HasValue)
            {
                await _appointmentOptionsService.UpdateHealthCoachAppointmentDateAsync(
                    patientId: note.PatientId,
                    nextAppointmentDate: command.NextCoachAppointmentDate.Value
                );
            }
        }
    }
}