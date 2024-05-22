using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.PatientJourney;
using WildHealth.Domain.Enums.PatientJourney;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.PatientJourney.Services;

public interface IPatientJourneyService
{
    Task<JourneyTask> GetJourneyTask(int journeyTaskId);
    Task<PatientJourneyTask> GetPatientJourneyTask(int patientId, int journeyTaskId);
    Task<List<JourneyTask>> GetJourneyTasks(int[] ids);
    Task<List<PatientJourneyTask>> GetPatientJourneyTasks(int patientId, int[] journeyTaskIds);
}

public class PatientJourneyService : IPatientJourneyService
{
    private readonly IGeneralRepository<JourneyTask> _journeyTasksRepository;

    private readonly IGeneralRepository<PatientJourneyTask> _patientJourneyTasksRepository;

    public PatientJourneyService(
        IGeneralRepository<JourneyTask> journeyTasksRepository, 
        IGeneralRepository<PatientJourneyTask> patientJourneyTasksRepository)
    {
        _journeyTasksRepository = journeyTasksRepository;
        _patientJourneyTasksRepository = patientJourneyTasksRepository;
    }

    public async Task<JourneyTask> GetJourneyTask(int journeyTaskId)
    {
        return await _journeyTasksRepository.All()
            .ById(journeyTaskId)
            .FindAsync();
    }

    public async Task<PatientJourneyTask> GetPatientJourneyTask(int patientId, int journeyTaskId)
    {
        return await _patientJourneyTasksRepository.All()
            .Where(x => x.PatientId == patientId && x.JourneyTaskId == journeyTaskId)
            .FindAsync();
    }

    public async Task<List<JourneyTask>> GetJourneyTasks(int[] ids)
    {
        return await _journeyTasksRepository.All()
            .ByIds(ids)
            .ToListAsync();
    }

    public async Task<List<PatientJourneyTask>> GetPatientJourneyTasks(int patientId, int[] journeyTaskIds)
    {
        return await _patientJourneyTasksRepository.All()
            .Where(x => x.PatientId == patientId && journeyTaskIds.Contains(x.JourneyTaskId))
            .ToListAsync();
    }
}