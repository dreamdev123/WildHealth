using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Appointments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Appointments
{
    public class CreateAllDayAppointmentCommand : IRequest<Appointment[]>, IValidatabe
    {
        public int PracticeId { get; }
        
        public int[] EmployeeIds { get; }
        
        public int LocationId { get; }
        
        public DateTime Date { get; }
        
        public string Name { get; }
        
        public string Comment { get; }
        
        public string TimeZoneId { get; }
        
        public string? Source { get; }

        public CreateAllDayAppointmentCommand(
            int practiceId, 
            int[] employeeIds, 
            int locationId, 
            DateTime date, 
            string name, 
            string comment, 
            string timeZoneId,
            string? source = null)
        {
            PracticeId = practiceId;
            EmployeeIds = employeeIds;
            LocationId = locationId;
            Date = date;
            Name = name;
            Comment = comment;
            TimeZoneId = timeZoneId;
            Source = source;
        }

        #region validation

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<CreateAllDayAppointmentCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeIds)
                    .NotNull()
                    .NotEmpty()
                    .ForEach(x=> x.GreaterThan(0));

                RuleFor(x => x.LocationId).GreaterThan(0);
                
                RuleFor(x => x.PracticeId).GreaterThan(0);

                RuleFor(x => x.Name).NotNull().NotEmpty();
                
                RuleFor(x => x.Date.Date)
                    .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
                    .WithMessage("Can't create appointment in the past.");
            }
        }
        
        #endregion
    }
}