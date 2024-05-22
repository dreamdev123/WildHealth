using MediatR;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Auth;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.ConfirmCodes;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Permissions;
using WildHealth.Common.Models.Auth;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Licensing.Api.Services;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Auth
{
    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthenticationResultModel>
    {
        private readonly IAuthService _authService;
        private readonly IConfirmCodeService _confirmCodeService;
        private readonly ILocationsService _locationsService;
        private readonly IPermissionsService _permissionsService;
        private readonly IWildHealthLicensingApiService _licenseService;

        public RefreshTokenCommandHandler(
            IAuthService authService,
            ILocationsService locationsService,
            IPermissionsService permissionsService,
            IWildHealthLicensingApiService licenseService,
            IConfirmCodeService confirmCodeService)
        {
            _authService = authService;
            _confirmCodeService = confirmCodeService;
            _locationsService = locationsService;
            _permissionsService= permissionsService;
            _licenseService = licenseService;
        }


        public async Task<AuthenticationResultModel> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            var confirmCode = await _confirmCodeService.ConfirmAsync(request.RefreshToken, ConfirmCodeType.RefreshToken);

            var identity = await _authService.GetByEmailAsync(confirmCode.User.Email);

            AssertIdentityIsActive(identity);

            var license = await _licenseService.GetLicenseRelatedToPractice(
                practiceId: identity.Practice.GetId(), 
                businessId: identity.Practice.BusinessId);

            var authResultModel = await _authService.Authenticate(
                identity: identity,
                originPractice: identity.Practice,
                onBehalfPractice: null,
                permissions: await _permissionsService.GetOwnedPermissionsAsync(identity, license.AccessStatus),
                availableLocationsIds: await _locationsService.GetOwnedLocationIdsAsync(identity),
                defaultLocationId: null,
                external: null
            );

            var newConfirmCode = await _confirmCodeService.GenerateAsync(identity.User, ConfirmCodeType.RefreshToken);

            var refreshTokenModel = new RefreshTokenModel
            {
                ExpirationDate = newConfirmCode.ExpireAt,
                RefreshToken = newConfirmCode.Code
            };

            authResultModel.RefreshTokenModel = refreshTokenModel;

            return authResultModel;
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
    }
}
