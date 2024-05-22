using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Employees
{
    public class DeleteEmployeeCommandHandler : IRequestHandler<DeleteEmployeeCommand>
    {
        private readonly ITransactionManager _transactionManager;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IEmployeeService _employeeService;
        private readonly IAuthService _authService;
        private readonly IAuthTicket _authTicket;

        public DeleteEmployeeCommandHandler(
            ITransactionManager transactionManager,
            IPermissionsGuard permissionsGuard,
            IEmployeeService employeeService,
            IAuthService authService,
            IAuthTicket authTicket)
        {
            _transactionManager = transactionManager;
            _permissionsGuard = permissionsGuard;
            _employeeService = employeeService;
            _authService = authService;
            _authTicket = authTicket;
        }

        public async Task Handle(DeleteEmployeeCommand command, CancellationToken cancellationToken)
        {
            if (_authTicket.GetEmployeeId() == command.EmployeeId)
            {
                throw new AppException(HttpStatusCode.Conflict, "User cannot execute self deleting!");
            }

            var employee = await _employeeService.GetByIdAsync(command.EmployeeId);

            _permissionsGuard.AssertPermissions(employee);

            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                await _employeeService.DeleteAsync(employee);

                await _authService.DeleteAsync(employee.UserId);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
        }
    }
}
