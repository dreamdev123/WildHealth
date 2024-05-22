using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Application.Services.Practices;
using WildHealth.Domain.Entities.Users;
using WildHealth.Licensing.Api.Enums.Licenses;
using WildHealth.Licensing.Api.Models.Licenses;
using WildHealth.Licensing.Api.Services;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Common.Models.Auth;
using MediatR;
using System;
using Microsoft.Extensions.Logging;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class AuthenticateOnBehalfCommandHandler : IRequestHandler<AuthenticateOnBehalfCommand, AuthenticationResultModel>
    {
        private readonly IAuthTicket _authTicket;
        private readonly IAuthService _authService;
        private readonly IPracticeService _practiceService;
        private readonly ILocationsService _locationsService; 
        private readonly IPermissionsService _permissionsService;
        private readonly IWildHealthLicensingApiService _licenseService;
        private readonly ILogger<AuthenticateOnBehalfCommandHandler> _logger;

        public AuthenticateOnBehalfCommandHandler(
            IAuthTicket authTicket, 
            IAuthService authService, 
            IPracticeService practiceService, 
            ILocationsService locationsService, 
            IPermissionsService permissionsService, 
            IWildHealthLicensingApiService licenseService,
            ILogger<AuthenticateOnBehalfCommandHandler> logger)
        {
            _authTicket = authTicket;
            _authService = authService;
            _practiceService = practiceService;
            _locationsService = locationsService;
            _permissionsService = permissionsService;
            _licenseService = licenseService;
            _logger = logger;
        }

        public async Task<AuthenticationResultModel> Handle(AuthenticateOnBehalfCommand command, CancellationToken cancellationToken)
        {
            var userId = _authTicket.GetId();
            var identity = await _authService.GetByIdAsync(userId);
            
            AssertIdentityIsActive(identity);

            var license = await GetLicenseAsync(
                practiceId: identity.Practice.GetId(),
                businessId: identity.Practice.BusinessId);
            
            AssertLicenseAccess(license);
            
            return await _authService.Authenticate(
                identity: identity,
                originPractice: identity.Practice,
                onBehalfPractice: await _practiceService.GetAsync(command.PracticeId),
                permissions: await _permissionsService.GetOwnedPermissionsAsync(identity, license.AccessStatus),
                availableLocationsIds: await GetPracticeLocationsAsync(command.PracticeId),
                defaultLocationId: command.LocationId,
                external: null);
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
        /// Returns practice location ids
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        private async Task<int[]> GetPracticeLocationsAsync(int practiceId)
        {
            var allLocations = await _locationsService.GetAllAsync(practiceId);

            return allLocations.Select(x => x.GetId()).ToArray();
        }
        
        #endregion
    }
}