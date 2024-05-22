using System;
using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.ShortcutGroups
{
    public class CopyShortcutGroupsToPracticeCommand: IRequest, IValidatabe
    {
        public int FromPracticeId { get; }
        
        public int ToPracticeId { get; }
        
        public DateTime StartDate { get; }
        
        public CopyShortcutGroupsToPracticeCommand(
            int fromPracticeId, 
            int toPracticeId)
        {
            FromPracticeId = fromPracticeId;
            ToPracticeId = toPracticeId;
        }
        
        #region validation

        private class Validator : AbstractValidator<CopyShortcutGroupsToPracticeCommand>
        {
            public Validator()
            {
                RuleFor(x => x.FromPracticeId).GreaterThan(0);
                RuleFor(x => x.ToPracticeId).GreaterThan(0);
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
        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}