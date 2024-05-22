using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Schedulers.Availability;
using WildHealth.Application.Services.Schedulers.Bookings;
using WildHealth.Application.Services.Schedulers.Meetings;
using WildHealth.Application.Utils.Timezones;
using WildHealth.Common.Models.Scheduler;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Enums;
using WildHealth.Zoom.Clients.Models.Meetings;
using MediatR;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Domain.Constants;
using WildHealth.Zoom.Clients.Constants;

namespace WildHealth.Application.CommandHandlers.Appointments
{
    public class CreateAllDayAppointmentCommandHandler : IRequestHandler<CreateAllDayAppointmentCommand, Appointment[]>
    {
        private readonly ISchedulerAvailabilityService _schedulerAvailabilityService;
        private readonly ISchedulerMeetingsService _schedulerMeetingsService;
        private readonly ISchedulerBookingsService _schedulerBookingsService;
        private readonly IAppointmentsService _appointmentsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IEmployeeService _employeeService;
        private readonly IDurableMediator _durableMediator;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CreateAllDayAppointmentCommandHandler(
            ISchedulerAvailabilityService schedulerAvailabilityService,
            ILogger<CreateAllDayAppointmentCommandHandler> logger,
            ISchedulerMeetingsService schedulerMeetingsService,
            ISchedulerBookingsService schedulerBookingsService,
            IAppointmentsService appointmentsService,
            ITransactionManager transactionManager,
            IEmployeeService employeeService,
            IDurableMediator durableMediator,
            IMediator mediator)
        {
            _schedulerAvailabilityService = schedulerAvailabilityService;
            _schedulerBookingsService = schedulerBookingsService;
            _schedulerMeetingsService = schedulerMeetingsService;
            _appointmentsService = appointmentsService;
            _transactionManager = transactionManager;
            _employeeService = employeeService;
            _durableMediator = durableMediator;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Appointment[]> Handle(CreateAllDayAppointmentCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Create all day appointment for [EmployeeIds] = {string.Join(',', command.EmployeeIds.ToArray())} has been started.", command);

            var employees = await _employeeService.GetByIdsAsync(command.EmployeeIds, EmployeeSpecifications.ActiveWithUser);

            var createdAppointments = new List<Appointment>();
            
            var intervals = await GetIntervals(command.PracticeId, command.EmployeeIds, command.Date);
            
            await _transactionManager.Run(async () =>
                {
                    foreach (var (start, end) in intervals)
                    {
                        var appointment = new Appointment(
                            patientId: null,
                            locationId: command.LocationId,
                            locationType: AppointmentLocationType.Online,
                            startDate: start.ToUniversalTime(),
                            endDate: end.ToUniversalTime(),
                            withType: AppointmentWithType.Other,
                            configurationId: null,
                            type: null
                        )
                        {
                            Purpose = AppointmentPurpose.Other,
                            Name = command.Name,
                            Comment = command.Comment,
                            TimeZoneId = string.IsNullOrEmpty(command.TimeZoneId)
                                ? TimeZoneInfo.Utc.Id
                                : TimezoneHelper.GetWindowsId(command.TimeZoneId)
                        };
            
                        await _appointmentsService.CreateAppointmentAsync(appointment);

                        await SetEmployeesAsync(appointment, employees);
            
                        _logger.LogInformation($"Create all day appointment for [EmployeeIds] = {command.EmployeeIds} has been added into a database.");

                        await CreateEventInMeetingServiceAsync(command.PracticeId, appointment, employees);

                        await LinkAppointmentToSchedulerServiceAsync(command.PracticeId, appointment, employees);
            
                        _logger.LogInformation($"Create all day appointment for [EmployeeIds] = {command.EmployeeIds} has been finished.");
                        
                        createdAppointments.Add(appointment);
                    }
                },
                (error) =>
                {
                    _logger.LogError($"Create all day appointment for [EmployeeIds] = {command.EmployeeIds} has been failed with [Error]: {error.ToString()}");
                });

            var ids = createdAppointments.Select(x => x.GetId()).ToArray();
            var resultAppointments = await _appointmentsService.GetByIdsAsync(ids);
            
            var source = command.Source ?? ClientConstants.Source.MobileApp;

            foreach (var resultAppointment in resultAppointments)
            {
                await _durableMediator.Publish(new AppointmentCreatedEvent(
                    appointmentId: resultAppointment.GetId(),
                    createdBy: UserType.Employee,
                    isRescheduling: false,
                    source: source));
            }

            return resultAppointments.ToArray();
        }
        
        #region private

        private async Task<(DateTime, DateTime)[]> GetIntervals(int practiceId, int[] employeeIds, DateTime date)
        {
            var allAvailability = await _schedulerAvailabilityService.GetAvailabilityAsync(
                practiceId: practiceId,
                configurationId: null,
                employeeIds: employeeIds,
                from: GetFromTime(date),
                to: date.Date.AddDays(1).AddHours(5) //5h boost to avoid timezone issues.
            );

            var intervals = new List<(DateTime, DateTime)>();
            void SelectInterval(SchedulerAvailabilityModel[] availability)
            {
                foreach (var item in availability)
                {
                    var isNotLastElement = availability.All(x => x.Start != item.End);
                    if (!isNotLastElement)
                    {
                        continue;
                    }

                    intervals.Add((availability[0].Start, item.End));

                    var nextInterval = availability
                        .Where(x => x.Start > item.Start)
                        .ToArray();
                    
                    SelectInterval(nextInterval);

                    return;
                }
            }
            SelectInterval(allAvailability);
            

            return intervals.ToArray();
        }

        private DateTime GetFromTime(DateTime date)
        {
            var currentTime = DateTime.UtcNow;
            var isToday = date.Date == currentTime.Date;
            
            return isToday 
                ? new DateTime(date.Year, date.Month, date.Day, currentTime.Hour, currentTime.Minute, currentTime.Second) 
                : date;
        }

        private async Task SetEmployeesAsync(Appointment appointment, Employee[] employees)
        {
            var appointmentDomain = AppointmentDomain.Create(appointment);

            appointmentDomain.SetEmployees(employees);
            
            await _appointmentsService.EditAppointmentAsync(appointment);
        }
        
        private async Task CreateEventInMeetingServiceAsync(int practiceId, Appointment appointment, Employee[] employees)
        {
            _logger.LogInformation($"Create meeting for appointment with [Id] = {appointment.GetId()} has been started.");
            var appointmentDomain = AppointmentDomain.Create(appointment);
            
            var meetingOwner = appointmentDomain.GetMeetingOwner(employees);

            if (meetingOwner is null)
            {
                _logger.LogInformation($"Create meeting for appointment with [Id] = {appointment.GetId()}, has been skipped. Owner was not found");
                return;
            }
            
            //Trying to create meeting for specific owner. If it can not be done, create meeting for default user.
            var meeting = await TryCreateMeetingAsync(practiceId, appointment, meetingOwner.User.Email) 
                          ?? await TryCreateMeetingAsync(practiceId, appointment);

            if (meeting is null)
            {
                _logger.LogError($"Create meeting for appointment with [Id] = {appointment.GetId()}, has been failed.");
                return;
            }

            appointmentDomain.SetMeetingInformation(meeting.Id, meeting.StartUrl.ToString(), meeting.JoinUrl.ToString());

            await _appointmentsService.EditAppointmentAsync(appointment);
            
            _logger.LogInformation($"Create meeting for appointment with [Id] = {appointment.GetId()} has been finished.");
        }

        private async Task LinkAppointmentToSchedulerServiceAsync(
            int practiceId, 
            Appointment appointment, 
            IEnumerable<Employee> employees)
        {
            try
            {
                _logger.LogInformation($"Create booking for appointment with [Id] = {appointment.GetId()} has been started.");
            
                var booking = await _schedulerBookingsService.CreateBookingAsync(practiceId, appointment, employees, null);
                
                var appointmentDomain = AppointmentDomain.Create(appointment);

                appointmentDomain.SetSchedulerSystemId(booking.Id);

                await _appointmentsService.EditAppointmentAsync(appointment);
            
                _logger.LogInformation($"Create booking for appointment with [Id] = {appointment.GetId()} has been finished.");
            }
            catch (Exception e)
            {
                _logger.LogError($"Create booking for appointment with [Id] = {appointment.GetId()} has been failed with [Error]: {e.ToString()}");
                throw;
            }
        }

        /// <summary>
        /// Trying to create item in meeting service
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="appointment"></param>
        /// <param name="ownerEmail"></param>
        /// <returns></returns>
        private async Task<MeetingModel?> TryCreateMeetingAsync(
            int practiceId, 
            Appointment appointment,
            string? ownerEmail = default)
        {
            try
            {
                return await _schedulerMeetingsService.CreateMeetingAsync(
                    practiceId: practiceId, 
                    appointment: appointment,
                    autoRecording: ZoomConstants.AutoRecording.None,
                    ownerEmail: ownerEmail);
            }
            catch(Exception e)
            {
                _logger.LogWarning($"Create meeting for appointment with [Id] = {appointment.GetId()} has failed with [Error]: {e.ToString()}");
                return null;
            }
        }
        
        #endregion
    }
}