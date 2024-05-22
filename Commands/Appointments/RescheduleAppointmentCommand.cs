using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Extensions;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Shared.Enums;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Appointments
{
    /// <summary>
    /// Command provides reschedule appointment by cancelling previous and creates new
    /// </summary>
    public class RescheduleAppointmentCommand : IRequest<Appointment>, IValidatabe
    {
        public int CancelledAppointmentId { get; }
        public int PracticeId { get; }
        public int[] EmployeeIds { get; }
        public int? PatientId { get; }
        public int? LocationId { get; }
        public DateTime StartDate { get; }
        public DateTime EndDate { get; }
        public AppointmentLocationType LocationType { get; }
        public int? AppointmentTypeId { get; }
        public int? AppointmentTypeConfigurationId { get; }
        public string Comment { get; }
        public string TimeZoneId { get; }
        public string Name { get; }
        public UserType UserType { get; }
        public int CreatedById { get; }
        public string Reason { get; }
        public AppointmentReasonType? ReasonType { get; }
        public bool IsPatientRequesting { get; }
        public string? Source { get; }

        public RescheduleAppointmentCommand(
            int cancelledAppointmentId,
            int practiceId,
            int[] employeeIds,
            int? patientId, 
            int? locationId, 
            DateTime startDate, 
            DateTime endDate, 
            AppointmentLocationType locationType, 
            int? appointmentTypeId,
            int? appointmentTypeConfigurationId,
            string comment,
            string timeZoneId,
            string name,
            UserType userType,
            int createdById,
            string reason,
            bool isPatientRequesting,
            AppointmentReasonType? reasonType = null,
            string? source = null)
        {
            CancelledAppointmentId = cancelledAppointmentId;
            PracticeId = practiceId;
            EmployeeIds = employeeIds;
            PatientId = patientId;
            LocationId = locationId;
            StartDate = startDate;
            EndDate = endDate;
            LocationType = locationType;
            AppointmentTypeId = appointmentTypeId;
            AppointmentTypeConfigurationId = appointmentTypeConfigurationId;
            Comment = comment;
            TimeZoneId = timeZoneId;
            Name = name;
            UserType = userType;
            CreatedById = createdById;
            Reason = reason;
            ReasonType = reasonType;
            IsPatientRequesting = isPatientRequesting;
            Source = source;
        }

        public bool IsValid() => new Validator().Validate(this).IsValid;

        public void Validate() => new Validator().ValidateAndThrow(this);

        private class Validator : AbstractValidator<RescheduleAppointmentCommand>
        {
            public Validator()
            {
                RuleFor(x => x.Name).Length(1, 250).NotWhitespace();

                RuleFor(x => x.CancelledAppointmentId).GreaterThan(0);
                
                RuleFor(x => x.EmployeeIds).NotNull();

                RuleFor(x => x.PracticeId).GreaterThan(0);

                RuleFor(x => x.AppointmentTypeId).GreaterThan(0);
                
                RuleFor(x => x.AppointmentTypeConfigurationId).GreaterThan(0);
                
                RuleFor(x => x.LocationId)
                    .GreaterThan(0)
                    .When(x => x.LocationType == AppointmentLocationType.InPerson);
                
                RuleFor(x => x.EndDate)
                    .GreaterThan(x => x.StartDate)
                    .WithMessage("Appointment end date must be greater then start date.");

                RuleFor(x => x.StartDate)
                    .GreaterThan(DateTime.UtcNow)
                    .WithMessage("Can't create appointment in the past.");
            }
        }
    }
}