using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Insurances;
using ClaimStatus = WildHealth.Domain.Enums.Insurance.ClaimStatus;

namespace WildHealth.Application.Commands.Insurances;

public class CreateClaimCommand : IRequest<Claim?>, IValidatabe
{
    public int NoteId { get; }
    
    public ClaimStatus ClaimStatus { get; }

    public CreateClaimCommand(int noteId, ClaimStatus claimStatus)
    {
        NoteId = noteId;
        ClaimStatus = claimStatus;
    }
    
    #region validation

    private class Validator : AbstractValidator<CreateClaimCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
            RuleFor(x => x.ClaimStatus).NotNull();
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
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}