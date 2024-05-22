using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Appointments;
using FluentValidation;
using MediatR;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Commands.Appointments
{
    public class CancelAppointmentCommand : IRequest<Appointment>, IValidatabe 
    {
        public int Id { get; }
        
        public int CancelledBy { get; }
        
        public AppointmentCancellationReason CancellationReason { get; }

        public string? Source { get; }
        
        public CancelAppointmentCommand(int id, int cancelledBy, AppointmentCancellationReason cancellationReason, string? source = null)
        {
            Id = id;
            CancelledBy = cancelledBy;
            CancellationReason = cancellationReason;
            Source = source;
        }
        
        #region validation

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<CancelAppointmentCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
            }
        }

        #endregion
    }
}