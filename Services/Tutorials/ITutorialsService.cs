using System.Threading.Tasks;
using WildHealth.Domain.Entities.Tutorials;
using WildHealth.Common.Models.Tutorials;

namespace WildHealth.Application.Services.Tutorials
{
    /// <summary>
    /// Provides method for working with tutorials
    /// </summary>
    public interface ITutorialsService
    {
        /// <summary>
        /// Returns tutorialStatus by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TutorialStatus> GetTutorialStatusByIdAsync(int id);

        /// <summary>
        /// Check TutorialStatus
        /// </summary>
        /// <param name="tutorialName"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<TutorialStatus> CheckFirstView(string tutorialName, int userId);

        /// <summary>
        /// Acknowledge the tutorial for the given user
        /// </summary>
        /// <param name="statusId"></param>
        /// <returns></returns>
        Task<TutorialStatus> AcknowledgeTutorialStatus(int statusId);

        /// <summary>
        /// Deletes tutorialStatus log
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TutorialStatus> DeleteAsync(int id);

        /// <summary>
        /// Returns an updated tutorialStatus log
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<TutorialStatus> UpdateAsync(TutorialStatusModel model);
    }
}
