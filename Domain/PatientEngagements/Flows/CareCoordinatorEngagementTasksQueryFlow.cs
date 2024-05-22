using System;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Models.PatientTasks;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Engagement;

using static WildHealth.Domain.Entities.Engagement.EngagementAssignee;
using static WildHealth.Domain.Entities.Engagement.EngagementNotificationType;
using static WildHealth.Domain.Entities.Engagement.PatientEngagementStatus;

namespace WildHealth.Application.Domain.PatientEngagements.Flows;

public record CareCoordinatorEngagementTasksQueryFlow(IQueryable<PatientEngagement> Source, int[] Locations, DateTime Timestamp) : IQueryFlow<CareCoordinatorEngagementTaskModel>
{
    public IQueryable<CareCoordinatorEngagementTaskModel> Execute()
    {
        return Source
            .Where(x =>
                Locations.Contains(x.Patient.LocationId) &&
                x.ExpirationDate.Date >= Timestamp && // not expired
                (x.EngagementCriteria.NotificationType & Dashboard) == Dashboard &&
                (x.EngagementCriteria.Assignee & CareCoordinator) == CareCoordinator &&
                (x.Status == InProgress || x.Status == PendingAction) &&
                !x.EngagementCriteria.IsDisabled)
            .Select(pe => new CareCoordinatorEngagementTaskModel
            {
                PatientId = pe.PatientId,
                PatientName = $"{pe.Patient.User.FirstName} {pe.Patient.User.LastName}",
                NotificationTitle = pe.EngagementCriteria.Name,
                CreatedAt = pe.CreatedAt,
                AssignedHealthCoaches = pe.Patient.Employees
                    .Where(e => e.Employee.RoleId == Roles.CoachId && e.DeletedAt == null)
                    .Select(e => $"{e.Employee.User.FirstName} {e.Employee.User.LastName}"),
                AssignedProviders = pe.Patient.Employees
                    .Where(e => e.Employee.RoleId == Roles.ProviderId && e.DeletedAt == null)
                    .Select(e => $"{e.Employee.User.FirstName} {e.Employee.User.LastName}"),
                IsPremium = pe.IsPremium
            })
            .OrderByDescending(pe => pe.CreatedAt);
    }
}