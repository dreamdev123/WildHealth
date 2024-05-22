using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class SendDorothyClaimSubmittedCommsCommand : IRequest
{
    public int ClaimId { get; }

    public SendDorothyClaimSubmittedCommsCommand(int claimId)
    {
        ClaimId = claimId;
    }
    
    #region validation

    private class Validator : AbstractValidator<SendDorothyClaimSubmittedCommsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ClaimId).GreaterThan(0);
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