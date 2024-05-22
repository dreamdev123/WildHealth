using System.Threading.Tasks;
using WildHealth.Common.Models.Onboarding;


namespace WildHealth.Application.Services.Onboarding
{
    /// <summary>
    /// Provides methods for working with Onboarding configurations
    /// </summary>
    public interface IOnboardingService
    {
        /// <summary>
        /// Fetches and returns onboarding configuration for a given practice
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<OnboardingConfigurationModel> GetOnboardingConfiguration(int practiceId);
    }
}