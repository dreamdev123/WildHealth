using WildHealth.Domain.Entities.Locations;
using MediatR;
using WildHealth.Application.Commands._Base;
using FluentValidation;

namespace WildHealth.Application.Commands.Locations
{
    public class DeleteLocationCommand : IRequest<Location>, IValidatabe
    {
        public int Id { get; }

        public DeleteLocationCommand(int id)
        {
            Id = id;
        }

        #region private
        
        /// <summary>
        /// <see cref="IValidatabe.IsValid"/>
        /// </summary>
        /// <returns></returns>
        public bool IsValid() => new Validator().Validate(this).IsValid;

        /// <summary>
        /// <see cref="IValidatabe.Validate"/>
        /// </summary>
        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<DeleteLocationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
            }
        }
        
        #endregion
    }
}