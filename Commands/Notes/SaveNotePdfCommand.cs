using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Validators;

namespace WildHealth.Application.Commands.Notes;

public class SaveNotePdfCommand : IRequest, IValidatabe
{
    public IFormFile NotePdf { get; }
    public int NoteId { get; }
    public bool WithAmendments { get; }
    
    public SaveNotePdfCommand(IFormFile notePdf, int noteId, bool withAmendments)
    {
        NotePdf = notePdf;
        NoteId = noteId;
        WithAmendments = withAmendments;
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

    private class Validator : AbstractValidator<SaveNotePdfCommand>
    {
        public Validator()
        {
            RuleFor(x => x.NoteId).GreaterThan(0);
            RuleFor(x => x.NotePdf).SetValidator(new PdfFileValidator());
        }
    }

    #endregion
}