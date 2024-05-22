using System;
using System.Threading.Tasks;
using WildHealth.Common.Models.HealthCoachEngagement;
using WildHealth.Common.Models.PatientTasks;

namespace WildHealth.Application.Domain.PatientEngagements.Services;

public interface IEngagementQueryService
{
    Task<PatientEngagementTaskModel> GetPatientEngagementTask(int patientId, DateTime timestamp);
    Task<CareCoordinatorEngagementTaskModel[]> GetCareCoordinatorEngagementTasks(int[] locations, DateTime timestamp);
    Task<HealthCoachEngagementTaskModel[]> GetHealthCoachEngagementTasks(int healthCoachId, int userId, DateTime timestamp);
    Task<HealthCoachEngagementTaskModel[]> FindMoreHealthCoachEngagementTasks(int healthCoachId, int userId, DateTime timestamp);
}