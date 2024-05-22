using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Enums.Patient;
using MediatR;
using FluentValidation;
using WildHealth.Common.Extensions;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Patients
{
    public class MigratePatientFromIntegrationSystemCommand : IRequest<MigratePatientFromIntegrationSystemResultModel>, IValidatabe
    {
        #region configuration
        
        public int PracticeId { get; }
        
        public PatientOnBoardingStatus Status { get; }
        
        public bool ConfirmAgreements { get; }

        public bool IsDPC { get; }

        #endregion

        #region patient information

        public string FirstName { get; }

        public string LastName { get; }

        public string MiddleName { get; }

        public string Email { get; }
        
        #endregion
        
        #region payment information
        
        public int PaymentPlanId { get; }
        
        public int PaymentPeriodId { get; }
        
        public int PaymentPriceId { get; }
        
        #endregion

        public MigratePatientFromIntegrationSystemCommand(
            int practiceId,
            PatientOnBoardingStatus status,
            bool confirmAgreements,
            string firstName,
            string lastName,
            string middleName,
            string email, 
            int paymentPlanId,
            int paymentPeriodId,
            int paymentPriceId,
            bool isDpc)
        {
            PracticeId = practiceId;
            Status = status;
            ConfirmAgreements = confirmAgreements;
            FirstName = firstName;
            LastName = lastName;
            MiddleName = middleName;
            Email = email;
            PaymentPlanId = paymentPlanId;
            PaymentPeriodId = paymentPeriodId;
            PaymentPriceId = paymentPriceId;
            IsDPC = isDpc;
        }

        #region validation

        private class Validator : AbstractValidator<MigratePatientFromIntegrationSystemCommand>
        {
            public Validator()
            {
                RuleFor(x => x.FirstName).NotEmpty().NotWhitespace();
                RuleFor(x => x.LastName).NotEmpty().NotWhitespace();
                RuleFor(x => x.Email).NotEmpty().NotWhitespace();
                RuleFor(x => x.MiddleName).NotEmpty().NotWhitespace();
                RuleFor(x => x.PracticeId).GreaterThan(0);
                RuleFor(x => x.PaymentPriceId).GreaterThan(0);
                RuleFor(x => x.PaymentPeriodId).GreaterThan(0);
                RuleFor(x => x.PaymentPlanId).GreaterThan(0);
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