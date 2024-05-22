using System;
using System.Linq;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Utils.Patients;

public class PatientFilterHelper : IPatientFilterHelper
{
    public PatientStatusModel[] HandlePatientFilter(PatientStatusModel[] patientStatusModels,
        MyPatientsFilterModel filter,
        Employee emp)
    {
        var results = patientStatusModels.Where(p => 
            HandlePatientFilter(p, filter, emp)
        ).ToArray();
        
        return results;
    }

    public PatientStatusModel[] HandlePatientFilterWithoutAssigment(PatientStatusModel[] patientStatusModel,
        MyPatientsFilterModel filter)
    {
        var results = patientStatusModel.Where(p => 
            HandlePatientFilterWithoutAssigment(p, filter)
        ).ToArray();
        
        return results;
    }

    public bool HandlePatientFilter(PatientStatusModel patientStatusModel, 
                                    MyPatientsFilterModel filter,
                                    Employee emp)
    {
        
        var empIsAssigned = EmployeeIsAssigned(patientStatusModel, emp.Id);
        var empIdMatches = empIsAssigned || emp.Role.DisplayName == WildHealth.Domain.Constants.Roles.LocationDirectorDisplayName;
        
        if (!empIdMatches)
        {
            //If this situation is the case, then nothing else matters, because this patient is not
            //under purview of the employee. 
            return false;
        }

        return HandlePatientFilterWithoutAssigment(patientStatusModel,filter);
    }
    
    public bool HandlePatientFilterWithoutAssigment(PatientStatusModel patientStatusModel, 
        MyPatientsFilterModel filter)
    {
        if (AllFiltersAreNull(filter))
        {
            return true;
        }
        
        //This is a very strict search.  
        //If a filter is null, we can exclude the patient from the result set at this point.
        //Because we know another filter is set somewhere.
        var tagsMatch = FilterTags(patientStatusModel, filter);
        var lastAppointmentMatch = FilterLastAppointment(patientStatusModel, filter);
        var lastCoachingVisitMatch = FilterLastCoachingVisit(patientStatusModel, filter);
        var lastMessageSentMatch = FilterLastMessageSent(patientStatusModel, filter);
        var daysSinceIccWithoutImcScheduledMatch = FilterSinceIcc(patientStatusModel, filter);
        var planRenewalDateMatch = FilterRenewalDate(patientStatusModel, filter);
        var daysSinceSignupWithoutIccScheduledMatch = FilterNoIcc(patientStatusModel, filter);
        var planNameMatch = FilterPlan(patientStatusModel, filter);
        var fumdMatch = FilterFuMedicalAppointment(patientStatusModel, filter);

        var ok = tagsMatch || lastAppointmentMatch || lastCoachingVisitMatch
                 || lastMessageSentMatch || daysSinceIccWithoutImcScheduledMatch
                 || planRenewalDateMatch || daysSinceSignupWithoutIccScheduledMatch || planNameMatch
                 || fumdMatch;
        return ok;
    }

    private bool AllFiltersAreNull(MyPatientsFilterModel filter)
    {
        return (filter.IncludesTags == null || filter.IncludesTags.Length == 0)
               && !filter.LastAppointmentGreaterThanDaysAgo.HasValue
               && !filter.LastCoachingVisitGreaterThanDaysAgo.HasValue
               && !filter.LastMessageSentGreaterThanDaysAgo.HasValue
               && !filter.DaysSinceIccWithoutImcScheduledFromToday.HasValue
               && !filter.PlanRenewalDateLessThanDaysFromToday.HasValue
               && !filter.DaysSinceSignUpWithoutIccScheduledFromToday.HasValue
               && filter.PlanName == null
               && filter.HasFollowUpMedicalScheduled == null;
    }

    private bool EmployeeIsAssigned(PatientStatusModel patientStatusModel, int? empId)
    {
        if (patientStatusModel.AssignedEmployees == null || !empId.HasValue)
        {
            return false;
        }
        return patientStatusModel.AssignedEmployees.Contains(empId.Value);
    }

