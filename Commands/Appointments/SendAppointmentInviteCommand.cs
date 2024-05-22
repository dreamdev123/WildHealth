using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Appointments
{
    /// <summary>
    /// Command sends email with invite to appointment
    /// </summary>
    public class SendAppointmentInviteCommand : IRequest, IValidatabe
    {
        public int AppointmentId { get; }

        public string Email { get; }

        public SendAppointmentInviteCommand(
            int appointmentId,
            string email)
        {
            AppointmentId = appointmentId;
            Email = email;
        }

        #region Validation

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<SendAppointmentInviteCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Email)
                    .NotNull()
                    .NotEmpty()
                    .EmailAddress();

                RuleFor(x => x.AppointmentId)
                    .GreaterThan(0);
            }
        }

        #endregion
    }
}