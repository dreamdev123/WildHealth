using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Commands;

public class GetPreauthorizedSignUpUrlCommand : IRequest<string>, IValidatabe
{
    public int PreauthorizeRequestId { get; }
    
    public GetPreauthorizedSignUpUrlCommand(int preauthorizeRequestId)
    {
        PreauthorizeRequestId = preauthorizeRequestId;
    }

    
    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetPreauthorizedSignUpUrlCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PreauthorizeRequestId).GreaterThan(0);
        }
    }

    #endregion
}