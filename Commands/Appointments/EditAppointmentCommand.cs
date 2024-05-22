using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Appointments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Appointments
{
    public class EditAppointmentCommand : IRequest<Appointment>, IValidatabe
    {
        /// <summary>
        /// Appointment ID
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Comment to update
        /// </summary>
        public string Comment { get; }

        /// <summary>
        /// Patient id for security check
        /// </summary>
        public int? PatientId { get; }

        public EditAppointmentCommand(
            int id,
            string comment,
            int? patientId)
        {
            Id = id;
            Comment = comment;
            PatientId = patientId;
        }
        
        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<EditAppointmentCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Id).GreaterThan(0);
                
                RuleFor(x => x.PatientId).GreaterThan(0);
            }
        }
    }
}