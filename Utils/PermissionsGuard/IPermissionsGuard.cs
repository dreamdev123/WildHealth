using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Interfaces;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Utils.PermissionsGuard
{
    /// <summary>
    /// Provides permissions guard
    /// </summary>
    public interface IPermissionsGuard
    {
        /// <summary>
        /// Returns is current identity has highest role
        /// </summary>
        /// <returns></returns>
        bool IsHighestRole();
        
        /// <summary>
        /// Returns if current identity has assist role
        /// </summary>
        /// <returns></returns>
        bool IsAssistRole();
        
        /// <summary>
        /// Returns if current identity has assist role
        /// </summary>
        /// <returns></returns>
        bool IsAssistRole(int roleId);

        /// <summary>
        /// Returns is this the highest role
        /// </summary>
        /// <returns></returns>
        bool IsHighestRole(int roleId);
        
        /// <summary>
        /// Ensures the auth ticket is for an administrator in the user's practice.
        /// </summary>
        /// <returns></returns>
        void AssertAdminUser(User? u);

        /// <summary>
        /// Ensure the auth ticket has specified permission
        /// </summary>
        /// <param name="permissionType"></param>
        /// <param name="employee"></param>
        /// <returns></returns>
        bool HasPermission(Employee employee, PermissionType permissionType);

        #region generic

        /// <summary>
        /// Asserts permission for practice related entity
        /// </summary>
        /// <param name="entity"></param>
        void AssertPermissions(IPracticeRelated? entity);

        /// <summary>
        /// Asserts permission for location related entity
        /// </summary>
        /// <param name="entity"></param>
        void AssertPermissions(ILocationRelated entity);

        /// <summary>
        /// Asserts permission for patient related entity
        /// </summary>
        /// <param name="entity"></param>
        void AssertPermissions(IPatientRelated entity);

        #endregion

        #region coreesponding

        /// <summary>
        /// Verifies permissions to control the user.  Also allows
        /// the current user to control their own record. 
        /// </summary>
        /// <param name="user"></param>
        void AssertUserPermissions(User user);

        /// <summary>
        /// Asserts permission for manage patient 
        /// </summary>
        /// <param name="location"></param>
        void AssertPermissions(Location location);

        /// <summary>
        /// Asserts permission for location id 
        /// </summary>
        /// <param name="locationId"></param>
        void AssertLocationIdPermissions(int locationId);

        /// <summary>
        /// Asserts permission for manage patient 
        /// </summary>
        /// <param name="patient"></param>
        void AssertPermissions(Patient patient);

        /// <summary>
        /// Asserts permission for manage employee 
        /// </summary>
        /// <param name="employee"></param>
        void AssertPermissions(Employee employee);

        /// <summary>
        /// Asserts permission for manage role 
        /// </summary>
        /// <param name="role"></param>
        void AssertPermissions(Role role);

        /// <summary>
        /// Asserts permission for permission types
        /// </summary>
        /// <param name="permissions"></param>
        void AssertPermissions(Permission[] permissions);

        /// <summary>
        /// Asserts permissions for practice
        /// </summary>
        /// <param name="practice"></param>
        void AssertPracticePermissions(Practice practice);

        #endregion
    }
}