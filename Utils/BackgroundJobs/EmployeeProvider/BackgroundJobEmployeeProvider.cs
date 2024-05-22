using System;
using System.Linq;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.Roles;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.PasswordUtil;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Models.Employees;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Utils.BackgroundJobs.EmployeeProvider
{
    public class BackgroundJobEmployeeProvider : IBackgroundJobEmployeeProvider
    {
        private IEmployeeService _employeeService { get; }
        private IUsersService _usersService { get; }
        private IPracticeService _practiceService { get; }
        private IRolesService _rolesService { get; }
        private IPasswordUtil _passwordUtil { get; }
        private ILocationsService _locationsService { get; }
        private IAuthService _authService { get; }
        
        public BackgroundJobEmployeeProvider(IEmployeeService employeeService, 
            IUsersService usersService, 
            IPracticeService practiceService, 
            IPasswordUtil passwordUtil, 
            IRolesService rolesService,
            ILocationsService locationsService,
            IAuthService authService)
        {
            _employeeService = employeeService;
            _usersService = usersService;
            _practiceService = practiceService;
            _passwordUtil = passwordUtil;
            _rolesService = rolesService;
            _locationsService = locationsService;
            _authService = authService;
        }

        private static Employee? BackgroundJobEmployee;
        
        public async Task<Employee> GetBackgroundJobEmployee()
        {
            if (BackgroundJobEmployee == null)
            {
                BackgroundJobEmployee = Init().Result;
            }
            var g = Task.FromResult(BackgroundJobEmployee);
            return await g;
        }

        private async Task<Employee> Init()
        {
            var user = await _usersService.GetByEmailAsync(EmployeeConstants.BackgroundJobEmployee.Email);

            if (user is null)
            {
                //create the background job employee
                // decided to do it in run time since we should avoid id managements with seeds and migration files..
                // it should be done once.
                var employee = await CreateUserAndEmployee();

                return employee;
            }
            else
            {
                return await _employeeService.GetByUserIdAsync(user.GetId());
            }
        }

        private async Task<Employee> CreateUserAndEmployee()
        {
            
            var (passwordHash, passwordSalt) = _passwordUtil.CreatePasswordHash(EmployeeConstants.BackgroundJobEmployee.Password);
            var practice = await _practiceService.GetAsync(EmployeeConstants.BackgroundJobEmployee.PracticeId);
            var identity = new UserIdentity(practice)
            {
                Email = EmployeeConstants.BackgroundJobEmployee.Email,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                Type = UserType.Employee,
                IsVerified = true,
                User = new User(new Practice { Id = EmployeeConstants.BackgroundJobEmployee.PracticeId })
                {
                    Email = EmployeeConstants.BackgroundJobEmployee.Email,
                    FirstName = EmployeeConstants.BackgroundJobEmployee.FirstName,
                    LastName = EmployeeConstants.BackgroundJobEmployee.LastName
                }
            };
            
           await _authService.CreateAsync(identity);
           
           var location = await _locationsService.GetByIdAsync(EmployeeConstants.BackgroundJobEmployee.LocationId,
               EmployeeConstants.BackgroundJobEmployee.PracticeId); 

           return await CreateAdminEmployee(identity.User, location);
           
        }
        
        private static UserIdentity CreateIdentity(int id, int practiceId, string email)
        {
            //Password: "Passw0rd!"
            return new UserIdentity(new Practice { Id = practiceId })
            {
                Id = id,
                Email = email,
                PasswordHash = new byte[] { 31, 165, 87, 3, 185, 46, 207, 180, 57, 227, 4, 213, 194, 191, 100, 131, 178, 139, 145, 57, 214, 163, 54, 194, 50, 250, 93, 50, 232, 208, 146, 69, 192, 62, 63, 39, 52, 200, 1, 134, 221, 23, 180, 250, 243, 127, 131, 84, 233, 221, 233, 229, 191, 132, 94, 105, 164, 35, 56, 180, 85, 27, 168, 221 },
                PasswordSalt = new byte[] { 68, 90, 129, 163, 189, 223, 132, 120, 230, 244, 144, 172, 20, 207, 240, 112, 39, 2, 210, 15, 180, 223, 102, 254, 168, 93, 234, 193, 244, 110, 1, 219, 34, 47, 126, 186, 13, 35, 200, 47, 194, 198, 215, 16, 137, 173, 27, 144, 178, 171, 144, 240, 199, 121, 54, 6, 64, 218, 34, 214, 0, 169, 109, 150, 81, 249, 80, 193, 235, 5, 243, 98, 44, 202, 56, 115, 243, 198, 210, 71, 11, 2, 71, 213, 42, 100, 160, 224, 64, 76, 79, 122, 200, 176, 90, 113, 188, 17, 209, 58, 22, 39, 56, 28, 48, 10, 186, 207, 61, 120, 20, 44, 99, 115, 123, 186, 94, 249, 210, 225, 95, 178, 68, 40, 26, 252, 226, 212 },
                Type = UserType.Employee,
            };
        }
        
        private async Task<Employee> CreateAdminEmployee(User user, Location location )
        {
            var employee = new Employee(user.GetId(), Roles.AdminId);

            var employeeDomain = EmployeeDomain.Create(employee);
            
            var role = await _rolesService.GetByIdAsync(Roles.AdminId);

            employeeDomain.UpdatePermissions(role.Permissions.Select(x => x.Permission));

            employeeDomain.UpdateLocations(new[] { location });

            return await _employeeService.CreateAsync(employee);
        }
    }
}

