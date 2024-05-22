using System.Linq;
using System.Net;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Shared.Enums;
using WildHealth.Domain.Interfaces;
using WildHealth.Domain.Entities.Practices;

namespace WildHealth.Application.Utils.PermissionsGuard
{
    /// <summary>
    /// <see cref="IPermissionsGuard"/>
    /// </summary>
    public class PermissionsGuard : IPermissionsGuard
    {
        private readonly IAuthTicket _authTicket;

        public PermissionsGuard(IAuthTicket authTicket)
        {
            _authTicket = authTicket;
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.IsHighestRole()"/>
        /// </summary>
        /// <returns></returns>
        public bool IsHighestRole()
        {
            if (_authTicket.GetUserType() != UserType.Employee)
            {
                return false;
            }

            var ownRoleId = _authTicket.GetRoleId();

            if (ownRoleId is null)
            {
                return false;
            }

            return IsHighestRole(ownRoleId.Value);
            
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.IsAssistRole()"/>
        /// </summary>
        /// <returns></returns>
        public bool IsAssistRole()
        {
            var ownRoleId = _authTicket.GetRoleId();
            
            return Roles.VirtualAssistantId == ownRoleId;
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.IsAssistRole(int)"/>
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        public bool IsAssistRole(int roleId)
        {
            return Roles.VirtualAssistantId == roleId;
        }

        public void AssertAdminUser(User? user)
        {
            if (!IsHighestRole())
            {
                throw new AppException(HttpStatusCode.Unauthorized, $"You have insufficient permissions.");
            }
            AssertPermissions(user);
        }

        public bool HasPermission(Employee employee, PermissionType permissionType)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return true;
            }

            if (IsHighestRole(employee.RoleId))
            {
                return true;
            }
            
            return employee.Permissions.Any(x => x.PermissionId == (int)permissionType);
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.IsHighestRole(int)"/>
        /// </summary>
        /// <returns></returns>
        public bool IsHighestRole(int roleId)
        {
            var hierarchyLevel = Roles.Hierarchy[roleId];

            return Roles.Hierarchy.All(x => x.Value >= hierarchyLevel);
        }

        /// <summary>
        /// Verifies permissions to control the user.  This method allows
        /// the current user to control their own record. 
        /// </summary>
        /// <param name="user">the user that will be accessed or modified</param>
        /// <exception cref="AppException"></exception>
        public void AssertUserPermissions(User user)
        {
            //Is the requester an employee in the user's practice? If so, ok.
            //Is the requester the user? If so, ok.
            //Otherwise, permission is denied.
            if (_authTicket.GetUserType() == UserType.Employee)
            {
                AssertPermissions(user);
            }
            else if (_authTicket.GetId() == user.Id)
            {
                //ok
            }
            else
            {
                throw new AppException(HttpStatusCode.Unauthorized, $"You have no permissions for this user.");
            }
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPermissions(IPracticeRelated)"/>
        /// </summary>
        /// <param name="entity"></param>
        public void AssertPermissions(IPracticeRelated? entity)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            if (_authTicket.IsOnBehalf())
            {
                var originPracticeId = _authTicket.GetOriginPracticeId();
                var onBehalfPracticeId = _authTicket.GetPracticeId();
                
                if (originPracticeId != entity?.PracticeId && onBehalfPracticeId != entity?.PracticeId)
                {
                    throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this practice.");
                }
            }
            else
            {
                if (_authTicket.GetPracticeId() != entity?.PracticeId)
                {
                    throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this practice.");
                }
            }
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPermissions(ILocationRelated)"/>
        /// </summary>
        /// <param name="entity"></param>
        public void AssertPermissions(ILocationRelated entity)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            var availableLocationIds = _authTicket.GetAvailableLocationIds();
            
            if (!availableLocationIds.Contains(entity.LocationId))
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this pod.");
            }
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPermissions(IPatientRelated)"/>
        /// </summary>
        /// <param name="entity"></param>
        public void AssertPermissions(IPatientRelated entity)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            AssertPermissions(entity.Patient);
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPermissions(Location)"/>
        /// </summary>
        /// <param name="location"></param>
        public void AssertPermissions(Location location)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            try
            {
                AssertPermissions(location as IPracticeRelated);
            }
            catch(AppException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this pod.");
            }

            var ownRoleId = _authTicket.GetRoleId();

            if (ownRoleId == Roles.PortalAdminId)
            {
                return;
            }

            AssertLocationIdPermissions(location.GetId());
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertLocationIdPermissions(int)"/>
        /// </summary>
        /// <param name="locationId"></param>
        public void AssertLocationIdPermissions(int locationId)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            if (IsHighestRole()) 
            {
                return;
            }

            var availableLocationIds = _authTicket.GetAvailableLocationIds();

            if (availableLocationIds.All(x => x != locationId))
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this pod.");
            }
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPermissions(Patient)"/>
        /// </summary>
        /// <param name="patient"></param>
        public void AssertPermissions(Patient patient)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            try
            {
                AssertPermissions(patient.User);

                if (IsHighestRole()) 
                {
                    return;
                }

                AssertPermissions(patient as ILocationRelated);
            }
            catch(AppException ex) when (ex.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this patient.");
            }
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPermissions(Employee)"/>
        /// </summary>
        /// <param name="employee"></param>
        public void AssertPermissions(Employee employee)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            AssertPermissions(employee.User);
            
            if (IsHighestRole()) 
            {
                return;
            }

            var availableLocationIds = _authTicket.GetAvailableLocationIds();

            if (employee.Locations.All(x => !availableLocationIds.Contains(x.LocationId)))
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this employee.");
            }
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPermissions(Role)"/>
        /// </summary>
        /// <param name="role"></param>
        public void AssertPermissions(Role role)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            var onwRoleId = _authTicket.GetRoleId();

            if (onwRoleId is null)
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this role.");
            }

            if (!role.IsProtected)
            {
                return;
            }

            if (Roles.Hierarchy[role.GetId()] < Roles.Hierarchy[onwRoleId.Value])
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this role.");
            }
        }

        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPermissions(Permission[])"/>
        /// </summary>
        /// <param name="permissions"></param>
        public void AssertPermissions(Permission[] permissions)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            var ownPermissions = _authTicket.GetPermission();

            var protectedPermissions = permissions.Where(x => x.IsProtected);

            if (!protectedPermissions.All(x => ownPermissions.Contains(x.GetPermission())))
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission");
            }
        }


        /// <summary>
        /// <see cref="IPermissionsGuard.AssertPracticePermissions"/>
        /// </summary>
        /// <param name="practice"></param>
        public void AssertPracticePermissions(Practice practice)
        {
            // Avoid all permissions check if operation processing by background process
            if (_authTicket.IsBackgroundProcess())
            {
                return;
            }
            
            var availablePracticeId = _authTicket.GetPracticeId();

            if (availablePracticeId != practice.Id)
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this practice.");
            }
        }
    }
}