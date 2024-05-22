using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Appointments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Appointments
{
    /// <summary>
    /// Command provides mark appointment as a NoShow
    /// </summary>
    public class NoShowAppointmentCommand : IRequest<Appointment>, IValidatabe
    {
        public int AppointmentId { get; }

        public NoShowAppointmentCommand(
            int appointmentId)
        {
            AppointmentId = appointmentId;
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<NoShowAppointmentCommand>
        {
            public Validator()
            {
                RuleFor(x => x.AppointmentId).GreaterThan(0);
            }
        }
    }
}