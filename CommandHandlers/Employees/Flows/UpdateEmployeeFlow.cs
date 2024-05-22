using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Models.Employees;
using System.Linq;
using WildHealth.Application.Utils.Encryption;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;

public class UpdateEmployeeFlow : IMaterialisableFlow
{
    private readonly Employee _employee;
    private readonly Location[] _newLocations;
    private readonly Permission[] _newPermissions;
    private readonly Role _newRole;
    private readonly string _schedulerAccountId;
    private readonly string _credentials;
    private readonly string? _npi;
    private readonly string _encryptionKey;
    private readonly string? _rxntUserName;
    private readonly string? _rxntPassword;

    public UpdateEmployeeFlow(Employee employee,
        Location[] newLocations,
        Permission[] newPermissions,
        Role newRole,
        string schedulerAccountId,
        string credentials,
        string? npi,
        string encryptionKey,
        string? rxntUserName = null,
        string? rxntPassword = null)
    {
        _employee = employee;
        _newLocations = newLocations;
        _newPermissions = newPermissions;
        _newRole = newRole;
        _schedulerAccountId = schedulerAccountId;
        _credentials = credentials;
        _npi = npi;
        _encryptionKey = encryptionKey;
        _rxntUserName = rxntUserName;
        _rxntPassword = rxntPassword;
    }

    public MaterialisableFlowResult Execute()
    {
        var employeeDomain = EmployeeDomain.Create(_employee);

        employeeDomain.UpdateSchedulerAccountId(_schedulerAccountId);

        employeeDomain.UpdateCredentials(_credentials);

        UpdateNpiNumber();

        UpdateRole();
        
        UpdateLocations();

        UpdatePermissions();

        UpdateRxntIntegration();
        
        return _employee.Updated().ToFlowResult();
    }

    private void UpdateRxntIntegration()
    {
        // Manage RxNT UserName integration
        var userNameIntegration = _employee.Integrations.FirstOrDefault(x =>
            x.Integration.Purpose == IntegrationPurposes.Employee.RxntUserName &&
            x.Integration.Vendor == IntegrationVendor.RxNT);
        
        // RxNT UserName changed
        if (_rxntUserName != userNameIntegration?.Integration?.Value)
        {
            // RxNT UserName added
            if (userNameIntegration == null && !string.IsNullOrEmpty(_rxntUserName)) 
                _employee.Integrations.Add(new EmployeeIntegration(IntegrationVendor.RxNT, IntegrationPurposes.Employee.RxntUserName, _rxntUserName, _employee)); // add new integration
            // RxNT UserName removed
            else if (userNameIntegration != null && string.IsNullOrEmpty(_rxntUserName))
                _employee.Integrations = _employee.Integrations.Where(i => i != userNameIntegration).ToList(); // remove integration
            // RxNT UserName modified
            else if (userNameIntegration != null && !string.IsNullOrEmpty(_rxntUserName))
                userNameIntegration.Integration!.Value = _rxntUserName;
        }
        
        // Manage RxNT Password integration
        var passwordIntegration = _employee.Integrations.FirstOrDefault(x =>
            x.Integration.Purpose == IntegrationPurposes.Employee.RxntPassword &&
            x.Integration.Vendor == IntegrationVendor.RxNT);

        // RxNT Password changed
        var encryptedRxntPassword = _rxntPassword == null ? null : WhEncryptor.Encrypt(_rxntPassword, _encryptionKey);
        if (encryptedRxntPassword != passwordIntegration?.Integration?.Value)
        {
            // RxNT Password added
            if (passwordIntegration == null && !string.IsNullOrEmpty(_rxntPassword)) 
                _employee.Integrations.Add(new EmployeeIntegration(IntegrationVendor.RxNT, IntegrationPurposes.Employee.RxntPassword, encryptedRxntPassword, _employee)); // add new integration
            // RxNT Password removed
            else if (passwordIntegration != null && string.IsNullOrEmpty(_rxntPassword))
                _employee.Integrations = _employee.Integrations.Where(i => i != passwordIntegration).ToList(); // remove integration
            // RxNT Password modified
            else if (passwordIntegration != null && !string.IsNullOrEmpty(_rxntPassword))
                passwordIntegration.Integration!.Value = encryptedRxntPassword;
        }
    }

    private void UpdateRole()
    {
        if (_employee.RoleId != _newRole.Id)
        {
            var employeeDomain = EmployeeDomain.Create(_employee);
            employeeDomain.UpdateRole(_newRole);
        }
    }

    private bool IsNeedToUpdateLocations()
    {
        if (_employee.Locations.Count != _newLocations.Length)
        {
            return true;
        }

        var newLocationIds = _newLocations.Select(x => x.GetId());
        if (!_employee.Locations.All(x => newLocationIds.Contains(x.LocationId)))
        {
            return true;
        }

        return false;
    }

    private void UpdateLocations()
    {
        if (IsNeedToUpdateLocations())
        {
            var employeeDomain = EmployeeDomain.Create(_employee);
            employeeDomain.UpdateLocations(_newLocations);
        }
    }

    private bool IsNeedToUpdatePermissions()
    {
        var existingPermissionIds = _employee.Permissions.Select(o => o.PermissionId);
        
        var permissionIds = _newPermissions.Select(x => x.GetId()).ToArray();
        if (!_employee.Permissions.All(x => permissionIds.Contains(x.PermissionId)) ||
            !permissionIds.All(x => existingPermissionIds.Contains(x)))
        {
            return true;
        }

        return false;
    }

    private void UpdatePermissions()
    {
        if (IsNeedToUpdatePermissions())
        {
            var employeeDomain = EmployeeDomain.Create(_employee);
            employeeDomain.UpdatePermissions(_newPermissions);
        }
    }

    private void UpdateNpiNumber()
    {
        var employeeDomain = EmployeeDomain.Create(_employee);
        if (employeeDomain.IsProvider())
        {
            employeeDomain.UpdateNpi(_npi);
        }
    }
}