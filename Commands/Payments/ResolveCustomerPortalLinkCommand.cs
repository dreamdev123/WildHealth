using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Payments;

public class ResolveCustomerPortalLinkCommand : IRequest<string>, IValidatabe
{
    public string Code { get; }
    
    public ResolveCustomerPortalLinkCommand(string code)
    {
        Code = code;
    }

    #region validation

    private class Validator : AbstractValidator<ResolveCustomerPortalLinkCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Code).NotNull().NotEmpty();
        }
    }

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}