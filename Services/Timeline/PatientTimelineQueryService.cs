using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Models.Timeline;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Timeline;

public class PatientTimelineQueryService : IPatientTimelineQueryService
{
    private readonly IGeneralRepository<PatientTimelineEvent> _repository;

    public PatientTimelineQueryService(IGeneralRepository<PatientTimelineEvent> repository)
    {
        _repository = repository;
    }

    public async Task<List<PatientTimelineDomain>> GetTimelineEvents(int patientId)
    {
        var events = await _repository.All()
            .RelatedToPatient(patientId)
            .Where(x => PatientTimelineDomain.GeneralEventTypes.Contains(x.Type))
            .AsNoTracking()
            .ToListAsync();

        return events.Select(PatientTimelineDomain.Create).ToList();
    }

    public async Task<List<PatientTimelineDomain>> GetPaymentPlanUpdateEvents(int patientId)
    {
        var events = await _repository.All()
            .RelatedToPatient(patientId)
            .Where(x => PatientTimelineDomain.SubscriptionEventTypes.Contains(x.Type))
            .AsNoTracking()
            .ToListAsync();

        return events.Select(PatientTimelineDomain.Create).ToList();
    }
}