using System.Threading.Tasks;
using WildHealth.Common.Models.Onboarding;
using WildHealth.Settings;
using WildHealth.Common.Constants;


namespace WildHealth.Application.Services.Onboarding
{
    /// <summary>
    /// Provides methods for working with Onboarding configurations
    /// </summary>
    public class OnboardingService : IOnboardingService
    {
        private readonly ISettingsManager _settingsManager;

        private static readonly string[] OnboardingSettings =
        {
            SettingsNames.Onboarding.ConsultationFormId
        };

        public OnboardingService(
            ISettingsManager settingsManager
        ) {
            _settingsManager = settingsManager;
        }

        
        /// <summary>
        /// Fetches and returns onboarding configuration for a given practice
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<OnboardingConfigurationModel> GetOnboardingConfiguration(int practiceId)
        {
            var settings = await _settingsManager.GetSettings(OnboardingSettings, practiceId);

            return new OnboardingConfigurationModel(
                practiceId: practiceId,
                consultationFormId: settings[SettingsNames.Onboarding.ConsultationFormId]
            );
        }
    }
}


