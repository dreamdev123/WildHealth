using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Employees;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Appointments;

namespace WildHealth.Application.CommandHandlers.Appointments.Flows;

public record CreatePastAppointmentFlow(
    Patient Patient, 
    Employee Employee, 
    Note Note,
    int? Duration, 
    TimeZoneInfo TimeZone,
    AppointmentType? Type,
    AppointmentTypeConfiguration? Configuration,
    DateTime UtcNow) : IMaterialisableFlow
{
    private const int DefaultDuration = 25;

    public MaterialisableFlowResult Execute()
    {
        var startDate = Note.VisitDate;
        var endDate = startDate.AddMinutes(Duration ?? DefaultDuration);
            
        if (startDate >= endDate)
        {
            throw new DomainException("Appointment start date should be less then end date.");
        }
        
        if (startDate > UtcNow)
        {
            throw new DomainException("Appointment date should be in the past.");
        }
        
        var appointment = new Appointment(
            patientId: Patient.GetId(),
            locationId: Patient.LocationId,
            locationType: AppointmentLocationType.Online,
            startDate: startDate,
            endDate: endDate,
            withType: Configuration?.WithType ?? GetWithType(Employee),
            type: Type?.Type,
            configurationId: Configuration?.GetId(),
            replacedAppointmentId: null
        )
        {
            Status = AppointmentStatus.Submitted,
            Purpose = Type?.Purpose ?? AppointmentPurpose.FollowUp,
            Name = Note.Title,
            Comment = string.Empty,
            Reason = string.Empty,
            ReasonType = null,
            TimeZoneId = TimeZone.Id,
            ProductId =  null,
            AutoRecordingSet = false
        };

        Note.Appointment = appointment;

        var appointmentDomain = AppointmentDomain.Create(appointment);

        appointmentDomain.SetEmployees(new[] { Employee });

        return appointment.Added() + Note.Updated();
    }
    
    #region private

    private AppointmentWithType GetWithType(Employee employee)
    {
        return employee.Type switch
        {
            EmployeeType.Coach => AppointmentWithType.HealthCoach,
            EmployeeType.Provider => AppointmentWithType.Provider,
            EmployeeType.RegistrationManager => AppointmentWithType.Other,
            EmployeeType.Staff => AppointmentWithType.Staff,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    #endregion
}