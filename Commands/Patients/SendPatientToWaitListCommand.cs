using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Patients
{
    public class SendPatientToWaitListCommand : IRequest, IValidatabe
    {
        public string FirstName { get; }

        public string LastName { get; }

        public string PhoneNumber { get; }

        public string Email { get; }

        public string State { get; }

        public int PaymentPlanId { get; }

        public int PracticeId { get; }

        public SendPatientToWaitListCommand(
            string firstName, 
            string lastName,
            string phoneNumber,
            string email,
            string state,
            int paymentPlanId,
            int practiceId)
        {
            FirstName = firstName;
            LastName = lastName;
            PhoneNumber = phoneNumber;
            Email = email;
            State = state;
            PaymentPlanId = paymentPlanId;
            PracticeId = practiceId;
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

        private class Validator : AbstractValidator<SendPatientToWaitListCommand>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotNull().NotEmpty();
                RuleFor(x => x.LastName).NotNull().NotEmpty();
                RuleFor(x => x.PhoneNumber).NotNull().NotEmpty();
                RuleFor(x => x.Email).EmailAddress();
                RuleFor(x => x.State).NotNull().NotEmpty();
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.PaymentPlanId).GreaterThan(0);
            }
        }

        #endregion
    }
}