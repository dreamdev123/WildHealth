using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Users;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Commands;

public class CreatePreauthorizeRequestsBulkCommand : IRequest<PreauthorizeRequest[]>, IValidatabe
{
    public CreatePreauthorizeRequestCommand[] Commands { get; }
    
    public CreatePreauthorizeRequestsBulkCommand(CreatePreauthorizeRequestCommand[] commands)
    {
        Commands = commands;
    }

    
    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<CreatePreauthorizeRequestsBulkCommand>
    {
        public Validator()
        {
            RuleForEach(x => x.Commands).NotNull();
            RuleForEach(x => x.Commands)
                .SetValidator(new CreatePreauthorizeRequestCommand.Validator());
        }
    }

    #endregion
}