using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Notes;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Notes;

public class ParseManualApoeFromNoteCommand : IRequest, IValidatabe
{
    public Note Note { get; }

    public ParseManualApoeFromNoteCommand(Note note)
    {
        Note = note;
    }

    #region validation

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<ParseManualApoeFromNoteCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Note).NotNull();
        }
    }
    
    #endregion
}