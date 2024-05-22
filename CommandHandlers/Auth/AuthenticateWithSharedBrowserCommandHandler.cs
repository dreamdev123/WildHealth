using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Authorization.Factories.AuthorizationServiceFactory;
using WildHealth.Common.Models.Auth;
using WildHealth.Licensing.Api.Services;
using Microsoft.AspNetCore.Http;
using WildHealth.Authorization.Models.Authorization;
using WildHealth.Authorization.Models.Identity;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Licensing.Api.Enums.Licenses;
using WildHealth.Licensing.Api.Models.Licenses;
using WildHealth.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using WildHealth.Authorization.Enums;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class AuthenticateWithSharedBrowserCommandHandler  : IRequestHandler<AuthenticateWithSharedBrowseCommand, AuthenticationResultModel>
    {
        private readonly IExternalAuthorizationServiceFactory _authorizationServiceFactory;
        private readonly IWildHealthLicensingApiService _licenseService;
        private readonly IPermissionsService _permissionsService;
        private readonly ILocationsService _locationsService;        
        private readonly IAuthService _authService;
        private readonly ILogger<AuthenticateWithSharedBrowserCommandHandler> _logger;

        public AuthenticateWithSharedBrowserCommandHandler(
            IExternalAuthorizationServiceFactory authorizationServiceFactory, 
            IWildHealthLicensingApiService licenseService, 
            IPermissionsService permissionsService, 
            ILocationsService locationsService, 
            IAuthService authService,
            ILogger<AuthenticateWithSharedBrowserCommandHandler> logger)
        {
            _authorizationServiceFactory = authorizationServiceFactory;
            _licenseService = licenseService;
            _permissionsService = permissionsService;
            _locationsService = locationsService;
            _authService = authService;
            _logger = logger;
        }

        public async Task<AuthenticationResultModel> Handle(AuthenticateWithSharedBrowseCommand command, CancellationToken cancellationToken)
        {
            var (authResult, externalIdentity) = await AuthorizeAsync(command.Context, command.Provider);
            
            var externalAuth = await _authService.GetExternalAuthorizationAsync(externalIdentity.Id, command.Provider);

            var identity = await _authService.GetByIdAsync(externalAuth.UserId);

            await RefreshAuthorization(externalAuth, authResult);
            
            AssertIdentityIsActive(identity);

            var license = await GetLicenseAsync(
                practiceId: identity.Practice.GetId(),
                businessId: identity.Practice.BusinessId);
            
            AssertLicenseAccess(license);
            
            return await _authService.Authenticate(
                identity: identity, 
                originPractice: identity.Practice,
                onBehalfPractice: null,
                permissions: await _permissionsService.GetOwnedPermissionsAsync(identity, license.AccessStatus),
                availableLocationsIds: await _locationsService.GetOwnedLocationIdsAsync(identity),
                defaultLocationId: null,
                external: externalAuth);
        }
        
        #region private

        /// <summary>
        /// Processes authorization
        /// </summary>
        /// <param name="context"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        private async Task<(ExternalAuthorizationModel, ExternalIdentityModel)> AuthorizeAsync(HttpContext context, AuthorizationProvider provider)
        {
            var externalProvider =  (ExternalAuthorizationProvider) provider;

            var externalAuthorizationService = _authorizationServiceFactory.Create(externalProvider);

            var authResult = await externalAuthorizationService.AuthorizeAsync(context);
            
            var externalIdentity = await externalAuthorizationService.GetIdentityAsync(authResult);

            return (authResult, externalIdentity);
        }

        /// <summary>
        /// Refreshes external authorization
        /// </summary>
        /// <param name="authorization"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task RefreshAuthorization(ExternalAuthorization authorization, ExternalAuthorizationModel model)
        {
            authorization.Refresh(
                accessToken: model.AccessToken,
                refreshToken: model.RefreshToken,
                expirationDate: model.ExpiresIn
            );

            await _authService.UpdateExternalAuthorizationAsync(authorization);
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
            catch (Exception e)
            {
                _logger.LogWarning($"Get license for [PracticeId] = {practiceId}, [BusinessId] = {businessId} has failed with [Error]: {e.ToString()}");
                throw new AppException(HttpStatusCode.NotFound, "License does not exist.");
            }
        }
        
        /// <summary>
        /// Assert license access
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
        
        #endregion
    }
}