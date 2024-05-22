using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Engagement;

namespace WildHealth.Application.Domain.PatientEngagements.Services;

public interface IPatientEngagementService
{
    Task<List<EngagementCriteria>> GetNotDisabledCriteria(EngagementAssignee assignee);
    Task<List<PatientEngagement>> GetPending();
    Task<List<PatientEngagement>> GetHistory(int[] patientIds);
    Task<List<PatientEngagement>> GetActive(int[] patientIds, params EngagementCriteriaType[] types);
}