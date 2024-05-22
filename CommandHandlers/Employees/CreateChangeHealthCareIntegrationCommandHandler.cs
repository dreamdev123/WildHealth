using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Application.Commands.Employees;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Exceptions;
using WildHealth.Domain.Models.Integration;

namespace WildHealth.Application.CommandHandlers.Employees;

public class CreateChangeHealthCareIntegrationCommandHandler : IRequestHandler<CreateChangeHealthCareIntegrationCommand, UserIntegration>
{
    private readonly IIntegrationsService _integrationsService;
    private readonly IUsersService _usersService;
    private readonly IEmployeeService _employeeService;
    private readonly ILogger<CreateChangeHealthCareIntegrationCommandHandler> _logger;

    public CreateChangeHealthCareIntegrationCommandHandler(IIntegrationsService integrationsService,
        IUsersService usersService,
        IEmployeeService employeeService,
        ILogger<CreateChangeHealthCareIntegrationCommandHandler> logger)
    {
        _integrationsService = integrationsService;
        _usersService = usersService;
        _employeeService = employeeService;
        _logger = logger;
    }

    public async Task<UserIntegration> Handle(CreateChangeHealthCareIntegrationCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _usersService.GetByEmailAsync(request.EmployeeEmail);

        if (user == null) 
        {
            throw new EntityNotFoundException($"User {request.EmployeeEmail} is not found");
        }
        
        var userId = user.GetId();

        await AssertIsEmployee(userId);

        _logger.LogInformation($"Creating CHC user integration for {user.Email}");
        var existingIntegration = await _integrationsService.GetUserIntegrationAsync(userId, IntegrationVendor.ChangeHealthCare,
            IntegrationPurposes.Employee.ChangeHealthCareCredential);

        var valueModel = new ChangeHealthCareUserIntegrationValue(request.CHCUsername, request.CHCPassword);
        var json = JsonConvert.SerializeObject(valueModel);

        if (existingIntegration == null)
        {
            _logger.LogInformation($"Creating new CHC user integration for {user.Email}");
            var ui = new UserIntegration(
                vendor: IntegrationVendor.ChangeHealthCare,
                purpose: IntegrationPurposes.Employee.ChangeHealthCareCredential,
                value: json,
                user: user
            );

            var integration = await _integrationsService.CreateAsync(ui);
            return integration;
        }
        
        _logger.LogInformation($"Updating existing CHC user integration for {user.Email}");
        existingIntegration.Integration.Value = json;
        await _integrationsService.UpdateAsync(existingIntegration.Integration);
        return existingIntegration;
    }

    private async Task AssertIsEmployee(int userId)
    {
        var employee = await _employeeService.GetByUserIdAsync(userId);

        if (employee == null)
        {
            throw new DomainException("Only an employee can have a user integration for CHC.");
        }
    }
}
