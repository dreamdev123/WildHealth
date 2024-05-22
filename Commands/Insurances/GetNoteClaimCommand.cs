using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Fhir.Models.Claims;

namespace WildHealth.Application.Commands.Insurances;

public class GetNoteClaimCommand : IRequest<ClaimModel?>
{
    public int NoteId { get; }

    public GetNoteClaimCommand(int noteId)
    {
        NoteId = noteId;
    }
    
    #region validation

    private class Validator : AbstractValidator<GetNoteClaimCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
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