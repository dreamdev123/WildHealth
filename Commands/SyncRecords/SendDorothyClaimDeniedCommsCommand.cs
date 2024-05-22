using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.SyncRecords;

public class SendDorothyClaimDeniedCommsCommand : IRequest, IValidatabe
{
    public int ClaimId { get; }

    public SendDorothyClaimDeniedCommsCommand(int claimId)
    {
        ClaimId = claimId;
    }
    
    #region validation

    private class Validator : AbstractValidator<SendDorothyClaimDeniedCommsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ClaimId).NotNull().NotEmpty();
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