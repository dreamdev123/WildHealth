using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.DistributedCache.Services;

namespace WildHealth.Application.Services.Roles
{
    /// <summary>
    /// <see cref="IRolesService"/>
    /// </summary>
    public class RolesService : IRolesService
    {
        private readonly IGeneralRepository<Role> _rolesRepository;
        private readonly IAuthTicket _authTicket;
        private readonly IWildHealthSpecificCacheService<Role, Role[]> _wildHealthSpecificCacheRolesService;

        public RolesService(
            IGeneralRepository<Role> rolesRepository, 
            IAuthTicket authTicket,
            IWildHealthSpecificCacheService<Role, Role[]> wildHealthSpecificCacheRolesService)
        {
            _rolesRepository = rolesRepository;
            _authTicket = authTicket;
            _wildHealthSpecificCacheRolesService = wildHealthSpecificCacheRolesService;
        }

        /// <summary>
        /// <see cref="IRolesService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Role> GetByIdAsync(int id)
        {
            var role = await _rolesRepository
                .All()
                .Include(x => x.Permissions)
                .ThenInclude(x => x.Permission)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (role is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Role does not exist", exceptionParam);
            }

            return role;
        }

        /// <summary>
        /// <see cref="IRolesService.GetAvailableAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<Role[]> GetAvailableAsync()
        {
            var ownRoleId = _authTicket.GetRoleId();
            var hierarchy = WildHealth.Domain.Constants.Roles.Hierarchy;
            if (ownRoleId is null)
            {
                return Array.Empty<Role>();
            }

            var allRoles = await GetAllRoles();
            
            var availableRoles = allRoles
                .Where(x => !x.IsProtected || hierarchy[x.GetId()] >= hierarchy[ownRoleId.Value])
                .ToArray();

            return availableRoles.Select(FilterRolePermissions).ToArray();
        }

        #region private 

        /// <summary>
        /// Filter role permissions according to current permission
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        private Role FilterRolePermissions(Role role)
        {
            var permissions = _authTicket.GetPermission();

            role.Permissions = role.Permissions.Where(x => permissions.Contains(x.Permission.GetPermission())).ToArray();

            return role;
        }


        /// <summary>
        /// Get all roles from cache
        /// </summary>
        /// <returns></returns>
        private async Task<Role[]> GetAllRoles()
        {
            return await _wildHealthSpecificCacheRolesService
                .GetAsync($"{WildHealth.Domain.Constants.Roles.PortalAdminId.GetHashCode()}",
                    async () => await _rolesRepository.All()
                        .Include(x => x.Permissions)
                        .ThenInclude(x => x.Permission)
                        .ToArrayAsync());
        }
        
        #endregion
    }
}