using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Notes;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.HealthSummaries;

public class ParseNoteToHealthSummaryCommand : IRequest, IValidatabe
{
    public Note Note { get; }

    public ParseNoteToHealthSummaryCommand(Note note)
    {
        Note = note;
    }
    
    #region Validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<ParseNoteToHealthSummaryCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Note).NotNull();
        }
    }

    #endregion
}