using System;
using System.Linq;
using System.Collections.Generic;
using WildHealth.Application.Events.Notes;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.Notes;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Notes;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Patient;

namespace WildHealth.Application.CommandHandlers.Notes.Flows;

public record CreateNoteFlow(
    string Name,
    int Version,
    string Title,
    NoteType Type,
    string Content,
    string InternalContent,
    DateTime VisitDate,
    int? OriginalNoteId,
    Patient Patient,
    Employee Employee,
    Employee? DelegatedEmployee,
    Appointment? Appointment,
    NoteLogModel[] Logs) : IMaterialisableFlow
{
    private readonly IDictionary<(NoteType, PatientType), bool> _appointmentRequirementMap =
        new Dictionary<(NoteType, PatientType), bool>
        {
            { (NoteType.Initial, PatientType.Default), true },
            { (NoteType.Initial, PatientType.Premium), false },
            { (NoteType.Blank, PatientType.Default), false },
            { (NoteType.Blank, PatientType.Premium), false },
            { (NoteType.FollowUp, PatientType.Default), true },
            { (NoteType.FollowUp, PatientType.Premium), false },
            { (NoteType.Internal, PatientType.Default), false },
            { (NoteType.Internal, PatientType.Premium), false },
            { (NoteType.HistoryAndPhysicalInitial, PatientType.Default), true },
            { (NoteType.HistoryAndPhysicalInitial, PatientType.Premium), false },
            { (NoteType.HistoryAndPhysicalFollowUp, PatientType.Default), true },
            { (NoteType.HistoryAndPhysicalFollowUp, PatientType.Premium), false },
            { (NoteType.HistoryAndPhysicalGroupVisit, PatientType.Default), false },
            { (NoteType.HistoryAndPhysicalGroupVisit, PatientType.Premium), false },
            { (NoteType.Soap, PatientType.Default), true },
            { (NoteType.Soap, PatientType.Premium), false },
        };

    public MaterialisableFlowResult Execute()
    {
        ValidateAppointment();
        
        var content = new NoteContent
        {
            Content = Content,
            InternalContent = InternalContent,
            Logs = Logs.Select(x => new NoteLog
            {
                Key = x.Key,
                Value = x.Value
            }).ToList()
        };

        var note = new Note(
            employee: Employee,
            delegatedEmployee: DelegatedEmployee,
            patient: Patient,
            name: Name,
            title: Title,
            type: Type,
            visitDate: VisitDate,
            content: content,
            appointment: Appointment
        )
        {
            Version = Version,
            OriginalNoteId = OriginalNoteId
        };

        return note.Added() + new NoteCreatedEvent(note);
    }
    
    #region private

    private void ValidateAppointment()
    {
        if (Appointment is not null)
        {
            if (Appointment.PatientId != Patient.Id)
            {
                throw new DomainException("Appointment is related to another patient");
            }
        }
        
        var patientDomain = PatientDomain.Create(Patient);
        var patientType = patientDomain.GetPatientType();
        var appointmentRequired = _appointmentRequirementMap[(Type, patientType)];

        if (appointmentRequired)
        {
            if (Appointment is null && !OriginalNoteId.HasValue)
            {
                throw new DomainException("Appointment is required for this note.");
            }
        }
    }
    
    #endregion
}