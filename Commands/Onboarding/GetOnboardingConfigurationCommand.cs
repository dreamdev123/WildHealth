using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Onboarding;

using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Onboarding
{
    public class GetOnboardingConfigurationCommand : IRequest<OnboardingConfigurationModel>, IValidatabe
    {
        public int PracticeId { get; }
        
        public GetOnboardingConfigurationCommand(int practiceId)
        {
            PracticeId = practiceId;
        }
        
        #region validation

        private class Validator : AbstractValidator<GetOnboardingConfigurationCommand>
        {
            public Validator()
            {
                RuleFor(x => x.PracticeId).GreaterThan(0);
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