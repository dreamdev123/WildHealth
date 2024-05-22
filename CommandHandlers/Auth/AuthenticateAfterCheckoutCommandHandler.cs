using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Auth;
using WildHealth.Domain.Entities.Users;
using WildHealth.Licensing.Api.Enums.Licenses;
using WildHealth.Licensing.Api.Models.Licenses;
using WildHealth.Licensing.Api.Services;
using WildHealth.Shared.Exceptions;
using WildHealth.Settings;
using MediatR;
using System;
using Microsoft.Extensions.Logging;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class AuthenticateAfterCheckoutCommandHandler : IRequestHandler<AuthenticateAfterCheckoutCommand, AuthenticationResultModel>
    {
        private readonly IAuthService _authService;
        private readonly IPermissionsService _permissionsService;
        private readonly ILocationsService _locationsService;        
        private readonly IWildHealthLicensingApiService _licenseService;
        private readonly ISettingsManager _settingsManager;
        private readonly ILogger<AuthenticateAfterCheckoutCommandHandler> _logger;

        public AuthenticateAfterCheckoutCommandHandler(
            IAuthService authService,
            IPermissionsService permissionsService, 
            ILocationsService locationsService, 
            IWildHealthLicensingApiService licenseService, 
            ISettingsManager settingsManager,
            ILogger<AuthenticateAfterCheckoutCommandHandler> logger)
        {
            _authService = authService;
            _permissionsService = permissionsService;
            _locationsService = locationsService;
            _licenseService = licenseService;
            _settingsManager = settingsManager;
            _logger = logger;
        }

        public async Task<AuthenticationResultModel> Handle(AuthenticateAfterCheckoutCommand command, CancellationToken cancellationToken)
        {
            var identity = await _authService.GetByEmailAsync(command.Email);

            var external = await GetExternalAuthorization(identity);
            
            await AssertPracticeAccess(command.PracticeId, identity);

            AssertIdentityIsActive(identity);

            var license = await GetLicenseAsync(
                practiceId: identity.Practice.GetId(),
                businessId: identity.Practice.BusinessId
            );
            
            AssertLicenseAccess(license);

            return await _authService.Authenticate(
                identity: identity,
                originPractice: identity.Practice,
                onBehalfPractice: null,
                permissions: await _permissionsService.GetOwnedPermissionsAsync(identity, license.AccessStatus),
                availableLocationsIds: await _locationsService.GetOwnedLocationIdsAsync(identity),
                defaultLocationId: null,
                external: external
            );
        }
        
        #region private

        /// <summary>
        /// Get and returns an external authorization
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        private async Task<ExternalAuthorization?> GetExternalAuthorization(UserIdentity identity)
        {
            try
            {
                return await _authService.GetUserExternalAuthorizationAsync(identity.GetId());
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // If no external identities exist for corresponding user - ignore the error
                return null;
            }            
        }

        /// <summary>
        /// Asserts identity is active
        /// </summary>
        /// <param name="identity"></param>
        private void AssertIdentityIsActive(UserIdentity identity)
        {
            if (!identity.IsActive())
            {
                throw new AppException(HttpStatusCode.Forbidden, "User is not active");
            }
        }

        /// <summary>
        /// Returns patient license
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="businessId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<LicenseModel> GetLicenseAsync(int practiceId, int businessId)
        {
            try
            {
                return await _licenseService.GetLicenseRelatedToPractice(
                    practiceId: practiceId, 
                    businessId: businessId);
            }
            catch(Exception e)
            {
                _logger.LogWarning($"Get license for [PracticeId] = {practiceId}, [BusinessId] = {businessId} has failed with [Error]: {e.ToString()}");
                throw new AppException(HttpStatusCode.NotFound, "License does not exist.");
            }
        }
        
        /// <summary>
        /// Assert license accesses
        /// </summary>
        /// <param name="license"></param>
        /// <exception cref="AppException"></exception>
        private void AssertLicenseAccess(LicenseModel license)
        {
            if (license.AccessStatus == LicenseAccessStatus.Denied)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Access denied.");
            }
        }

        /// <summary>
        /// Assert practice access
        /// </summary>
        /// <param name="sourcePracticeId"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        private async Task AssertPracticeAccess(int sourcePracticeId, UserIdentity identity)
        {
            var hasSeparateUi = await _settingsManager.GetSetting<bool>(SettingsNames.General.HasSeparateUi, identity.PracticeId);
            
            if (hasSeparateUi && sourcePracticeId != identity.PracticeId)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Access denied.");
            }
        }
        
        #endregion
    }
}