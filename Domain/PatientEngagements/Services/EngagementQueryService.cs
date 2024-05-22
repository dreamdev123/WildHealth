using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Domain.PatientEngagements.Flows;
using WildHealth.Application.Extensions.Query;
using WildHealth.Common.Models.HealthCoachEngagement;
using WildHealth.Common.Models.PatientTasks;
using WildHealth.Domain.Entities.Engagement;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Domain.PatientEngagements.Services;

/// <summary>
/// Represents Patient Engagement tasks
/// </summary>
public class EngagementQueryService : IEngagementQueryService
{
    private readonly IGeneralRepository<PatientEngagement> _engagementRepository;
    private readonly IGeneralRepository<PatientEmployee> _patientEmployeeRepository;
    private readonly IEngagementTaskCountCalculator _engagementTaskCountCalculator;
    
    public EngagementQueryService(
        IGeneralRepository<PatientEngagement> engagementRepository, 
        IGeneralRepository<PatientEmployee> patientEmployeeRepository, 
        IEngagementTaskCountCalculator engagementTaskCountCalculator)
    {
        _engagementRepository = engagementRepository;
        _patientEmployeeRepository = patientEmployeeRepository;
        _engagementTaskCountCalculator = engagementTaskCountCalculator;
    }

    public async Task<PatientEngagementTaskModel> GetPatientEngagementTask(int patientId, DateTime timestamp)
    {
        var task = await _engagementRepository.All()
            .Query(source => new PatientTaskQueryFlow(source, patientId, timestamp))
            .FindAsync();

        return task;    
    }

    public async Task<CareCoordinatorEngagementTaskModel[]> GetCareCoordinatorEngagementTasks(int[] locations, DateTime timestamp)
    {
        var tasks = await _engagementRepository.All()
            .Query(source => new CareCoordinatorEngagementTasksQueryFlow(source, locations, timestamp))
            .ToArrayAsync();

        return tasks;
    }

    public async Task<HealthCoachEngagementTaskModel[]> GetHealthCoachEngagementTasks(int healthCoachId, int userId, DateTime timestamp)
    {
        var taskCountForToday = await _engagementTaskCountCalculator.GetEngagementTaskCountForToday(userId, timestamp);
        
        var tasks = await _patientEmployeeRepository.All()
            .Query(source => new HealthCoachEngagementTasksQueryFlow(source, healthCoachId, taskCountForToday.Count, timestamp))
            .ToArrayAsync();

        return tasks;
    }

    public async Task<HealthCoachEngagementTaskModel[]> FindMoreHealthCoachEngagementTasks(int healthCoachId, int userId, DateTime timestamp)
    {
        var activeTasksCount = (await GetHealthCoachEngagementTasks(healthCoachId, userId, timestamp)).Length;
        
        var taskCountForToday = await _engagementTaskCountCalculator.RaiseEngagementTaskCountForToday(userId, activeTasksCount, timestamp);
        
        var tasks = await _patientEmployeeRepository.All()
            .Query(source => new HealthCoachEngagementTasksQueryFlow(source, healthCoachId, taskCountForToday.Count, timestamp))
            .ToArrayAsync();

        return tasks;
    }
}