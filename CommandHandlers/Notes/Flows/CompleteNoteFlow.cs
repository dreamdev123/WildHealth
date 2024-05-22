using System;
using System.Collections.Generic;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Events.Notes;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Models.Employees;
using WildHealth.Domain.Models.Notes;
using WildHealth.Domain.Models.Timeline;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Exceptions;
using System.Net;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public class CompleteNoteFlow : IMaterialisableFlow
{
    private readonly Note _note;
    private readonly Appointment? _appointment;
    private readonly Employee _completedByEmployee;
    private readonly DateTime _utcNow;

    public CompleteNoteFlow(Note note, Appointment? appointment, Employee completedByEmployee, DateTime utcNow)
    {
        _note = note;
        _appointment = appointment;
        _completedByEmployee = completedByEmployee;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        AssertEmployeeCanCompleteNote(_completedByEmployee);
        var completedByEmployeeDomain = EmployeeDomain.Create(_completedByEmployee);
        var noteDomain = NoteDomain.Create(_note);
        
        noteDomain.Complete(
            completedAt: _utcNow,
            completedBy: completedByEmployeeDomain.GetSignature(),
            completedById: _completedByEmployee.GetId()
        );
        
        var healthCoachName = $"{_completedByEmployee.User.FirstName} {_completedByEmployee.User.LastName}";
        var timelineEvent = new NotePublishedTimelineEvent(_note.PatientId, _utcNow, new NotePublishedTimelineEvent.Data(_note.Type, healthCoachName, _note.GetId())).Entity();
            
        return _note.Updated() + timelineEvent.Added() + RaiseEvents(noteDomain);
    }
    
    private void AssertEmployeeCanCompleteNote(Employee employee)
    {
        if (string.IsNullOrEmpty(employee.Credentials))
        {
            throw new AppException(HttpStatusCode.BadRequest, "Please update your credentials in your account.");
        }
    }

    private IEnumerable<INotification> RaiseEvents(NoteDomain noteDomain)
    {
        yield return new NoteCompletedEvent(_note);

        if (noteDomain.HasAppointment() && noteDomain.IsAppointmentRelated() && _appointment != null)
        {
            yield return new AppointmentCompletedEvent(_appointment, UserType.Employee);
        }
    }
}