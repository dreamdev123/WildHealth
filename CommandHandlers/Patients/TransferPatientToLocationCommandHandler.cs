using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Application.Services.Locations;
using WildHealth.Shared.Data.Repository;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Application.Services.Employees;
using WildHealth.Domain.Entities.Locations;
using System.Collections.Generic;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Application.Durable.Mediator;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class TransferPatientToLocationCommandHandler : IRequestHandler<TransferPatientToLocationCommand, Patient>
    {
        private readonly IGeneralRepository<PatientEmployee> _patientEmployeesRepository;
        private readonly IPatientsService _patientsService;
        private readonly IEmployeeService _employeesService;
        private readonly ILocationsService _locationsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IDurableMediator _durableMediator;

        public TransferPatientToLocationCommandHandler(
            IGeneralRepository<PatientEmployee> patientEmployeesRepository,
            IPatientsService patientsService,
            IEmployeeService employeesService,
            ILocationsService locationsService,
            ITransactionManager transactionManager,
            IPermissionsGuard permissionsGuard,
            IDurableMediator durableMediator)
        {
            _patientEmployeesRepository = patientEmployeesRepository;
            _patientsService = patientsService;
            _employeesService = employeesService;
            _locationsService = locationsService;
            _transactionManager = transactionManager;
            _permissionsGuard = permissionsGuard;
            _durableMediator = durableMediator;
        }

        public async Task<Patient> Handle(TransferPatientToLocationCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(command.PatientId);

            _permissionsGuard.AssertPermissions(patient);

            if (patient.LocationId == command.LocationId)
            {
                return patient;
            }

            Location previousLocation;
            
            var location = await _locationsService.GetByIdAsync(command.LocationId, patient.User.PracticeId);

            _permissionsGuard.AssertPermissions(location);

            var assignedEmployeeIds = patient.GetAssignedEmployeesIds();

            var employees = await _employeesService.GetActiveAsync(
                ids: assignedEmployeeIds, 
                practiceId: patient.User.PracticeId,
                locationId: patient.LocationId);

            var employeesToUnAssign = GetEmployeesToUnAssign(employees, location);

            await using var transaction = _transactionManager.BeginTransaction();

            try
            {
                previousLocation = patient.Location;
                
                patient.ChangeLocation(location);

                UnAssignEmployees(patient, employeesToUnAssign);

                await _patientsService.UpdateAsync(patient);

                if (!patient.GetAssignedEmployees().Any())
                {
                    await _patientsService.UpdatePatientOnBoardingStatusAsync(patient, PatientOnBoardingStatus.New);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }

            await _durableMediator.Publish(new PatientUpdatedEvent(patient.GetId(), Enumerable.Empty<int>()));
            await _durableMediator.Publish(new PatientTransferredToLocationEvent(
                PatientId: patient.GetId(), 
                NewLocationId: patient.Location.GetId(),
                OldLocationId: previousLocation.GetId()));

            return patient;
        }

        #region private

        private Employee[] GetEmployeesToUnAssign(IEnumerable<Employee> employees, Location location)
        {
            return employees.Where(x => x.Locations.All(t => t.LocationId != location.GetId())).ToArray();
        }

        private void UnAssignEmployees(Patient patient, Employee[] employeesToUnAssign)
        {
            var assignments = patient.Employees.ToArray();

            foreach (var assignment in assignments)
            {
                if (employeesToUnAssign.Any(x => x.GetId() == assignment.EmployeeId))
                {
                    _patientEmployeesRepository.Delete(assignment);

                    patient.Employees.Remove(assignment);
                }
            }
        }

        #endregion
    }
}