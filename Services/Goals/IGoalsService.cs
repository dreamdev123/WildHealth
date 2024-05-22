using System.Threading.Tasks;
using WildHealth.Domain.Entities.Goals;
using WildHealth.Domain.Entities.Notes.Common;
using WildHealth.Domain.Enums.Goals;
using WildHealth.Common.Models.Goals;

namespace WildHealth.Application.Services.Goals;

/// <summary>
/// Provides methods for working with goals
/// </summary>
public interface IGoalsService
{
    /// <summary>
    /// Return current patient goals which are not completed yet
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    Task<Goal[]> GetCurrentAsync(int patientId);

    /// <summary>
    /// Selects, querying goals by name and return pages with completed patient goals
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="query"></param>
    /// <param name="skip"></param>
    /// <param name="take"></param>
    /// <returns></returns>
    Task<(Goal[], int)> SelectPastAsync(int patientId, string? query = null, int? skip = null, int? take = null);
    
    /// <summary>
    /// Returns Common Goal by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<CommonGoal> GetByIdAsync(int id);

    /// <summary>
    /// Returns all Common Goals
    /// </summary>
    /// <returns></returns>
    Task<CommonGoal[]> GetCommonGoalsAsync();

    /// <summary>
    /// Returns Common Goals by category
    /// </summary>
    /// <returns></returns>
    Task<CommonGoal[]> GetCommonGoalsByCategoryAsync(GoalCategory category);

    /// <summary>
    /// creates common goal
    /// <param name="model"></param>
    /// </summary>
    /// <returns></returns>
    Task<CommonGoal> CreateCommonGoalAsync(CommonGoalModel model);

    /// <summary>
    /// Returns an updated Common Goal
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<CommonGoal> UpdateCommonGoalAsync(CommonGoalModel model);

    /// <summary>
    /// Add common intervention by CommonGoalId
    /// <param name="model"></param>
    /// </summary>
    /// <returns></returns>
    Task<CommonIntervention> AddCommonInterventionAsync(InterventionModel model);

    /// <summary>
    /// Returns an updated Common Intervention
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<CommonIntervention> UpdateCommonInterventionAsync(InterventionModel model);

    /// <summary>
    /// Deletes Common Goal
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<CommonGoal> DeleteCommonGoalAsync(int id);

    /// <summary>
    /// Deletes Common Integration
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<CommonIntervention> DeleteCommonInterventionAsync(int id);
}