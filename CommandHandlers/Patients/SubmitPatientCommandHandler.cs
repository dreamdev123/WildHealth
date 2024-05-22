using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Application.Services.Locations;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;
using WildHealth.Application.Durable.Mediator;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class SubmitPatientCommandHandler : IRequestHandler<SubmitPatientCommand, Patient>
    {
        private readonly IPatientsService _patientsService;
        private readonly ILocationsService _locationsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IMediator _mediator;
        private readonly IDurableMediator _durableMediator;
        private readonly ILogger _logger;

        public SubmitPatientCommandHandler(
            IPatientsService patientsService,
            ILocationsService locationsService,
            ITransactionManager transactionManager,
            IPermissionsGuard permissionsGuard,
            IMediator mediator, 
            ILogger<SubmitPatientCommandHandler> logger,
            IDurableMediator durableMediator)
        {
            _patientsService = patientsService;
            _locationsService = locationsService;
            _transactionManager = transactionManager;
            _permissionsGuard = permissionsGuard;
            _mediator = mediator;
            _logger = logger;
            _durableMediator = durableMediator;
        }

        public async Task<Patient> Handle(SubmitPatientCommand command, CancellationToken cancellationToken)
        {
            var spec = PatientSpecifications.SubmitPatientSpecification;

            var patient = await _patientsService.GetByIdAsync(command.Id, spec);

            _permissionsGuard.AssertPermissions(patient);

            var updateUserCommand = new UpdateUserCommand(
                id: patient.UserId,
                firstName: command.FirstName,
                lastName: command.LastName,
                birthday: command.Birthday,
                gender: command.Gender,
                email: command.Email,
                phoneNumber: command.PhoneNumber,
                billingAddress: command.BillingAddress,
                shippingAddress: command.ShippingAddress,
                userType: null
            );

            int[] newlyAssignedEmployeeIds;

            var isLocationChanged = false;

            Location? previousLocation = null;

            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                if (patient.LocationId != command.LocationId)
                {
                    var location = await _locationsService.GetByIdAsync(command.LocationId, patient.User.PracticeId);

                    _permissionsGuard.AssertPermissions(location);

                    previousLocation = patient.Location;
                    
                    patient.ChangeLocation(location);

                    isLocationChanged = true;
                }

                await _patientsService.UpdatePatientOptionsAsync(patient, command.Options);
                
                await _patientsService.UpdatePatientOnBoardingStatusAsync(patient, PatientOnBoardingStatus.Completed);

                await _mediator.Send(updateUserCommand, cancellationToken);

                newlyAssignedEmployeeIds = (await _patientsService.AssignToEmployeesAsync(patient, command.EmployeeIds)).ToArray();

                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation($"Patient with [Id] = {patient.Id} was submitted.");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Submit of Patient with [Id] = {patient.Id} was failed. {ex}");
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            await _durableMediator.Publish(new PatientUpdatedEvent(patient.GetId(), newlyAssignedEmployeeIds));

            if (isLocationChanged)
            {
                await _durableMediator.Publish(new PatientTransferredToLocationEvent(
                    PatientId: patient.GetId(), 
                    NewLocationId: patient.Location.GetId(),
                    OldLocationId: previousLocation!.GetId()));
            }
            
            return patient;
        }
    }
}