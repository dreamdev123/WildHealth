using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Attachments;

namespace WildHealth.Application.Commands.Notes;

public class GetNotePdfCommand : IRequest<(Attachment? attachment, byte[] bytes)>, IValidatabe
{
    public GetNotePdfCommand(int noteId, bool withAmendments)
    {
        NoteId = noteId;
        WithAmendments = withAmendments;
    }

    public int NoteId { get; }
    public bool WithAmendments { get; }
    
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

    private class Validator : AbstractValidator<GetNotePdfCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
        }
    }

    #endregion
}