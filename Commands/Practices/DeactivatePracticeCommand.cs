using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Practices;
using MediatR;

namespace WildHealth.Application.Commands.Practices
{
    public class DeactivatePracticeCommand : IRequest<Practice>, IValidatabe
    {
        public int PracticeId { get; }

        public DeactivatePracticeCommand(int practiceId)
        {
            PracticeId = practiceId;
        }

        #region validation

        public class Validator : AbstractValidator<DeactivatePracticeCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
            }
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        #endregion
    }
}