using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Notes;
using WildHealth.Domain.Entities.Notes;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes;

public class CompleteAndSignOffCommandHandler : IRequestHandler<CompleteAndSignOffCommand, Note>
{
    private readonly INoteService _noteService;
    private readonly IEmployeeService _employeeService;
    private readonly IAppointmentsService _appointmentsService;
    private readonly MaterializeFlow _materialize;

    public CompleteAndSignOffCommandHandler(
        INoteService noteService,
        IEmployeeService employeeService, 
        IAppointmentsService appointmentsService,
        MaterializeFlow materialize)
    {
        _noteService = noteService;
        _employeeService = employeeService;
        _appointmentsService = appointmentsService;
        _materialize = materialize;
    }

    public async Task<Note> Handle(CompleteAndSignOffCommand request, CancellationToken cancellationToken)
    {
        var note = await _noteService.GetByIdAsync(request.NoteId);
        var employee = await _employeeService.GetByUserIdAsync(request.CompletedBy);
        var appointment = note.AppointmentId.HasValue
            ? await _appointmentsService.GetByIdAsync(note.AppointmentId.Value)
            : null;
        
        var result = await new CompleteNoteFlow(note, appointment, employee, DateTime.UtcNow).Materialize(_materialize);
        return result.Select<Note>();
    }
}