    private bool FilterRenewalDate(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        return WithinXDaysInFuture(patientStatusModel.RenewalDate, filter.PlanRenewalDateLessThanDaysFromToday);
    }

    private bool FilterSinceIcc(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        return patientStatusModel.DaysSinceSignUpAndNoIccScheduled > filter.DaysSinceIccWithoutImcScheduledFromToday;
    }

    private bool FilterLastMessageSent(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        if (patientStatusModel.LastMessage == null && filter.LastMessageSentGreaterThanDaysAgo != null)
        {
            //There is no last message, so include it.
            return true;
        }
        return DateMoreThanXDaysAgo(patientStatusModel.LastMessage?.SentDate, filter.LastMessageSentGreaterThanDaysAgo);
    }

    private bool FilterNoIcc(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        return patientStatusModel.DaysSinceSignUpAndNoIccScheduled > filter.DaysSinceSignUpWithoutIccScheduledFromToday;
    }

    private bool FilterLastCoachingVisit(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        if (patientStatusModel.LastAppointmentCompletedHealthCoachDate == null &&
            filter.LastCoachingVisitGreaterThanDaysAgo != null)
        {
            //There is no previous coaching visit, so include it.
            return true;
        }
        return DateMoreThanXDaysAgo(patientStatusModel.LastAppointmentCompletedHealthCoachDate, filter.LastCoachingVisitGreaterThanDaysAgo);
    }

    private bool FilterLastAppointment(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        if (patientStatusModel.LastAppointment == null && filter.LastAppointmentGreaterThanDaysAgo != null)
        {
            //There is no last appointment, but the caller is counting the days, so include it.
            return true;
        }
        return DateMoreThanXDaysAgo(patientStatusModel.LastAppointment?.StartDate, filter.LastAppointmentGreaterThanDaysAgo);
    }
    
    private bool WithinXDaysInFuture(DateTime? subject, int? x)
    {
        if (x.HasValue)
        {
            if (subject.HasValue)
            {
                var days = subject.Value.Subtract(DateTime.UtcNow).Days;
                return days < x.Value;
            }
        }
        //The caller is not filtering by this field.
        return false;
    }
    
    private bool DateMoreThanXDaysAgo(DateTime? subject, int? x)
    {
        if (x.HasValue)
        {
            if (subject.HasValue)
            {
                var days = DateTime.UtcNow.Subtract(subject.Value).Days;
                return days > x.Value;
            }
        }
        //The caller is not filtering by the given date.
        return false;
    }

    private bool FilterTags(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        if (filter.IncludesTags == null || filter.IncludesTags.Length == 0)
        {
            //The caller is not filtering based on tags.
            return false;
        }
        
        if ( (patientStatusModel.Tags == null || patientStatusModel.Tags.Length == 0) && 
             filter.IncludesTags.Length > 0) 
        {
            //The caller is using tags to filter, but the patient has no tags.
            //No match.
            return false;
        }
        
        if( (patientStatusModel.Tags == null || patientStatusModel.Tags.Length == 0) &&
            (filter.IncludesTags == null || filter.IncludesTags.Length == 0))
        {
            //There is no meaningful tags filter articulated.
            return false;
        }

        foreach (var t in filter.IncludesTags)
        {
            if (patientStatusModel.Tags?.Any(pt => pt.Name == t) ?? false)
            {
                return true;
            }
        }
        
        //There were tags in the filter and on the patient, but none of them matched.
        return false;
    }

    private bool FilterPlan(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        return filter.PlanName != null && 
               patientStatusModel.ActivePlan?.Name == filter.PlanName;
    }

    private bool FilterFuMedicalAppointment(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter)
    {
        return filter.HasFollowUpMedicalScheduled != null && 
               patientStatusModel.LastAppointmentScheduledProviderDate >= DateTime.UtcNow && patientStatusModel.LastAppointmentScheduledProviderPurpose == AppointmentPurpose.FollowUp;
    }
}