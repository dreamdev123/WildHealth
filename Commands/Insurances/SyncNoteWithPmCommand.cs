using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Insurances;

public class SyncNoteWithPmCommand : IRequest, IValidatabe
{
    public int NoteId { get; }
    
    public SyncNoteWithPmCommand(int noteId)
    {
        NoteId = noteId;
    }
    
    #region validation
    
    private class Validator : AbstractValidator<SyncNoteWithPmCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
        }
    }
    
    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);


    #endregion
}