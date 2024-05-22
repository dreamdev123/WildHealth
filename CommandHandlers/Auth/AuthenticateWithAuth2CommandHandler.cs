using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Common.Models.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Authorization.Factories.AuthorizationServiceFactory;
using WildHealth.Domain.Enums.User;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Application.Services.Users;
using WildHealth.Authorization.Models.Authorization;
using WildHealth.Authorization.Models.Identity;
using WildHealth.Domain.Entities.Users;
using WildHealth.Licensing.Api.Enums.Licenses;
using WildHealth.Licensing.Api.Models.Licenses;
using WildHealth.Licensing.Api.Services;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Commands.Users;
using WildHealth.Shared.Enums;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Settings;
using WildHealth.Common.Constants;
using WildHealth.Authorization.Services;
using Microsoft.Extensions.Logging;
using WildHealth.Authorization.Enums;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class AuthenticateWithAuth2CommandHandler  : IRequestHandler<AuthenticateWithAuth2Command, AuthenticationResultModel>
    {
        private readonly IExternalAuthorizationServiceFactory _authorizationServiceFactory;
        private readonly IWildHealthLicensingApiService _licenseService;
        private readonly IPermissionsService _permissionsService;
        private readonly ILocationsService _locationsService;        
        private readonly IAuthService _authService;
        private readonly IUsersService _usersService;
        private readonly IMediator _mediator;
        private readonly ISettingsManager _settingsManager;
        private readonly ILogger<AuthenticateWithAuth2CommandHandler> _logger;

        public AuthenticateWithAuth2CommandHandler(
            IExternalAuthorizationServiceFactory authorizationServiceFactory, 
            IWildHealthLicensingApiService licenseService, 
            IPermissionsService permissionsService, 
            ILocationsService locationsService,
            IAuthService authService,
            IUsersService usersService,
            IMediator mediator,
            ISettingsManager settingsManager,
            ILogger<AuthenticateWithAuth2CommandHandler> logger)
        {
            _authorizationServiceFactory = authorizationServiceFactory;
            _licenseService = licenseService;
            _permissionsService = permissionsService;
            _locationsService = locationsService;
            _authService = authService;
            _usersService = usersService;
            _mediator = mediator;
            _settingsManager = settingsManager;
            _logger = logger;
        }

        public async Task<AuthenticationResultModel> Handle(AuthenticateWithAuth2Command command, CancellationToken cancellationToken)
        {
            var (authResult, externalIdentity) = await AuthorizeAsync(command.Code, command.Provider);

            var externalAuth = await GetOrCreateExternalAuth(externalIdentity, authResult, command, cancellationToken);

            if (externalAuth == null) throw new AppException(HttpStatusCode.Forbidden, "User is not active");
            
            var identity = await _authService.GetByIdAsync(externalAuth.UserId);

            await RefreshAuthorization(externalAuth, authResult);
            
            AssertIdentityIsActive(identity);

            await AssertPracticeAccess(GetPracticeIdForProvider(command.Provider), identity);

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
        /// <param name="code"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        private async Task<(ExternalAuthorizationModel, ExternalIdentityModel)> AuthorizeAsync(string code, AuthorizationProvider provider)
        {
            var externalProvider = (ExternalAuthorizationProvider) provider;

            var externalAuthorizationService = _authorizationServiceFactory.Create(externalProvider);

            var authResult = await AuthorizeExternalAsync(externalAuthorizationService, code);

            var externalIdentity = await externalAuthorizationService.GetIdentityAsync(authResult);

            return (authResult, externalIdentity);
        }

        private async Task<ExternalAuthorizationModel> AuthorizeExternalAsync(IExternalAuthorizationService service, string code)
        {
            try
            {
                return await service.AuthorizeAsync(code);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Authorize external has failed with [Error]: {e}");
                throw new AppException(HttpStatusCode.Unauthorized, "Invalid external authorization code.");
            }
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
        /// Assert practice access
        /// </summary>
        /// <param name="sourcePracticeId"></param>
        /// <param name="identity"></param>
        /// <returns></returns>
        private async Task AssertPracticeAccess(int sourcePracticeId, UserIdentity identity)
        {
            var hasSeparateUi = await _settingsManager.GetSetting<bool>(SettingsNames.General.HasSeparateUi, identity.PracticeId);
            
            var isSourcePracticeMain = await _settingsManager.GetSetting<bool>(SettingsNames.General.IsMainPractice, sourcePracticeId);

            if ((hasSeparateUi && sourcePracticeId != identity.PracticeId) || (!hasSeparateUi && !isSourcePracticeMain))
            {
                throw new AppException(HttpStatusCode.Unauthorized, "Access denied - Incompatible Practice Detected");
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

        /// <summary>
        /// Checks to see if the pass ExternalAuthorizationModel was successful
        /// </summary>
        /// <param name="authResult"></param>
        private bool IsAuthSuccess(ExternalAuthorizationModel authResult)
        {
            return String.IsNullOrEmpty(authResult.Error);
        }

        /// <summary>
        /// Get a practice Id for a given authorization provider
        /// </summary>
        /// <param name="provider"></param>
        private int GetPracticeIdForProvider(AuthorizationProvider provider)
        {
            switch(provider)
            {
                case AuthorizationProvider.Google:
                    return (int)PlanPlatform.WildHealth;

                default:
                    throw new AppException(HttpStatusCode.BadRequest, "Problem assigning practice to authenticated user");
            }
        }
        
        /// <summary>
        /// Get or create an external authorization
        /// </summary>
        /// <param name="externalIdentity"></param>
        /// <param name="authResult"></param>
        /// <param name="command"></param>
        /// <param name="cancellationToken"></param>
        private async Task<ExternalAuthorization?> GetOrCreateExternalAuth(ExternalIdentityModel externalIdentity, ExternalAuthorizationModel authResult, AuthenticateWithAuth2Command command, CancellationToken cancellationToken)
        {
            ExternalAuthorization? externalAuth = null;

            try
            {
                externalAuth = await _authService.GetExternalAuthorizationAsync(externalIdentity.Id, command.Provider);
            } 
            catch(AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // If this comes back with nothing we want to check to see if the auth with the external entity was successful, if it was
                // then we want to create an entry here
                if(IsAuthSuccess(authResult))
                {

                    var user = await _usersService.GetByEmailAsync(externalIdentity.Email);
                    var dob = DateTime.TryParse(externalIdentity.Birthday, out var date)
                        ? date
                        : new DateTime?();
                    
                    if(user is null)
                    {
                        // Critical that we only create a user in this spot.  At this point we don't know if this will be an employee or patient.
                        var createUserCommand = new CreateUserCommand(
                            firstName: externalIdentity.FirstName,
                            lastName: externalIdentity.LastName,
                            email: externalIdentity.Email,
                            phoneNumber: string.Empty,
                            password: Guid.NewGuid().ToString(),
                            birthDate: dob,
                            gender: Gender.None,
                            userType: UserType.Unspecified,
                            practiceId: GetPracticeIdForProvider(command.Provider),
                            billingAddress: null,
                            shippingAddress: null,
                            isVerified: true,
                            isRegistrationCompleted: false);

                        user = await _mediator.Send(createUserCommand, cancellationToken);
                    }

                    // Create the external authorization
                    externalAuth = await _authService.CreateExternalAuthorizationAsync(new ExternalAuthorization(
                        user,
                        externalIdentity.Id,
                        command.Code,
                        authResult.AccessToken,
                        authResult.RefreshToken,
                        authResult.ExpiresIn,
                        command.Provider
                    ));
                }
            }

            return externalAuth;
        }


        #endregion
    }
}