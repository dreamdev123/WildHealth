using System;
using System.Threading.Tasks;
using WildHealth.LaunchDarkly.Clients.Models.FeatureFlags;


namespace WildHealth.Application.Services.FeatureFlags
{
    /// <summary>
    /// Provides method for working with feature flags
    /// </summary>
    public interface IFeatureFlagsService
    {
        /// <summary>
        /// Returns the state of a feature flag
        /// </summary>
        /// <param name="flagKey"></param>
        /// <returns></returns>
        bool GetFeatureFlag(string flagKey);

        /// <summary>
        /// Returns the state of a feature flag
        /// </summary>
        /// <param name="flagKey"></param>
        /// <param name="universalUserId"></param>
        /// <returns></returns>
        bool GetFeatureFlag(string flagKey, Guid universalUserId);
        
        /// <summary>
        /// Returns all available feature flags from LD
        /// </summary>
        /// <returns></returns>
        public Task<FeatureFlagModel[]?> GetAllFeatureFlagsAsync();
    }
}