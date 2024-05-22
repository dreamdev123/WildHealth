using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Enums;
using WildHealth.Application.Commands.Patients;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Shared.Exceptions;
using WildHealth.Integration.Models.Patients;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Insurances
{
    public class StartPatientInsuranceRegistrationCommandHandler : IRequestHandler<StartPatientInsuranceRegistrationCommand, Patient>
    {
        private readonly IUsersService _usersService;
        private readonly IIntegrationsService _integrationsService;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly ITransactionManager _transactionManager;
        private readonly ILocationsService _locationsService;
        private readonly IPatientsService _patientsService;
        private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public StartPatientInsuranceRegistrationCommandHandler(
            IUsersService usersService, 
            IIntegrationsService integrationsService, 
            IIntegrationServiceFactory integrationServiceFactory, 
            ITransactionManager transactionManager, 
            ILocationsService locationsService, 
            IPatientsService patientsService, 
            IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory, 
            IMediator mediator, 
            ILogger<StartPatientInsuranceRegistrationCommandHandler> logger)
        {
            _usersService = usersService;
            _integrationsService = integrationsService;
            _integrationServiceFactory = integrationServiceFactory;
            _transactionManager = transactionManager;
            _locationsService = locationsService;
            _patientsService = patientsService;
            _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Patient> Handle(StartPatientInsuranceRegistrationCommand command, CancellationToken cancellationToken)
        {
            var specification = UserSpecifications.UserWithIntegrations;
            
            var user = await _usersService.GetAsync(command.UserId, specification);
            
            _logger.LogInformation($"Registration for patient with [Email] = {user.Email} started.");

            Patient patient;
            
            var patientCreatedModels = Enumerable.Empty<PatientCreatedModel>();

            var integrationService = await _integrationServiceFactory.CreateAsync(user.PracticeId);

            var transaction = _transactionManager.BeginTransaction();
            
            try
            {
                user = await UpdateUserAsync(user, command);

                patient = await GetOrCreatePatientAsync(user, command.PracticeId);

                patientCreatedModels = await _mediator.Send(new CreatePaymentIntegrationAccountCommand(
                    patientId: patient.GetId()
                ), cancellationToken);

                if (user.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Customer) is null)
                {
                    await CreatePatientInOpenPmAsync(command, user, patient);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"Registration for patient with [Email] = {user.Email} failed. {e}");

                await transaction.RollbackAsync(cancellationToken);
                
                await integrationService.TryDeletePatientAsync(patientCreatedModels);

                throw;
            }

            _logger.LogInformation($"Registration for patient with [Email] = {user.Email} finished.");

            return patient;
        }
        
        #region private
        
        /// <summary>
        /// Updates user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        private async Task<User> UpdateUserAsync(User user, StartPatientInsuranceRegistrationCommand command)
        {
            var createUserCommand = new UpdateUserCommand(
                id: user.GetId(),
                email: user.Email,
                firstName: command.FirstName,
                lastName: command.LastName,
                phoneNumber: command.PhoneNumber,
                birthday: command.Birthday,
                gender: command.Gender,
                userType: UserType.Patient,
                billingAddress: new AddressModel(),
                shippingAddress: new AddressModel(),
                isRegistrationCompleted: true
            );

           return await _mediator.Send(createUserCommand);
        }

        /// <summary>
        /// Returns existing patient or creates new
        /// </summary>
        /// <param name="user"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        private async Task<Patient> GetOrCreatePatientAsync(User user, int practiceId)
        {
            try
            {
                var patient = await _patientsService.GetByUserIdAsync(user.GetId());

                AssertInsuranceRegistrationAvailable(patient);

                if (user.PracticeId == practiceId)
                {
                    return patient;
                }
                
                var changePracticeCommand = new ChangePatientsPracticeCommand(
                    patientId: patient.GetId(),
                    newPracticeId: practiceId);

                return await _mediator.Send(changePracticeCommand);
            }
            catch (AppException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                var defaultLocation = await _locationsService.GetDefaultLocationAsync(user.PracticeId);
            
                var patient = new Patient(
                    user: user,
                    options: new PatientOptions(),
                    location: defaultLocation
                );

                return await _patientsService.CreatePatientAsync(patient);
            }
        }

        /// <summary>
        /// Assert insurance registration available
        /// </summary>
        /// <param name="patient"></param>
        private void AssertInsuranceRegistrationAvailable(Patient patient)
        {
            if (patient.CurrentSubscription?.GetStatus() == SubscriptionStatus.Active)
            {
                throw new AppException(HttpStatusCode.BadRequest, "You already have an active subscription.");
            }
        }

        /// <summary>
        /// Creates patient in Open PM and stores integration
        /// </summary>
        /// <param name="command"></param>
        /// <param name="user"></param>
        /// <param name="patient"></param>
        private async Task CreatePatientInOpenPmAsync(StartPatientInsuranceRegistrationCommand command, User user, Patient patient)
        {
            var location = await _locationsService.GetDefaultLocationAsync(user.PracticeId);
            var fhirLocationId = location.GetIntegrationId(IntegrationVendor.OpenPm);
            var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(user.PracticeId);
            
            var patientId = await pmService.CreatePatientAsync(
                firstName: command.FirstName,
                lastName: command.LastName,
                birthday: command.Birthday,
                gender: command.Gender,
                phoneNumber: user.PhoneNumber,
                email: user.Email ?? string.Empty,
                streetAddress1: user.BillingAddress?.StreetAddress1 ?? string.Empty,
                streetAddress2: user.BillingAddress?.StreetAddress2 ?? string.Empty,
                city: user.BillingAddress?.City ?? string.Empty,
                state: user.BillingAddress?.State ?? string.Empty,
                zipCode: user.BillingAddress?.ZipCode ?? string.Empty,
                fhirLocationId: fhirLocationId,
                practiceId: user.PracticeId
            );

            var patientIntegration = new UserIntegration(
                vendor: IntegrationVendor.OpenPm,
                purpose: IntegrationPurposes.User.Customer,
                value: patientId,
                user: user
            );

            await _integrationsService.CreateAsync(patientIntegration);
        }
        
        #endregion
    }
}