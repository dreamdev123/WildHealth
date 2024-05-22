using System;
using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Models.Timeline;

namespace WildHealth.Application.CommandHandlers.Patients.Flows;

public class CreatePatientFlow : IMaterialisableFlow
{
    private readonly User _user;
    private readonly PatientOptions _patientOptions;
    private readonly Location _location;
    private readonly DateTime _utcNow;

    public CreatePatientFlow(User user, PatientOptions patientOptions, Location location, DateTime utcNow)
    {
        _user = user;
        _patientOptions = patientOptions;
        _location = location;
        _utcNow = utcNow;
    }

    public MaterialisableFlowResult Execute()
    {
        var timelineEvent = new PatientCreatedTimelineEvent(0, _utcNow); // patientId is 0 because patient doesn't exist yet
        var patient = new Patient(_user, _patientOptions, _location) {TimelineEvents = new List<PatientTimelineEvent> {timelineEvent }};
        patient.SetRegistrationDate(_utcNow);
        
        return patient.Added().ToFlowResult();
    }
}