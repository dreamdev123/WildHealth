using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Notes;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Notes;

public class RequestNoteSignOffCommand : IRequest<Note>, IValidatabe
{
    public int NoteId { get; }
    public int AssignToEmployeeId { get; }
    public string AdditionalNote { get; }
    
    public RequestNoteSignOffCommand(int noteId, int assignToEmployeeId, string additionalNote)
    {
        NoteId = noteId;
        AssignToEmployeeId = assignToEmployeeId;
        AdditionalNote = additionalNote;
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

    private class Validator : AbstractValidator<RequestNoteSignOffCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
            RuleFor(x => x.AssignToEmployeeId).GreaterThan(0);
        }
    }

    #endregion
}