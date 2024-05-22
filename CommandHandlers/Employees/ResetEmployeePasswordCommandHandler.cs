using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Auth;
using WildHealth.Domain.Entities.Employees;
using Microsoft.EntityFrameworkCore;
using WildHealth.Infrastructure.Data.Queries;
using MediatR;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Employees
{
    public class ResetEmployeePasswordCommandHandler : IRequestHandler<ResetEmployeePasswordCommand>
    {
        private readonly IGeneralRepository<Employee> _employeeRepository;
        private readonly IAuthService _authService;

        public ResetEmployeePasswordCommandHandler(
            IGeneralRepository<Employee> employeeRepository, 
            IAuthService authService)
        {
            _employeeRepository = employeeRepository;
            _authService = authService;
        }

        public async Task Handle(ResetEmployeePasswordCommand command, CancellationToken cancellationToken)
        {
            var employee = await _employeeRepository
                .All()
                .IncludeIdentity()
                .FirstOrDefaultAsync(x => x.Id == command.EmployeeId, cancellationToken: cancellationToken);

            if (employee == null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(command.EmployeeId), command.EmployeeId);                
                throw new AppException(HttpStatusCode.NotFound, "Employee not found", exceptionParam);
            }

            await _authService.UpdatePassword(employee.User.Identity, command.NewPassword);
        }
    }
}