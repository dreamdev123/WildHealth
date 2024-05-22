using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Services.Notes;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Domain.Entities.Notes;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes
{
    public class DeleteNoteCommandHandler : IRequestHandler<DeleteNoteCommand, Note>
    {
        private readonly IAuthTicket _authTicket;
        private readonly INoteService _noteService;
        private readonly IEmployeeService _employeeService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IFlowMaterialization _materializeFlow;

        public DeleteNoteCommandHandler(
            IAuthTicket authTicket,
            INoteService noteService, 
            IEmployeeService employeeService,
            IPermissionsGuard permissionsGuard,
            IDateTimeProvider dateTimeProvider,
            IFlowMaterialization materializeFlow)
        {
            _authTicket = authTicket;
            _noteService = noteService;
            _employeeService = employeeService;
            _permissionsGuard = permissionsGuard;
            _dateTimeProvider = dateTimeProvider;
            _materializeFlow = materializeFlow;
        }
        
        public async Task<Note> Handle(DeleteNoteCommand command, CancellationToken cancellationToken)
        {
            var utcNow = _dateTimeProvider.UtcNow();
            
            var note = await _noteService.GetByIdAsync(command.Id);

            var employee = await _employeeService.GetByIdAsync(_authTicket.GetEmployeeId() ?? 0);

            _permissionsGuard.AssertPermissions(note);

            var flow = new DeleteNoteFlow(note, employee, command.Reason, utcNow);

            await flow.Materialize(_materializeFlow.Materialize);

            return note;
        }
    }
}