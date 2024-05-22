using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Domain.Entities.Users;
using WildHealth.Licensing.Api.Enums.Licenses;
using WildHealth.Licensing.Api.Models.Licenses;
using WildHealth.Licensing.Api.Services;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Common.Models.Auth;
using MediatR;
using Microsoft.Extensions.Logging;
using System;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class ReauthenticateCommandHandler : IRequestHandler<ReauthenticateCommand, AuthenticationResultModel>
    {
        private readonly IAuthTicket _authTicket;
        private readonly IAuthService _authService;
        private readonly IPermissionsService _permissionsService;
        private readonly ILocationsService _locationsService;
        private readonly IWildHealthLicensingApiService _licenseService;
        private readonly IMediator _mediator;
        private readonly ILogger<ReauthenticateCommandHandler> _logger;

        public ReauthenticateCommandHandler(
            IAuthTicket authTicket, 
            IAuthService authService, 
            IPermissionsService permissionsService, 
            ILocationsService locationsService, 
            IWildHealthLicensingApiService licenseService, 
            IMediator mediator,
            ILogger<ReauthenticateCommandHandler> logger)
        {
            _authTicket = authTicket;
            _authService = authService;
            _permissionsService = permissionsService;
            _locationsService = locationsService;
            _licenseService = licenseService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<AuthenticationResultModel> Handle(ReauthenticateCommand command, CancellationToken cancellationToken)
        {
            if (_authTicket.IsOnBehalf())
            {
                return await AuthenticateOnBehalf(command.LocationId);
            }
            
            var identity = await _authService.GetByIdAsync(_authTicket.GetId());
            
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
                defaultLocationId: command.LocationId,
                external: null);
        }
        
        #region private

        /// <summary>
        /// Authenticates user on behalf
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        private async Task<AuthenticationResultModel> AuthenticateOnBehalf(int? locationId)
        {
            var authenticateOnBehalfCommand = new AuthenticateOnBehalfCommand(
                practiceId: _authTicket.GetPracticeId(),
                locationId: locationId
            );
                
            return await _mediator.Send(authenticateOnBehalfCommand);
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