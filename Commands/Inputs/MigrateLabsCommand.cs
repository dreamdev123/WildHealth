using FluentValidation;
using WildHealth.Application.Commands._Base;
using MediatR;

namespace WildHealth.Application.Commands.Inputs
{
    public class MigrateLabsCommand : IRequest, IValidatabe
    {
        
        public MigrateLabsCommand()
        {
            
        }

        #region validation

        private class Validator : AbstractValidator<MigrateLabsCommand>
        {
            public Validator()
            {}
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