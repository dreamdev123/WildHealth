using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Conversations;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Enums.Employees;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Shared.Exceptions;
using System.Collections.Generic;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Conversations
{
    public class StartHealthConversationCommandHandler : IRequestHandler<StartHealthCareConversationCommand, Conversation?>
    {
        private static readonly EmployeeType[] SupportedTypes =
        {
            EmployeeType.Coach,
            EmployeeType.Provider
        };
        
        private readonly IPatientsService _patientsService;
        private readonly IConversationsService _conversationsService;
        private readonly IConversationsSettingsService _conversationsSettingsService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IEmployeeService _employeesService;
        private readonly IMediator _mediator;

        public StartHealthConversationCommandHandler(
            IConversationsService conversationsService,
            IConversationsSettingsService conversationsSettingsService,
            IPermissionsGuard permissionsGuard,
            IPatientsService patientsService,
            IEmployeeService employeesService,
            IMediator mediator)
        {
            _patientsService = patientsService;
            _conversationsService = conversationsService;
            _conversationsSettingsService = conversationsSettingsService;
            _permissionsGuard = permissionsGuard;
            _employeesService = employeesService;
            _mediator = mediator;
        }

        public async Task<Conversation?> Handle(StartHealthCareConversationCommand command, CancellationToken cancellationToken)
        {
            var spec = PatientSpecifications.StartConversationWithPatientSpecification;

            var patient = await _patientsService.GetByIdAsync(command.PatientId, spec);

            _permissionsGuard.AssertPermissions(patient);

            var conversation = await TryGetHealthCareConversationAsync(command.PatientId);
            
            if (conversation is null)
            {
                return await CreateHealthCareConversationAsync(command);
            }

            if (!IsEmployeeMissing(conversation, command.EmployeeId))
            {
                return conversation;
            }

            var addParticipantCommand = new AddEmployeeParticipantToConversationCommand(
                conversationId: conversation.GetId(),
                employeeId: command.EmployeeId,
                isActive: true
            );

            return await _mediator.Send(addParticipantCommand, cancellationToken);
        }

        #region private

        /// <summary>
        /// Creates health care conversation
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private async Task<Conversation?> CreateHealthCareConversationAsync(StartHealthCareConversationCommand command)
        {
            var allAssignedEmployees = await _employeesService.GetAssignedToAsync(command.PatientId);

            var assignedEmployees = allAssignedEmployees
                .Where(x => SupportedTypes.Contains(x.Type))
                .ToArray();
            
            var targetEmployee = await TryGetEmployeeAsync(command.EmployeeId);

            var activeParticipants = assignedEmployees
                .Where(i => i.Type == EmployeeType.Coach)
                .ToArray();

            var inactiveParticipants = assignedEmployees
                .Where(i => i.Type == EmployeeType.Provider)
                .ToList();

            if (!activeParticipants.Any() && assignedEmployees.Any())
            {
                var participant = assignedEmployees.First();
                activeParticipants = new[] { participant };
                inactiveParticipants.Remove(participant);
            }

            if (IsEmployeeSelected(command.EmployeeId))
            {
                if (!IsContainsEmployee(activeParticipants, command.EmployeeId) && targetEmployee is not null)
                {
                    activeParticipants = activeParticipants.Concat(new[] { targetEmployee }).ToArray();
                }

                if (IsContainsEmployee(inactiveParticipants, command.EmployeeId))
                {
                    inactiveParticipants = inactiveParticipants.Where(x => x.GetId() != command.EmployeeId).ToList();
                }
            }
            if (!activeParticipants.Any())
            {
                return null;
            }

            var delegatedEmployees = await GetDelegatedEmployeesAsync(activeParticipants);
            
            var createConversationCommand = new CreatePatientHealthCareConversationCommand(
                practiceId: command.PracticeId,
                locationId: command.LocationId,
                patientId: command.PatientId,
                activeEmployees: activeParticipants,
                inactiveEmployees: inactiveParticipants.ToArray(),
                delegatedEmployees: delegatedEmployees.ToArray()
            );

            return await _mediator.Send(createConversationCommand);
        }

        /// <summary>
        /// Returns delegated employees
        /// </summary>
        /// <param name="employees"></param>
        /// <returns></returns>
        private async Task<(Employee employee, Employee delegatedBy)[]> GetDelegatedEmployeesAsync(Employee[] employees)
        {
            var result = new List<(Employee employee, Employee delegatedBy)>();
                
            var employeeIds = employees.Select(x => x.GetId()).ToArray();
            
            var settings = await _conversationsSettingsService.GetByEmployeeIdsAsync(employeeIds);

            var delegatedEmployeeIds = settings
                .Where(x => x.ForwardEmployeeEnabled)
                .Select(x => x.ForwardEmployeeId)
                .ToArray();

            if (delegatedEmployeeIds.Length == 0)
            {
                return result.ToArray();
            }

            var delegatedEmployees = await _employeesService.GetByIdsAsync(delegatedEmployeeIds, EmployeeSpecifications.ActiveWithUser);

            foreach (var delegatedEmployee in delegatedEmployees)
            {
                var delegatedById = settings.First(x => x.ForwardEmployeeId == delegatedEmployee.GetId()).EmployeeId;
                
                var delegatedBy = employees.First(x => x.Id == delegatedById);
                
                result.Add((delegatedEmployee, delegatedBy));
            }

            return result.ToArray();
        }

        /// <summary>
        /// Returns employee or null if it does not exist
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        private async Task<Employee?> TryGetEmployeeAsync(int employeeId)
        {
            try
            {
                return await _employeesService.GetByIdAsync(employeeId);
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Returns health care conversation or null if it does not exist.
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        private async Task<Conversation?> TryGetHealthCareConversationAsync(int patientId)
        {
            try
            {
                return await _conversationsService.GetHealthConversationByPatientAsync(patientId);
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if employees collection contains particular employee
        /// </summary>
        /// <param name="employees"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        private bool IsContainsEmployee(IEnumerable<Employee> employees, int employeeId)
        {
            return employees.Any(x => x.GetId() == employeeId);
        }

        /// <summary>
        /// Checks if employee is missing
        /// </summary>
        /// <param name="conversation"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        private bool IsEmployeeMissing(Conversation conversation, int employeeId)
        {
            var isEmployeeInConversation = conversation
                .EmployeeParticipants
                .Any(x => x.EmployeeId == employeeId && x.IsActive && !x.DeletedAt.HasValue);

            return IsEmployeeSelected(employeeId) && !isEmployeeInConversation;
        }

        /// <summary>
        /// Checks if employee is selected
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        private bool IsEmployeeSelected(int employeeId) => employeeId > 0;

        #endregion
    }
}
