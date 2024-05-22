using FluentValidation;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Practices;
using MediatR;

namespace WildHealth.Application.Commands.Practices
{
    public class ActivatePracticeCommand : IRequest<Practice>, IValidatabe
    {
        public int PracticeId { get; }

        public ActivatePracticeCommand(int practiceId)
        {
            PracticeId = practiceId;
        }

        #region validation

        public class Validator : AbstractValidator<ActivatePracticeCommand>
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