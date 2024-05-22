using System.Threading.Tasks;
using WildHealth.Domain.Entities.Notes.Common;
using WildHealth.Common.Models.MdmPlans;

namespace WildHealth.Application.Services.MdmPlans;

/// <summary>
/// Provides methods for working with Common MDM plans
/// </summary>
public interface IMdmPlansService
{
    /// <summary>
    /// Returns Common MDM Plan by Id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<CommonMdmPlan> GetByIdAsync(int id);

    /// <summary>
    /// Returns all Common MDM Plans
    /// </summary>
    /// <returns></returns>
    Task<CommonMdmPlan[]> GetCommonMdmPlansAsync();

    /// <summary>
    /// creates Common MDM Plan
    /// <param name="model"></param>
    /// </summary>
    /// <returns></returns>
    Task<CommonMdmPlan> CreateCommonMdmPlanAsync(CommonMdmPlanModel model);

    /// <summary>
    /// Returns an updated Common MDM Plan
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<CommonMdmPlan> UpdateCommonMdmPlanAsync(CommonMdmPlanModel model);

    /// <summary>
    /// Add Common MDM Plan Reason by CommonMdmPlanId
    /// <param name="model"></param>
    /// </summary>
    /// <returns></returns>
    Task<CommonReason> AddCommonMdmPlanReasonAsync(ReasonModel model);

    /// <summary>
    /// Returns an updated Common MDM Plan Reason
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    Task<CommonReason> UpdateCommonMdmPlanReasonAsync(ReasonModel model);

    /// <summary>
    /// Deletes Common MDM Plan
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<CommonMdmPlan> DeleteCommonMdmPlanAsync(int id);

    /// <summary>
    /// Deletes Common MDM Plan Reason
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<CommonReason> DeleteCommonMdmPlanReasonAsync(int id);
}