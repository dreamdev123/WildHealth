using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using LaunchDarkly.Sdk;
using LaunchDarkly.Sdk.Server;
using WildHealth.Common.Options;
using WildHealth.LaunchDarkly.Clients.WebClient;
using WildHealth.LaunchDarkly.Clients.Models.FeatureFlags;
using WildHealth.LaunchDarkly.Clients.Models.Credentials;

namespace WildHealth.Application.Services.FeatureFlags
{
    /// <summary>
    /// <see cref="IFeatureFlagsService"/>
    /// </summary>
    public class FeatureFlagsService : IFeatureFlagsService
    {
        private readonly ILaunchDarklyWebClient _client;
        private readonly FeatureFlagsOptions _options;
        private readonly LdClient _ldClient;

        private readonly string _flagsUrl;
        private readonly string _flagsEnv;
        private readonly ILogger _logger;

        public FeatureFlagsService(IOptions<FeatureFlagsOptions> options, ILaunchDarklyWebClient client,ILogger<FeatureFlagsService> logger)
        {

            _options = options.Value;
            _ldClient = GetClient();
            _flagsUrl = $"{_options.apiUrl}{_options.flagsPath}";
            _flagsEnv = _options.environment;
            _client = client;
            _client.Initialize(new CredentialsModel(_options.apiUrl, _options.LaunchDarklyAPIKey, "beta", _options.environment));
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IFeatureFlagsService.GetFeatureFlag(string)"/>
        /// </summary>
        /// <param name="flagKey"></param>
        /// <returns></returns>
        public bool GetFeatureFlag(string flagKey)
        {
            return _ldClient.BoolVariation(flagKey, GetUser());
        }

        public bool GetFeatureFlag(string flagKey, Guid universalUserId)
        {
            return _ldClient.BoolVariation(flagKey, GetUser(universalUserId.ToString()));
        }

        /// <summary>
        /// Initializes launch darkly.   Only call this method one time
        /// in the application's lifecycle.
        /// </summary>
        /// <returns></returns>
        private LdClient GetClient()
        {
            // There should only be 1 LdClient instance per sdk key.
            // DependencyRegistrar keeps FeatureFlagsService as a singleton.
            // see:  https://docs.launchdarkly.com/sdk/server-side/dotnet
            return new LdClient(_options.LaunchDarklySDKKey);
        }

        /// <summary>
        /// Initializes user for request
        /// </summary>
        /// <returns></returns>
        private User GetUser(string? key = null)
        {
            return User.WithKey(key ?? _options.UserKey);
        }


        /// <summary>
        /// <see cref="IFeatureFlagsService.GetAllFeatureFlagsRecorded(string)"/>
        /// </summary>
        /// <returns></returns>
        public async Task<FeatureFlagModel[]?> GetAllFeatureFlagsAsync()
        {
            try
            {
                return  await _client.GetAllFlags();
            }
            catch(Exception e)
            {
                _logger.LogError(e.ToString(), e);
                return null;
            }
            
        }      

    }
}
