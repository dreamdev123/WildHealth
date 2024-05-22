using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Practices;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Events.Schedulers;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.LeadSources;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Services.Roles;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.Schedulers.Accounts;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Enums.Location;
using WildHealth.Domain.Enums.User;
using WildHealth.Domain.Models.Employees;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Enums;
using WildHealth.Application.Services.Employees;
using Newtonsoft.Json;
using MediatR;
using System;

namespace WildHealth.Application.CommandHandlers.Practices
{
    public class RegisterPracticeCommandHandler : IRequestHandler<RegisterPracticeCommand, Practice>
    {
        private readonly IPracticeService _practicesService;
        private readonly ITransactionManager _transactionManager;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly ILeadSourcesService _leadSourcesService;
        private readonly ILocationsService _locationsService;
        private readonly IAddOnsService _addOnsService;
        private readonly IRolesService _rolesService;
        private readonly IEmployeeService _employeeService;
        private readonly ISchedulerAccountService _schedulerAccountService;
        private readonly IMediator _mediator;
        private readonly ILogger<RegisterPracticeCommandHandler> _logger;

        private readonly int _defaultRoleId = Roles.LicensingProviderId;

        public RegisterPracticeCommandHandler(
            IPracticeService practicesService,
            ITransactionManager transactionManager,
            IPaymentPlansService paymentPlansService,
            ILeadSourcesService leadSourcesService,
            ILocationsService locationsService,
            IAddOnsService addOnsService,
            IRolesService rolesService,
            IEmployeeService employeeService,
            ISchedulerAccountService schedulerAccountService,
            IMediator mediator,
            ILogger<RegisterPracticeCommandHandler> logger)
        {
            _practicesService = practicesService;
            _transactionManager = transactionManager;
            _paymentPlansService = paymentPlansService;
            _leadSourcesService = leadSourcesService;
            _locationsService = locationsService;
            _addOnsService = addOnsService;
            _rolesService = rolesService;
            _employeeService = employeeService;
            _schedulerAccountService = schedulerAccountService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Practice> Handle(RegisterPracticeCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Registration practice with name: {command.PracticeName} has been started.");

            Practice? practice = null;
            PaymentPlan[]? paymentPlans;
            Location? location = null;
            Employee? employee = null;
            
            await _transactionManager.Run(async () =>
            {
                practice = await CreatePracticeAsync(command.PracticeId, command.BusinessId, command.PracticeName);

                location = await CreateLocationAsync(
                    practice: practice,
                    originId: command.LocationId,
                    name: command.LocationName,
                    address: command.Address);

                paymentPlans = await CreatePaymentPlansAsync(practice, command.DataTemplates);
                
                await CreateAddOnsAsync(practice, paymentPlans, command.DataTemplates);

                var user = await CreateUser(command, practice);

                var schedulerRegistrationResult = await RegisterInSchedulerServiceAsync(user, location);

                employee = await CreateEmployee(
                    user: user, 
                    location: location, 
                    roleId: _defaultRoleId, 
                    credentials: command.ProviderCredentials,
                    schedulerAccountId: schedulerRegistrationResult.Id
                );

                await CreateDefaultLeadSourcesAsync(practice);

                await SendSchedulerInvitationEmail(user, schedulerRegistrationResult.Password, cancellationToken);
            });

            var defaultPatientCommand = new CreateDefaultPatientCommand(
                practice: practice!,
                location: location!,
                employee: employee!,
                dataTemplates: command.DataTemplates
            );
            
            await _mediator.Send(defaultPatientCommand, cancellationToken);

            return practice!;
        }

        private async Task<Practice> CreatePracticeAsync(int originId, int businessId, string name)
        {
            var practice = new Practice
            {
                Id = originId,
                BusinessId = businessId,
                Name = name,
                IsActive = false,
            };

            return await _practicesService.CreateAsync(practice);
        }

        private async Task CreateDefaultLeadSourcesAsync(Practice practice)
        {
            await _leadSourcesService.CreateAsync(
                name: "Other",
                isOther: true, 
                practice.GetId());
        }

        private async Task<Location> CreateLocationAsync(
            Practice practice, 
            int originId,
            string name,
            AddressModel address)
        {
            var location = new Location(practice)
            {
                Name = name,
                Description = string.Empty,
                Country = address.Country,
                City = address.City,
                State = address.State,
                ZipCode = address.ZipCode,
                StreetAddress1 = address.StreetAddress1,
                StreetAddress2 = address.StreetAddress2,
                Type = LocationType.HeadOffice
            };
            
            location.SetOriginId(originId);
            
            await _locationsService.CreateAsync(location);

            return location;
        }

        private async Task<SchedulerRegistrationResultModel> RegisterInSchedulerServiceAsync(User user, Location location)
        {
            try
            {
                var registerModel = new RegisterSchedulerAccountModel
                {
                    PracticeId = user.PracticeId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    TimeOffset = location.Offset
                };

                return await _schedulerAccountService.RegisterAccountAsync(registerModel);
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Scheduler registration for user with [Id] = {user.GetId()}, location with [Id] = {location.GetId()} has failed with [Error]: {e.ToString()}");
                //TODO: clarify requirements, this is temp solution
                return new SchedulerRegistrationResultModel
                {
                    Id = null,
                    Password = null
                };
            }
        }

        private async Task SendSchedulerInvitationEmail(User user, string schedulerPassword, CancellationToken cancellationToken)
        {
            if (schedulerPassword is null)
            {
                return;
            }

            var schedulerAccountCreatedEvent = new SchedulerAccountCreatedEvent(
                practiceId: user.PracticeId,
                firstName: user.FirstName,
                email: user.Email,
                schedulerPassword: schedulerPassword);

            await _mediator.Publish(schedulerAccountCreatedEvent, cancellationToken);
        }

        private async Task<User> CreateUser(RegisterPracticeCommand command, Practice practice)
        {
            var createUserCommand = new CreateUserCommand(
                firstName: command.ProviderFirstName,
                lastName: command.ProviderLastName,
                email: command.ProviderEmail,
                phoneNumber: command.ProviderPhoneNumber,
                password: command.ProviderPassword,
                birthDate: null,
                gender: Gender.None,
                userType: UserType.Employee,
                practiceId: practice.GetId(),
                billingAddress: new AddressModel(),
                shippingAddress: new AddressModel(),
                isVerified: true,
                isRegistrationCompleted: true);

            return await _mediator.Send(createUserCommand);
        }

        private async Task<Employee> CreateEmployee(User user, Location location, int roleId, string credentials, string schedulerAccountId)
        {
            var employee = new Employee(user.GetId(), Roles.LicensingProviderId);

            var employeeDomain = EmployeeDomain.Create(employee);

            employeeDomain.UpdateCredentials(credentials);
            
            var role = await _rolesService.GetByIdAsync(roleId);

            employeeDomain.UpdatePermissions(role.Permissions.Select(x => x.Permission));

            employeeDomain.UpdateLocations(new[] { location });

            employeeDomain.UpdateSchedulerAccountId(schedulerAccountId);

            return await _employeeService.CreateAsync(employee);
        }

        private async Task<PaymentPlan[]> CreatePaymentPlansAsync(Practice practice, IDictionary<string, string> dataTemplates)
        {
            if (!dataTemplates.ContainsKey(nameof(PaymentPlan)))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Can't create a practice without payment plans");
            }

            var data = dataTemplates[nameof(PaymentPlan)];

            var paymentPlans = JsonConvert.DeserializeObject<PaymentPlan[]>(data)!;

            foreach(var paymentPlan in paymentPlans)
            {
                paymentPlan.PracticeId = practice.GetId();

                await _paymentPlansService.CreatePaymentPlanAsync(paymentPlan);
            }

            return paymentPlans;
        }

        private async Task CreateAddOnsAsync(Practice practice, PaymentPlan[] paymentPlans, IDictionary<string, string> dataTemplates)
        {
            if (!dataTemplates.ContainsKey(nameof(AddOn)))
            {
                return;
            }

            var data = dataTemplates[nameof(AddOn)];

            var addOns = JsonConvert.DeserializeObject<AddOn[]>(data)!;

            foreach(var addOn in addOns)
            {
                addOn.PracticeId = practice.GetId();

                addOn.PaymentPlanAddOns = paymentPlans.Select(paymentPlan =>  new PaymentPlanAddOn
                    {
                        PaymentPlanId = paymentPlan.GetId()
                    }).ToList();

                await _addOnsService.CreateAddOnAsync(addOn);
            }
        }
    }
}