using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Notes;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Notes;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.CommandHandlers.Notes.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Notes;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Common.Constants;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Notes
{
    public class CreateNoteCommandHandler : IRequestHandler<CreateNoteCommand, Note>
    {
        private readonly IPatientsService _patientsService;
        private readonly IEmployeeService _employeeService;
        private readonly IAppointmentsService _appointmentsService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly INoteService _notesService;
        private readonly IAuthTicket _authTicket;
        private readonly MaterializeFlow _materialize;
        
        public CreateNoteCommandHandler(
            IPatientsService patientsService,
            IEmployeeService employeeService,
            IAppointmentsService appointmentsService,
            IPermissionsGuard permissionsGuard,
            INoteService notesService,
            IAuthTicket authTicket,
            MaterializeFlow materialize)
        {
            _patientsService = patientsService;
            _employeeService = employeeService;
            _appointmentsService = appointmentsService;
            _permissionsGuard = permissionsGuard;
            _notesService = notesService;
            _authTicket = authTicket;
            _materialize = materialize;
        }

        public async Task<Note> Handle(CreateNoteCommand command, CancellationToken cancellationToken)
        {
            var patient = await GetPatientAsync(command.PatientId);

            _permissionsGuard.AssertPermissions(patient);

            var employee = await GetEmployeeAsync(command.EmployeeId);

            var version = await _notesService.GetNextVersionNumber(command.OriginalNoteId);
                
            var appointment = await GetAppointmentAsync(command.AppointmentId);

            var delegatedEmployee = await GetDelegatedEmployeeAsync(command.Type, employee, patient, appointment);

            var flow = new CreateNoteFlow(
                Name: command.Name,
                Version: version,
                Title: command.Title,
                Type: command.Type,
                Content: command.Content,
                InternalContent: command.InternalContent,
                VisitDate: command.VisitDate,
                OriginalNoteId: command.OriginalNoteId,
                Patient: patient,
                Employee: employee,
                DelegatedEmployee: delegatedEmployee,
                Appointment: appointment,
                Logs: command.Logs
            );

            var note = await flow.Materialize(_materialize).Select<Note>();
            
            if (command.IsCompleted)
            {
                var completedBy = _authTicket.GetId();
                var completedByEmployee = await _employeeService.GetByUserIdAsync(completedBy);
                await new CompleteNoteFlow(note, appointment, completedByEmployee, DateTime.UtcNow).Materialize(_materialize);
            }
            
            return note;
        }

        #region private

        private async Task<Patient> GetPatientAsync(int patientId)
        {
            var specification = PatientSpecifications.PatientWithSubscription;
            var patient = await _patientsService.GetByIdAsync(patientId, specification);
            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Patient not found", exceptionParam);
            }

            return patient;
        }

        private async Task<Employee?> GetDelegatedEmployeeAsync(NoteType type, Employee employee, Patient patient, Appointment? appointment)
        {
            if (!_permissionsGuard.IsAssistRole(employee.RoleId))
            {
                return null;
            }

            if (NoteConstants.NoteTypesCanBeOwnedByAssistRole.Contains(type))
            {
                return null;
            }
            
            var assignedEmployees = await _employeeService.GetAssignedToAsync(patient.GetId());

            if (appointment is not null)
            {
                var host = AppointmentDomain.Create(appointment).GetMeetingOwner();

                if (host is not null)
                {
                    return host;
                }
            }

            if (NoteConstants.NoteTypeToEmployeeTypeMap.ContainsKey(type))
            {
                return assignedEmployees.FirstOrDefault(x => x.Type == NoteConstants.NoteTypeToEmployeeTypeMap[type]);
            }
            
            return assignedEmployees.FirstOrDefault();
        }
        
        private async Task<Employee> GetEmployeeAsync(int employeeId)
        {
            var employee = await _employeeService.GetByIdAsync(employeeId);
            if (employee is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(employeeId), employeeId);
                throw new AppException(HttpStatusCode.NotFound, "Employee not found", exceptionParam);
            }

            return employee;
        }
        
        /// <summary>
        /// Asserts note for particular endpoint does not exist
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <exception cref="AppException"></exception>
        private async Task AssertNoteWithSameAppointmentDoesNotExist(int appointmentId)
        {
            try
            {
                var existingNote = await _notesService.GetByAppointmentIdAsync(appointmentId);

                if (existingNote is not null)
                {
                    throw new AppException(HttpStatusCode.BadRequest, "Note for this appointment already exists");
                }
            }
            catch (AppException ex) when(ex.StatusCode == HttpStatusCode.NotFound)
            {
                // ignore
            }
        }
        
        /// <summary>
        /// Returns appointment by id
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <returns></returns>
        private async Task<Appointment?> GetAppointmentAsync(int? appointmentId)
        {
            if (!appointmentId.HasValue)
            {
                return null;
            }

            
            var appointment = await _appointmentsService.GetByIdAsync(appointmentId.Value);

            await AssertNoteWithSameAppointmentDoesNotExist(appointmentId.Value);

            return appointment;
        }

        #endregion
    }
}
