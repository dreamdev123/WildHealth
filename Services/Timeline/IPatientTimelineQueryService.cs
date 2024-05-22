using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Models.Timeline;

namespace WildHealth.Application.Services.Timeline;

public interface IPatientTimelineQueryService
{
    Task<List<PatientTimelineDomain>> GetTimelineEvents(int patientId);
    Task<List<PatientTimelineDomain>> GetPaymentPlanUpdateEvents(int patientId);
}