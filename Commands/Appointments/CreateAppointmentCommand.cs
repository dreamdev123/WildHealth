using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Enums.Appointments;
using FluentValidation;
using MediatR;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Commands.Appointments
{
    public class CreateAppointmentCommand : IRequest<Appointment?>, IValidatabe
    {
        public int PracticeId { get; }
        public int[] EmployeeIds { get; }
        public int? PatientId { get; }
        public int? LocationId { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public AppointmentLocationType LocationType { get; }
        public int? AppointmentTypeId { get; }
        public int? AppointmentTypeConfigurationId { get; }
        public string Name { get; }
        public string Comment { get; }
        public string TimeZoneId { get; }
        public UserType CreatedBy { get; }
        public int CreatedById { get; }
        public bool IsRescheduling { get; }
        
        public int? ReplacedAppointmentId { get; }
        public string Reason { get; }
        public AppointmentReasonType? ReasonType { get; }
        public string? Source { get; }

        public CreateAppointmentCommand(
            int practiceId,
            int[] employeeIds,
            int? patientId, 
            int? locationId, 
            DateTime startDate, 
            DateTime endDate, 
            AppointmentLocationType locationType,
            int? appointmentTypeId,
            int? appointmentTypeConfigurationId,
            string name,
            string comment,
            string timeZoneId,
            UserType userType,
            int createdById,
            string reason,
            AppointmentReasonType? reasonType = null,
            bool isRescheduling = false,
            int? replacedAppointmentId = null,
            string? source = null)
        {
            PracticeId = practiceId;
            EmployeeIds = employeeIds;
            PatientId = patientId;
            LocationId = locationId;
            StartDate = startDate;
            EndDate = endDate;
            LocationType = locationType;
            AppointmentTypeId = appointmentTypeId;
            AppointmentTypeConfigurationId = appointmentTypeConfigurationId;
            Name = name;
            Comment = comment;
            TimeZoneId = timeZoneId;
            CreatedBy = userType;
            CreatedById = createdById;
            Reason = reason;
            ReasonType = reasonType;
            IsRescheduling = isRescheduling;
            ReplacedAppointmentId = replacedAppointmentId;
            Source = source;
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<CreateAppointmentCommand>
        {
            public Validator()
            {
                RuleFor(x => x.EmployeeIds)
                    .NotNull()
                    .ForEach(x=> x.GreaterThan(0));

                RuleFor(x => x.PracticeId).GreaterThan(0);
                
                RuleFor(x => x.EndDate)
                    .GreaterThan(x => x.StartDate)
                    .WithMessage("Appointment end date must be greater then start date.");

                RuleFor(x => x.StartDate)
                    .GreaterThan(DateTime.UtcNow)
                    .WithMessage("Can't create appointment in the past.");

                RuleFor(x => x.CreatedBy).IsInEnum();
            }
        }
    }
}