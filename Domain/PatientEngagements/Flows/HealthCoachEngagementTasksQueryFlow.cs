using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.HealthCoachEngagement;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Payments;
using static WildHealth.Domain.Entities.Engagement.EngagementAssignee;
using static WildHealth.Domain.Entities.Engagement.PatientEngagementStatus;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record HealthCoachEngagementTasksQueryFlow(
    IQueryable<PatientEmployee> Source, 
    int HealthCoachId, 
    int Count, 
    DateTime Timestamp) : IQueryFlow<HealthCoachEngagementTaskModel>
{
    public IQueryable<HealthCoachEngagementTaskModel> Execute()
    {
        var inProgressAndCompleted = Source
            .Where(e => e.EmployeeId == HealthCoachId && e.DeletedAt == null)
            .SelectMany(e => e.Patient.PatientEngagements)
            .Where(x =>
                x.ExpirationDate.Date >= Timestamp.Date && // not expired
                (x.EngagementCriteria.Assignee & HealthCoach) == HealthCoach &&
                (x.Status == InProgress || x.Status == PendingAction || (x.Status == Completed && x.ModifiedAt!.Value.Date == Timestamp.Date)) &&
                !x.EngagementCriteria.IsDisabled)
            .Select(engagement => new
            {
                engagement,
                subscription = engagement.Patient.Subscriptions.OrderBy(s => s.EndDate).FirstOrDefault()
            })
            .Where(x => 
                x.subscription!.CanceledAt == null && 
                x.subscription.EndDate.Date >= Timestamp.Date && 
                x.subscription.Pauses.All(p => p.Status != SubscriptionPauseStatus.Active)) // active membership
            .OrderBy(x => x.engagement.EngagementCriteria.Priority)
            .ThenByDescending(x => x.subscription!.Price)
            .ThenBy(x => EF.Functions.DateDiffDay(x.subscription!.EndDate, Timestamp)) // fewer days to renewal would have priority.
            .Take(Count);

        return inProgressAndCompleted
            .Where(x => x.engagement.Status != Completed) // skip completed
            .Select(x => new HealthCoachEngagementTaskModel
            {
                PatientId = x.engagement.PatientId,
                EngagementId = x.engagement.Id!.Value,
                PatientFullName = $"{x.engagement.Patient.User.FirstName} {x.engagement.Patient.User.LastName}",
                EventName = x.engagement.EngagementCriteria.Name,
            });
    }
}