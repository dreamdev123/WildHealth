using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Notes;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes;

public class RequestNoteSignOffCommandHandler : IRequestHandler<RequestNoteSignOffCommand, Note>
{
    private readonly IEmployeeService _employeeService;
    private readonly INoteService _noteService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFlowMaterialization _materializeFlow;

    public RequestNoteSignOffCommandHandler(
        INoteService noteService, 
        IEmployeeService employeeService, 
        IDateTimeProvider dateTimeProvider,
        IFlowMaterialization materializeFlow)
    {
        _noteService = noteService;
        _employeeService = employeeService;
        _dateTimeProvider = dateTimeProvider;
        _materializeFlow = materializeFlow;
    }

    public async Task<Note> Handle(RequestNoteSignOffCommand command, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeProvider.UtcNow();
        
        var note = await _noteService.GetByIdAsync(command.NoteId);

        var employee = await _employeeService.GetByIdAsync(command.AssignToEmployeeId, EmployeeSpecifications.WithUser);

        var flow = new RequestNoteSignOffFlow(note, employee, command.AdditionalNote, utcNow);
        
        await flow.Materialize(_materializeFlow.Materialize);
        
        return note;
    }
}