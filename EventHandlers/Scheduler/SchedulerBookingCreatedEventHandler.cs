using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Events.Scheduler;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Shared.Enums;
using WildHealth.TimeKit.Clients.Models.Customers;
using MediatR;
using WildHealth.Domain.Enums.Employees;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Domain.Constants;


namespace WildHealth.Application.EventHandlers.Scheduler
{
    public class SchedulerBookingCreatedEventHandler : INotificationHandler<SchedulerBookingCreatedEvent>
    {
        private readonly IAppointmentsService _appointmentsService;
        private readonly IPatientsService _patientsService;
        private readonly ILocationsService _locationsService;
        private readonly IEmployeeService _employeeService;
        private readonly IMediator _mediator;
        private readonly ILogger<SchedulerBookingCreatedEventHandler> _logger;
        
        public SchedulerBookingCreatedEventHandler(
            IAppointmentsService appointmentsService,
            IPatientsService patientsService,
            ILocationsService locationsService,
            IEmployeeService employeeService,
            IMediator mediator,
            ILogger<SchedulerBookingCreatedEventHandler> logger)
        {
            _appointmentsService = appointmentsService;
            _patientsService = patientsService;
            _locationsService = locationsService;
            _employeeService = employeeService;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(SchedulerBookingCreatedEvent notification, CancellationToken cancellationToken)
        {
            if (await IsAppointmentExist(notification.BookingId))
            {
                return;
            }

            var locationIds = await GetLocationIds(notification.PracticeId);

            var patient = await GetPatient(notification.PracticeId, locationIds, notification.Customers);
            if (patient is null)
            {
                return;
            }

            var employee = await GetEmployee(notification.SchedulerUserId);
            if (employee is null)
            {
                return;
            }
            
            var appointment = new Appointment(
                patientId: patient.Id,
                locationId: patient.LocationId,
                locationType: AppointmentLocationType.Online,
                startDate: notification.Start,
                endDate: notification.End,
                withType: GetWithType(employee),
                configurationId: null,
                type: null
            );

            var appointmentDomain = AppointmentDomain.Create(appointment);

            await _appointmentsService.CreateAppointmentAsync(appointment);
            
            appointmentDomain.SetSchedulerSystemId(notification.SchedulerUserId);
            await _appointmentsService.EditAppointmentAsync(appointment);

            var appointmentCreatedEvent = new AppointmentCreatedEvent(
                appointmentId: appointment.GetId(), 
                createdBy: UserType.Patient,
                isRescheduling: false,
                source: ClientConstants.Source.Clarity);
            await _mediator.Publish(appointmentCreatedEvent, cancellationToken);
        }

        #region private

        private async Task<Patient?> GetPatient(int practiceId, int[] locationIds, IEnumerable<CustomerModel> customers)
        {
            if (!customers.Any())
            {
                _logger.LogError("No any patients in booking");
                return null;
            }
            
            var customerEmail = customers.First().Email;
            var (patients, totalCount) = await _patientsService.SelectPatientsAsync(
                practiceId: practiceId,
                locationIds: locationIds,
                searchQuery: customerEmail);

            if (totalCount == 0)
            {
                _logger.LogError($"No any patients with email {customerEmail}");
                return null;
            }

            if (totalCount > 1)
            {
                _logger.LogError($"There are 2 or more patients with email {customerEmail}");
                return null;
            }

            return patients.First();
        }

        private async Task<Employee?> GetEmployee(string schedulerUserId)
        {
            var employee = await _employeeService.GetBySchedulerAccountIdAsync(schedulerUserId);

            if (employee is null)
            {
                _logger.LogError($"Employee with scheduler system id: {schedulerUserId} does not exist");
            }

            return employee;
        }

        private async Task<int[]> GetLocationIds(int practiceId)
        {
            return (await _locationsService.GetAllAsync(practiceId))
                .Select(c=> c.GetId())
                .ToArray();
        }

        private async Task<bool> IsAppointmentExist(string bookingId)
        {
            var appointment = await _appointmentsService.GetBySchedulerSystemIdAsync(bookingId);

            if (appointment is null)
            {
                _logger.LogError($"Appointment with {bookingId} is already exist");
                return true;
            }

            return false;
        }

        private AppointmentWithType GetWithType(Employee employee)
        {
            return employee.Type switch
            {
                EmployeeType.Coach => AppointmentWithType.HealthCoach,
                EmployeeType.Provider => AppointmentWithType.Provider,
                EmployeeType.Unspecified => AppointmentWithType.Other,
                _ => AppointmentWithType.Other
            };
        }

        #endregion
    }
}