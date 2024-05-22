using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Extensions;

namespace WildHealth.Application.Commands.Employees;

public class CreateChangeHealthCareIntegrationCommand : IRequest<UserIntegration>, IValidatabe
{
    public CreateChangeHealthCareIntegrationCommand(string employeeEmail, string chcUsername, string chcPassword)
    {
        EmployeeEmail = employeeEmail;
        CHCUsername = chcUsername;
        CHCPassword = chcPassword;
    }
    
    public string EmployeeEmail { get; }
    public string CHCUsername { get; }
    public string CHCPassword { get; }
    
    public bool IsValid() => new Validator().Validate(this).IsValid;
    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<CreateChangeHealthCareIntegrationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeEmail).NotEmpty();
            RuleFor(x => x.CHCUsername).NotEmpty();
            RuleFor(x => x.CHCPassword).NotEmpty();
        }
    }
}