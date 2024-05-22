using System.Collections.Generic;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.Encryption;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Models.Employees;

namespace WildHealth.Application.CommandHandlers.Employees.Flows;

public class CreateEmployeeFlow : IMaterialisableFlow
{
    private readonly IEnumerable<Location> _locations;
    private readonly IEnumerable<Permission> _permissions;
    private readonly Role _role;
    private readonly User _user;
    private readonly string _credentials;
    private readonly string? _npi;
    private readonly string? _rxntUserName;
    private readonly string? _rxntPassword;
    private readonly string _encryptionKey;

    public CreateEmployeeFlow(
        IEnumerable<Location> locations,
        IEnumerable<Permission> permissions,
        Role role,
        User user,
        string credentials,
        string? npi,
        string encryptionKey,
        string? rxntUserName = null,
        string? rxntPassword = null)
    {
        _locations = locations;
        _permissions = permissions;
        _role = role;
        _user = user;
        _credentials = credentials;
        _npi = npi;
        _encryptionKey = encryptionKey;
        _rxntUserName = rxntUserName;
        _rxntPassword = rxntPassword;
    }

    public MaterialisableFlowResult Execute()
    {
        var employee = new Employee(user: _user, role: _role);
        var employeeDomain = EmployeeDomain.Create(employee);

        employeeDomain.UpdateCredentials(_credentials);

        employeeDomain.UpdatePermissions(_permissions);

        employeeDomain.UpdateLocations(_locations);

        if (employeeDomain.IsProvider())
        {
            employeeDomain.UpdateNpi(_npi);
        }

        var encryptedRxntPassword = _rxntPassword == null ? null : WhEncryptor.Encrypt(_rxntPassword, _encryptionKey);
        employeeDomain.AddRxNtIntegration(_rxntUserName, encryptedRxntPassword);

        return employee.Added();
    }
}