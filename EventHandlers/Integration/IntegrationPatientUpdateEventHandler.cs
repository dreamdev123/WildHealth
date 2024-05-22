using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Integration.Events;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.Users;
using WildHealth.Application.Commands.Users;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Employees;
using WildHealth.Integration.Models.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Entities.Practices;
using MediatR;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Address;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.EventHandlers.Integration
{
    public class IntegrationPatientUpdateEventHandler : INotificationHandler<IntegrationPatientUpdateEvent>
    {
        private readonly IPatientsService _patientsService;
        private readonly IEmployeeService _employeeService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public IntegrationPatientUpdateEventHandler(
            IPatientsService patientsService,
            IEmployeeService employeeService,
            IMediator mediator,
            ILogger<IntegrationPatientUpdateEventHandler> logger)
        {
            _patientsService = patientsService;
            _employeeService = employeeService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(IntegrationPatientUpdateEvent notification, CancellationToken cancellationToken)
        {
            var originPatient = notification.Patient;
            try
            {
                _logger.LogInformation($"Attempting to update patient from integration system with [IntegrationId]: {originPatient.Id}");

                var patient = await _patientsService.GetByIntegrationIdAsync(originPatient.Id, notification.Vendor);
                var patientHash = patient.GetHashCode();
                var originPatientHash = GetOriginPatientHash(originPatient, patient);

                if (patientHash != originPatientHash)
                {
                    var updateUserCommand = new UpdateUserCommand(
                        id: patient.User.GetId(),
                        firstName: originPatient.FirstName,
                        lastName: originPatient.LastName,
                        birthday: originPatient.Birthday,
                        gender: originPatient.Sex,
                        email: originPatient.Email,
                        phoneNumber: originPatient.Phone,
                        billingAddress: new AddressModel
                        {
                            City = originPatient.City,
                            Country = originPatient.Country,
                            State = originPatient.State,
                            StreetAddress1 = originPatient.Address1,
                            StreetAddress2 = originPatient.Address2,
                            ZipCode = originPatient.ZipCode
                        },
                        shippingAddress: new AddressModel
                        {
                            City = patient.User.ShippingAddress.City,
                            Country = patient.User.ShippingAddress.Country,
                            State = patient.User.ShippingAddress.State,
                            StreetAddress1 = patient.User.ShippingAddress.StreetAddress1,
                            StreetAddress2 = patient.User.ShippingAddress.StreetAddress2,
                            ZipCode = patient.User.ShippingAddress.ZipCode
                        },
                        userType: null
                    );

                    await _mediator.Send(updateUserCommand, cancellationToken);
                }

                var employee = await _employeeService.GetByIntegrationIdAsync(
                    integrationId: originPatient.PractitionerId, 
                    vendor: notification.Vendor,
                    purpose: IntegrationPurposes.Employee.ProviderId,
                    practiceId: patient.User.PracticeId,
                    locationId: patient.LocationId);

                await _patientsService.UpdateProviderAssignmentAsync(patient, employee);
            }
            catch (AppException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation($"Patient with [IntegrationId]: {originPatient.Id} not found");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Update patient from integration system with [IntegrationId]: {originPatient.Id} was failed. {ex}");
                throw;
            }
        }

        #region private 

        private int GetOriginPatientHash(PatientIntegrationModel originPatient, Patient patient)
        {
            var user = new User(new Practice { Id = 1})
            {
                FirstName = originPatient.FirstName,
                LastName = originPatient.LastName,
                Birthday = originPatient.Birthday,
                Gender = originPatient.Sex,
                Email = originPatient.Email,
                PhoneNumber = originPatient.Phone,
                BillingAddress = new Address
                {
                    City = originPatient.City,
                    Country = originPatient.Country,
                    State = originPatient.State,
                    StreetAddress1 = originPatient.Address1,
                    StreetAddress2 = originPatient.Address2,
                    ZipCode = originPatient.ZipCode
                },
                ShippingAddress = new Address
                {
                    City = patient.User.ShippingAddress.City,
                    Country = patient.User.ShippingAddress.Country,
                    State = patient.User.ShippingAddress.State,
                    StreetAddress1 = patient.User.ShippingAddress.StreetAddress1,
                    StreetAddress2 = patient.User.ShippingAddress.StreetAddress2,
                    ZipCode = patient.User.ShippingAddress.ZipCode
                }
            };

            return user.GetHashCode();
        }

        #endregion
    }
}