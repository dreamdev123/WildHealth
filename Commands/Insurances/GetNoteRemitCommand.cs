using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Fhir.Models.Claims;
using WildHealth.Fhir.Models.PaymentRecs;

namespace WildHealth.Application.Commands.Insurances;

public class GetNoteRemitCommand : IRequest<PaymentRecModel[]?>
{
    public int NoteId { get; }

    public GetNoteRemitCommand(int noteId)
    {
        NoteId = noteId;
    }
    
    #region validation

    private class Validator : AbstractValidator<GetNoteRemitCommand>
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