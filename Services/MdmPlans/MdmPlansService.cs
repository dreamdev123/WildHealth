using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Goals;
using WildHealth.Domain.Entities.Notes.Common;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Common.Models.MdmPlans;
using WildHealth.Application.Extensions.Query;

using System.Linq;

namespace WildHealth.Application.Services.MdmPlans;

/// <summary>
/// <see cref="IMdmPlansService"/>
/// </summary>
public class MdmPlansService : IMdmPlansService
{
    private readonly IGeneralRepository<Goal> _goals;
    private readonly IGeneralRepository<CommonGoal> _commonGoalsRepository;
    private readonly IGeneralRepository<CommonMdmPlan> _commonMdmPlansRepository;
    private readonly IGeneralRepository<CommonIntervention> _commonInterventionsRepository;
    private readonly IGeneralRepository<CommonReason> _commonReasonsRepository;

    public MdmPlansService(
        IGeneralRepository<Goal> goals,
        IGeneralRepository<CommonGoal> commonGoalsRepository,
        IGeneralRepository<CommonMdmPlan> commonMdmPlansRepository,
        IGeneralRepository<CommonIntervention> commonInterventionsRepository,
        IGeneralRepository<CommonReason> commonReasonsRepository
    )
    {
        _goals = goals;
        _commonGoalsRepository = commonGoalsRepository;
        _commonMdmPlansRepository = commonMdmPlansRepository;
        _commonInterventionsRepository = commonInterventionsRepository;
        _commonReasonsRepository = commonReasonsRepository;
    }

    /// <summary>
    /// <see cref="IMdmPlansService.GetByIdAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonMdmPlan> GetByIdAsync(int id)
    {
        var commonMdmPlan =  await _commonMdmPlansRepository
            .All()
            .ById(id)
            .WithCommonReasons()
            .FindAsync();

        return commonMdmPlan;
    }
    
    public async Task<CommonReason> GetMdmPlanReasonByIdAsync(int id)
    {
        var commonReason =  await _commonReasonsRepository
            .All()
            .ById(id)
            .FindAsync();

        return commonReason;
    }

    /// <summary>
    /// <see cref="IMdmPlansService.GetCommonMdmPlansAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonMdmPlan[]> GetCommonMdmPlansAsync()
    {
        var commonMdmPlans =  await _commonMdmPlansRepository
            .All()
            .WithCommonReasons()
            .ToArrayAsync();

        return commonMdmPlans;
    }

    /// <summary>
    /// <see cref="IMdmPlansService.CreateCommonMdmPlanAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonMdmPlan> CreateCommonMdmPlanAsync(CommonMdmPlanModel model)
    {
        var newCommonMdmPlan = new CommonMdmPlan
        {
            Name = model.Name,
            Description = model.Description,
        };

        await _commonMdmPlansRepository.AddAsync(newCommonMdmPlan);

        await _commonMdmPlansRepository.SaveAsync();

        return newCommonMdmPlan;
    }

    /// <summary>
    /// <see cref="IMdmPlansService.UpdateCommonMdmPlanAsync"/>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<CommonMdmPlan> UpdateCommonMdmPlanAsync(CommonMdmPlanModel model)
    {
        var commonMdmPlan = await GetByIdAsync(model.Id);

        commonMdmPlan.Name = model.Name;
        commonMdmPlan.Description = model.Description;

        _commonMdmPlansRepository.Edit(commonMdmPlan);

        await _commonMdmPlansRepository.SaveAsync();

        return commonMdmPlan;
    }

    /// <summary>
    /// <see cref="IMdmPlansService.AddCommonMdmPlanReasonAsync"/>
    /// </summary>
    /// <returns></returns>
    public async Task<CommonReason> AddCommonMdmPlanReasonAsync(ReasonModel model)
    {
        var newCommonReason = new CommonReason
        {
            Name = model.Name,
            MdmPlanId = model.MdmPlanId
        };

        await _commonReasonsRepository.AddAsync(newCommonReason);

        await _commonReasonsRepository.SaveAsync();

        return newCommonReason;
    }

    /// <summary>
    /// <see cref="IMdmPlansService.UpdateCommonMdmPlanReasonAsync"/>
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public async Task<CommonReason> UpdateCommonMdmPlanReasonAsync(ReasonModel model)
    {
        var commonReason = await GetMdmPlanReasonByIdAsync(model.Id);

        commonReason.Name = model.Name;

        _commonReasonsRepository.Edit(commonReason);

        await _commonReasonsRepository.SaveAsync();

        return commonReason;
    }

    /// <summary>
    /// <see cref="IMdmPlansService.DeleteCommonMdmPlanAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CommonMdmPlan> DeleteCommonMdmPlanAsync(int id)
    {
        var commonMdmPlan = await GetByIdAsync(id);

        _commonMdmPlansRepository.Delete(commonMdmPlan);

        await _commonMdmPlansRepository.SaveAsync();

        return commonMdmPlan;
    }

    /// <summary>
    /// <see cref="IMdmPlansService.DeleteCommonMdmPlanReasonAsync"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CommonReason> DeleteCommonMdmPlanReasonAsync(int id)
    {
        var commonReason = await GetMdmPlanReasonByIdAsync(id);

        _commonReasonsRepository.Delete(commonReason);

        await _commonReasonsRepository.SaveAsync();

        return commonReason;
    }
}