using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Goals;
using WildHealth.Domain.Entities.Notes.Common;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Domain.Enums.Goals;
using WildHealth.Common.Models.Goals;
using WildHealth.Application.Extensions.Query;

using System.Linq;

namespace WildHealth.Application.Services.Goals;

/// <summary>
/// <see cref="IGoalsService"/>
/// </summary>
public class GoalsService : IGoalsService
{
    private readonly IGeneralRepository<Goal> _goals;
    private readonly IGeneralRepository<CommonGoal> _commonGoalsRepository;
    private readonly IGeneralRepository<CommonIntervention> _commonInterventionsRepository;

    public GoalsService(
        IGeneralRepository<Goal> goals,
        IGeneralRepository<CommonGoal> commonGoalsRepository,
        IGeneralRepository<CommonIntervention> commonInterventionsRepository
    )
    {
        _goals = goals;
        _commonGoalsRepository = commonGoalsRepository;
        _commonInterventionsRepository = commonInterventionsRepository;
    }

    /// <summary>
    /// <see cref="IGoalsService.GetCurrentAsync"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    public Task<Goal[]> GetCurrentAsync(int patientId)
    {
        return _goals
            .All()
            .NotDeleted()
            .RelatedToPatient(patientId)
            .WithInterventions()
            .Completed(false)
            .ToArrayAsync();
    }

    /// <summary>
    /// <see cref="IGoalsService.SelectPastAsync"/>
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="query"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <returns></returns>
    public async Task<(Goal[], int)> SelectPastAsync(int patientId, string? query, int? skip = null, int? take = null)
    {
        var queryData = _goals
            .All()
            .NotDeleted()
            .RelatedToPatient(patientId)
            .WithInterventions()
            .Completed(true)
            .Query(query)
            .OrderByDescending(o => o.CreatedAt);
            
        var totalCount = await queryData.CountAsync();
        var goals = await queryData.Pagination(skip, take).ToArrayAsync();
        return (goals, totalCount);
    }

    /// <summary>
    /// <see cref="IGoalsService.GetByIdAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonGoal> GetByIdAsync(int id)
    {
        var commonGoal =  await _commonGoalsRepository
            .All()
            .ById(id)
            .WithCommonInterventions()
            .FindAsync();

        return commonGoal;
    }
    
    public async Task<CommonIntervention> GetInterventionByIdAsync(int id)
    {
        var commonIntervention =  await _commonInterventionsRepository
            .All()
            .ById(id)
            .FindAsync();

        return commonIntervention;
    }

    /// <summary>
    /// <see cref="IGoalsService.GetCommonGoalsAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonGoal[]> GetCommonGoalsAsync()
    {
        var commonGoals =  await _commonGoalsRepository
            .All()
            .WithCommonInterventions()
            .ToArrayAsync();

        return commonGoals;
    }

    /// <summary>
    /// <see cref="IGoalsService.GetCommonGoalsByCategoryAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonGoal[]> GetCommonGoalsByCategoryAsync(GoalCategory category)
    {
        var commonGoals =  await _commonGoalsRepository
            .All()
            .ByCategory(category)
            .WithCommonInterventions()
            .ToArrayAsync();

        return commonGoals;
    }

    /// <summary>
    /// <see cref="IGoalsService.CreateCommonGoalAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonGoal> CreateCommonGoalAsync(CommonGoalModel model)
    {
        var newCommonGoal = new CommonGoal
        {
            Name = model.Name,
            Category = model.Category,
            CompletionValue = model.CompletionValue,
            IsCompleted = model.IsCompleted
        };

        await _commonGoalsRepository.AddAsync(newCommonGoal);

        await _commonGoalsRepository.SaveAsync();

        return newCommonGoal;
    }

    /// <summary>
    /// <see cref="IGoalsService.UpdateCommonGoalAsync"/>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<CommonGoal> UpdateCommonGoalAsync(CommonGoalModel model)
    {
        var commonGoal = await GetByIdAsync(model.Id);

        commonGoal.Name = model.Name;
        commonGoal.Category = model.Category;
        commonGoal.CompletionValue = model.CompletionValue;
        commonGoal.IsCompleted = model.IsCompleted;

        _commonGoalsRepository.Edit(commonGoal);

        await _commonGoalsRepository.SaveAsync();

        return commonGoal;
    }

    /// <summary>
    /// <see cref="IGoalsService.AddCommonInterventionAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonIntervention> AddCommonInterventionAsync(InterventionModel model)
    {
        var newCommonIntervention = new CommonIntervention
        {
            Name = model.Name,
            GoalId = model.GoalId
        };

        await _commonInterventionsRepository.AddAsync(newCommonIntervention);

        await _commonInterventionsRepository.SaveAsync();

        return newCommonIntervention;
    }

    /// <summary>
    /// <see cref="IGoalsService.UpdateCommonInterventionAsync"/>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<CommonIntervention> UpdateCommonInterventionAsync(InterventionModel model)
    {
        var commonIntervention = await GetInterventionByIdAsync((int)model.Id);

        commonIntervention.Name = model.Name;

        _commonInterventionsRepository.Edit(commonIntervention);

        await _commonInterventionsRepository.SaveAsync();

        return commonIntervention;
    }

    /// <summary>
    /// <see cref="IGoalsService.DeleteCommonGoalAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CommonGoal> DeleteCommonGoalAsync(int id)
    {
        var commonGoal = await GetByIdAsync(id);

        _commonGoalsRepository.Delete(commonGoal);

        await _commonGoalsRepository.SaveAsync();

        return commonGoal;
    }

    /// <summary>
    /// <see cref="IGoalsService.DeleteCommonInterventionAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CommonIntervention> DeleteCommonInterventionAsync(int id)
    {
        var commonIntervention = await GetInterventionByIdAsync(id);

        _commonInterventionsRepository.Delete(commonIntervention);

        await _commonInterventionsRepository.SaveAsync();

        return commonIntervention;
    }
}