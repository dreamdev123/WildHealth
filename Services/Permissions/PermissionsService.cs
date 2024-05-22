using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Data.Repository;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Licensing.Api.Enums.Licenses;
using WildHealth.Shared.DistributedCache.Services;

namespace WildHealth.Application.Services.Permissions
{
    /// <summary>
    /// <see cref="IPermissionsService"/>
    /// </summary>
    public class PermissionsService : IPermissionsService
    {
        private readonly IGeneralRepository<Permission> _permissionsRepository;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IAuthTicket _authTicket;
        private readonly IWildHealthSpecificCacheService<PermissionsService, IEnumerable<Permission>> _wildHealthSpecificCachePermissionsService;


        public PermissionsService(
            IGeneralRepository<Permission> permissionsRepository, 
            IPermissionsGuard permissionsGuard,
            IAuthTicket authTicket,
            IWildHealthSpecificCacheService<PermissionsService, IEnumerable<Permission>> wildHealthSpecificCachePermissionsService)
        {
            _permissionsRepository = permissionsRepository;
            _permissionsGuard = permissionsGuard;
            _authTicket = authTicket;
            _wildHealthSpecificCachePermissionsService = wildHealthSpecificCachePermissionsService;
        }

        /// <summary>
        /// <see cref="IPermissionsService.GetAllAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Permission>> GetAllAsync()
        {
            var permissions = await _wildHealthSpecificCachePermissionsService
                .GetAsync($"{WildHealth.Domain.Constants.Roles.PortalAdminId.GetHashCode()}",
                    async () => await _permissionsRepository
                        .All()
                        .AsNoTracking()
                        .ToArrayAsync());
           

            return permissions;
        }

        /// <summary>
        /// <see cref="IPermissionsService.GetAvailableAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Permission>> GetAvailableAsync()
        {
            var allPermissions = await GetAllAsync();

            if (_permissionsGuard.IsHighestRole())
            {
                return allPermissions;
            }

            var ownPermissions = _authTicket.GetPermission();

            var availablePermissions = allPermissions
                .Where(x => ownPermissions.Contains((PermissionType) x.GetId()))
                .ToArray();

            return availablePermissions;
        }

        /// <summary>
        /// <see cref="IPermissionsService.GetAsync"/>
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Permission[]> GetAsync(IEnumerable<int> ids)
        {
            var allPermissions = await GetAllAsync();
            
            var permissions = allPermissions
                .Where(x => ids.Contains(x.Id!.Value))
                .ToArray();

            var missingPermissions = ids
                .Where(x => permissions.All(k => k.Id != x))
                .ToArray();

            if (missingPermissions.Any())
            {
                throw new AppException(HttpStatusCode.NotFound, "Some permissions does not exist.");
            }

            return permissions;
        }

        /// <summary>
        /// <see cref="IPermissionsService.GetOwnedPermissionsAsync"/>
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="accessStatus"></param>
        /// <returns></returns>
        public async Task<PermissionType[]> GetOwnedPermissionsAsync(UserIdentity identity, LicenseAccessStatus accessStatus)
        {
            switch(identity.Type)
            {
                case UserType.Employee:
                    if (_permissionsGuard.IsHighestRole(identity.User.Employee.RoleId))
                    {
                        var allPermissions = await GetAllAsync();
                        
                        return allPermissions.Select(x => x.GetPermission()).ToArray();
                    }

                    if (accessStatus == LicenseAccessStatus.ViewOnly)
                    {
                        return new[] { PermissionType.ViewOnly };
                    }

                    return identity.User.Employee.Permissions.Select(c => c.GetPermission()).ToArray();

                case UserType.Patient:
                case UserType.Unspecified:
                default: 
                    return Array.Empty<PermissionType>();

            }
        }
    }
}