using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Application.Utils.PasswordUtil;
using WildHealth.Domain.Entities.Users;
using WildHealth.Licensing.Api.Enums.Licenses;
using WildHealth.Licensing.Api.Models.Licenses;
using WildHealth.Licensing.Api.Services;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Models.Auth;
using MediatR;
using WildHealth.Settings;
using WildHealth.Application.Events.Users;
using Microsoft.Extensions.Logging;
using System;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class AuthenticateCommandHandler : IRequestHandler<AuthenticateCommand, AuthenticationResultModel>
    {
        private readonly IAuthService _authService;
        private readonly IPasswordUtil _passwordUtil;
        private readonly IPermissionsService _permissionsService;
        private readonly ILocationsService _locationsService;        
        private readonly IWildHealthLicensingApiService _licenseService;
        private readonly ISettingsManager _settingsManager;
        private readonly IConfirmCodeService _confirmCodeService;
        private readonly IMediator _mediator;
        private readonly ILogger<AuthenticateCommandHandler> _logger;

        public AuthenticateCommandHandler(
            IAuthService authService, 
            IPasswordUtil passwordUtil, 
            IPermissionsService permissionsService, 
            ILocationsService locationsService, 
            IWildHealthLicensingApiService licenseService,
            ISettingsManager settingsManager,
            IMediator mediator,
            ILogger<AuthenticateCommandHandler> logger,
            IConfirmCodeService confirmCodeService)
        {
            _authService = authService;
            _passwordUtil = passwordUtil;
            _permissionsService = permissionsService;
            _locationsService = locationsService;
            _licenseService = licenseService;
            _settingsManager = settingsManager;
            _mediator = mediator;
            _logger = logger;
            _confirmCodeService = confirmCodeService;
        }

        public async Task<AuthenticationResultModel> Handle(AuthenticateCommand command, CancellationToken cancellationToken)
        {
            var identity = await _authService.GetByEmailAsync(command.Email);

            AssertIdentityIsActive(identity);

            VerifyPassword(identity, command.Password);

            var license = await GetLicenseAsync(
                practiceId: identity.Practice.GetId(),
                businessId: identity.Practice.BusinessId);
            
            AssertLicenseAccess(license);

            var authResultModel = await _authService.Authenticate(
                identity: identity,
                originPractice: identity.Practice,
                onBehalfPractice: null,
                permissions: await _permissionsService.GetOwnedPermissionsAsync(identity, license.AccessStatus),
                availableLocationsIds: await _locationsService.GetOwnedLocationIdsAsync(identity),
                defaultLocationId: null,
                external: null
            );

            var confirmCode = await _confirmCodeService.GenerateAsync(identity.User, ConfirmCodeType.RefreshToken);

            var refreshTokenModel = new RefreshTokenModel 
            { 
                ExpirationDate = confirmCode.ExpireAt, 
                RefreshToken = confirmCode.Code 
            };

            authResultModel.RefreshTokenModel = refreshTokenModel;
            
            await _mediator.Publish(new UserAuthenticatedEvent(identity.User));

            return authResultModel;
        }
        
        #region private
        
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
        /// Verifies password
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="password"></param>
        private void VerifyPassword(UserIdentity identity, string password)
        {
            if (!_passwordUtil.VerifyPasswordHash(password, identity.PasswordHash, identity.PasswordSalt))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Incorrect password");
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