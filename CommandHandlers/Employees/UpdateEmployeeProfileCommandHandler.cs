using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Address;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Models.Employees;
using WildHealth.Shared.Data.Managers.TransactionManager;
using MediatR;
using WildHealth.Common.Models.Users;

namespace WildHealth.Application.CommandHandlers.Employees;

public class UpdateEmployeeProfileCommandHandler : IRequestHandler<UpdateEmployeeProfileCommand, Employee>
{
    private readonly ITransactionManager _transactionManager;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IEmployeeService _employeeService;
    private readonly IAddressService _addressService;
    private readonly IMediator _mediator;

    public UpdateEmployeeProfileCommandHandler(
        ITransactionManager transactionManager,
        IPermissionsGuard permissionsGuard,
        IEmployeeService employeeService,
        IAddressService addressService,
        IMediator mediator)
    {
        _transactionManager = transactionManager;
        _permissionsGuard = permissionsGuard;
        _employeeService = employeeService;
        _addressService = addressService;
        _mediator = mediator;
    }

    public async Task<Employee> Handle(UpdateEmployeeProfileCommand command, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(command.Id);

        var employeeDomain = EmployeeDomain.Create(employee);

        _permissionsGuard.AssertPermissions(employee);

        var updateUserCommand = new UpdateUserCommand(
            id: employee.User.GetId(),
            firstName: command.FirstName,
            lastName: command.LastName,
            birthday: command.Birthday,
            gender: command.Gender,
            email: command.Email,
            phoneNumber: command.PhoneNumber,
            billingAddress: new AddressModel(),
            shippingAddress: new AddressModel(),
            userType: null);

        await using var transaction = _transactionManager.BeginTransaction();
        try
        {
            await _mediator.Send(updateUserCommand, cancellationToken);

            employeeDomain.UpdateCredentials(command.Credentials);

            employee.Bio = command.Bio;
            
            await _addressService.UpdateEmployeeStatesAsync(command.States, employee);

            await UpdateProfilePhotoAsync(employee, command.ProfilePhoto);

            await _employeeService.UpdateAsync(employee);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
                
            throw;
        }

        return await _employeeService.GetByIdAsync(command.Id);
    }

    private async Task UpdateProfilePhotoAsync(Employee employee, IFormFile file)
    {
        if (file is null)
        {
            return;
        }
        
        await _mediator.Send(new UpdateProfilePhotoCommand(employee, file));
    }
}