using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Notes;

namespace WildHealth.Application.Commands.Notes
{
    public class DeleteNoteCommand : IRequest<Note>, IValidatabe
    {
        public int Id { get; }
        
        public NoteDeletionReason Reason { get; set; }
        
        public DeleteNoteCommand(int id, NoteDeletionReason reason)
        {
            Id = id;
            Reason = reason;
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

        private class Validator : AbstractValidator<DeleteNoteCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
            }
        }

        #endregion
    }
}