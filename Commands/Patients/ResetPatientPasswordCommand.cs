using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Validators;

namespace WildHealth.Application.Commands.Patients
{
    public class ResetPatientPasswordCommand: IRequest, IValidatabe
    {
        public int PatientId { get; }

        public string NewPassword { get; }

        public bool AssertPermissions { get; }

        public ResetPatientPasswordCommand(int patientId, string newPassword, bool assertPermissions = true)
        {
            PatientId = patientId;
            NewPassword = newPassword;
            AssertPermissions = assertPermissions;
        }

        #region validation

        private class Validator : AbstractValidator<ResetPatientPasswordCommand>
        {
            public Validator()
            {
                RuleFor(x => x.NewPassword).SetValidator(new PasswordValidator());
                RuleFor(x => x.PatientId).GreaterThan(0);
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
