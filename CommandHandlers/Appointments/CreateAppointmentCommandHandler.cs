using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Commands.Products;
using WildHealth.Application.Durable.Mediator;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Schedulers.Bookings;
using WildHealth.Application.Services.Schedulers.Meetings;
using WildHealth.Application.Utils.Timezones;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Exceptions;
using WildHealth.Zoom.Clients.Models.Meetings;
using WildHealth.Domain.Constants;
using WildHealth.Zoom.Clients.Constants;

namespace WildHealth.Application.CommandHandlers.Appointments
{
    public class CreateAppointmentCommandHandler : IRequestHandler<CreateAppointmentCommand, Appointment?>
    {
        private readonly ISchedulerMeetingsService _schedulerMeetingsService;
        private readonly ISchedulerBookingsService _schedulerBookingsService;
        private readonly IPatientProductsService _patientProductsService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IAppointmentsService _appointmentsService;
        private readonly ITransactionManager _transactionManager;
        private readonly IPatientsService _patientsService;
        private readonly IEmployeeService _employeeService;
        private readonly IDurableMediator _durableMediator;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CreateAppointmentCommandHandler(
            ISchedulerMeetingsService schedulerMeetingsService,
            ISchedulerBookingsService schedulerBookingsService,
            ILogger<CreateAppointmentCommandHandler> logger,
            IPatientProductsService patientProductsService,
            IFeatureFlagsService featureFlagsService,
            IAppointmentsService appointmentsService,
            ITransactionManager transactionManager,
            IPatientsService patientsService,
            IEmployeeService employeeService,
            IDurableMediator durableMediator,
            IMediator mediator)
        {
            _schedulerBookingsService = schedulerBookingsService;
            _schedulerMeetingsService = schedulerMeetingsService;
            _patientProductsService = patientProductsService;
            _featureFlagsService = featureFlagsService;
            _appointmentsService = appointmentsService;
            _transactionManager = transactionManager;
            _patientsService = patientsService;
            _employeeService = employeeService;
            _mediator = mediator;
            _durableMediator = durableMediator;
            _logger = logger;
        }

        public async Task<Appointment?> Handle(CreateAppointmentCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Create appointment for [EmployeeIds] = {string.Join(',', command.EmployeeIds.ToArray())} has been started.", command);

            var targetType = await GetTargetAppointmentTypeAsync(command);
            
            var targetConfiguration = GetTargetAppointmentTypeConfiguration(targetType, command);

            var abstractType = await GetAbstractAppointmentTypeAsync(command);
            
            var abstractConfiguration =  GetAbstractAppointmentConfiguration(abstractType, command);
            
            AppointmentTypeAvailabilityValidation(command, targetType, targetConfiguration);
            
            var patient = command.PatientId.HasValue
                ? await _patientsService.GetByIdAsync(command.PatientId.Value)
                : null;

            var employees = await _employeeService.GetByIdsAsync(command.EmployeeIds, EmployeeSpecifications.ActiveWithUser);

            await AssertEmployeesAvailableTimeAsync(employees, command.StartDate, command.EndDate);
            
            var recordingConsent = patient?.User.Options.MeetingRecordingConsent ?? false;

            Appointment? appointment = null;

            var product = await GetProductAsync(patient, abstractType);

            var locationId = command.LocationId ?? patient?.LocationId ?? 0;

            await _transactionManager.Run(async () =>
            {
                appointment = new Appointment(
                    patientId: command.PatientId,
                    locationId: locationId,
                    locationType: command.LocationType,
                    startDate: command.StartDate,
                    endDate: command.EndDate,
                    withType: abstractConfiguration?.WithType ?? AppointmentWithType.Other,
                    type: abstractType?.Type,
                    configurationId: abstractConfiguration?.Id,
                    replacedAppointmentId: command.ReplacedAppointmentId
                )
                {
                    Purpose = abstractType?.Purpose ?? AppointmentPurpose.Other,
                    Name = await GetAppointmentNameAsync(command),
                    Comment = command.Comment,
                    Reason = command.Reason,
                    ReasonType = command.ReasonType,
                    TimeZoneId = string.IsNullOrEmpty(command.TimeZoneId)
                        ? TimeZoneInfo.Utc.Id
                        : TimezoneHelper.GetWindowsId(command.TimeZoneId),
                    ProductId = product?.IsLimited ?? false
                        ? product.GetId()
                        : null,
                    AutoRecordingSet = recordingConsent
                };
                
                await _appointmentsService.CreateAppointmentAsync(appointment);
                
                await ApplyProductAsync(product, command);

                await UpdatePatientsTimeZoneAsync(patient, command);

                await SetEmployeesAsync(appointment, employees);
                
                _logger.LogInformation($"Create appointment for [EmployeeIds] = {command.EmployeeIds} has been added into a database.");

                await CreateEventInMeetingServiceAsync(command.PracticeId, appointment, employees, recordingConsent);

                await LinkAppointmentToSchedulerServiceAsync(command.PracticeId, appointment, employees, patient);

                if (patient is not null)
                {
                    await TryAssignEmployee(patient, employees, abstractType);
                }
                
                _logger.LogInformation($"Create appointment for [EmployeeIds] = {command.EmployeeIds} has been finished.");
            },
            (error) =>
            {
                _logger.LogError($"Create appointment for [EmployeeIds] = {command.EmployeeIds} has been failed with [Error]: {error.ToString()}");
            });
            
            var source = command.Source ?? ClientConstants.Source.MobileApp;
            
            var appointmentCreatedEvent = new AppointmentCreatedEvent(
                appointmentId: appointment!.GetId(), 
                createdBy: UserType.Employee,
                isRescheduling: command.IsRescheduling,
                source: source,
                command.PatientId
            );

            await _durableMediator.Publish(appointmentCreatedEvent);

            return await _appointmentsService.GetByIdAsync(appointment!.GetId());
        }

        #region private
        
        private async Task<PatientProduct?> GetProductAsync(Patient? patient, AppointmentType? targetType)
        {
            if (patient is null) return null;
            
            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.PatientProduct))
            {
                return null;
            }

            if (targetType?.RequiredProductType is null)
            {
                return null;
            }

            var product = await _patientProductsService.GetByTypeAsync(
                patientId: patient.GetId(),
                type: targetType.RequiredProductType.Value,
                builtInSourceId: patient.CurrentSubscription?.UniversalId ?? Guid.Empty
            );

            if (product is not null)
            {
                return product;
            }
            
            var buyProductsCommand = new BuyProductsCommand(
                patientId: patient.GetId(),
                products: new[]
                {
                    (targetType.RequiredProductType.Value, 1)
                },
                isPaidByDefaultEmployer: false
            );

            var products = await _mediator.Send(buyProductsCommand);

            return products.First();
        }
        
        private async Task UpdatePatientsTimeZoneAsync(Patient? patient, CreateAppointmentCommand command)
        {
            if (patient is null || command.CreatedBy == UserType.Employee)
            {
                return;
            }
            
            patient.SetTimeZone(command.TimeZoneId);

            await _patientsService.UpdateAsync(patient);
        }

        private async Task SetEmployeesAsync(Appointment appointment, Employee[] employees)
        {
            var appointmentDomain = AppointmentDomain.Create(appointment);

            appointmentDomain.SetEmployees(employees);
            
            await _appointmentsService.EditAppointmentAsync(appointment);
        }

        private async Task AssertEmployeesAvailableTimeAsync(IEnumerable<Employee> employees, DateTime start, DateTime end)
        {
            foreach (var employee in employees)
            {
                var isTimeAvailable = await _appointmentsService.AssertTimeAvailableAsync(
                    from: start,
                    to: end,
                    employeeId: employee.GetId());

                if (isTimeAvailable)
                {
                    continue;
                }
                
                _logger.LogInformation($"Time {start} is not available for {employee.GetId()}");
                    
                throw new AppException(HttpStatusCode.BadRequest, "This time is not available.");
            }
        }
        
        private async Task CreateEventInMeetingServiceAsync(int practiceId, Appointment appointment, Employee[] employees, bool recordingConsent)
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
            var meeting = await TryCreateMeetingAsync(practiceId, appointment, recordingConsent, meetingOwner.User.Email) 
                          ?? await TryCreateMeetingAsync(practiceId, appointment, recordingConsent);

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
            IEnumerable<Employee> employees,
            Patient? patient)
        {
            try
            {
                _logger.LogInformation($"Create booking for appointment with [Id] = {appointment.GetId()} has been started.");
            
                var booking = await _schedulerBookingsService.CreateBookingAsync(practiceId, appointment, employees, patient);
                
                var appointmentDomain = AppointmentDomain.Create(appointment);

                appointmentDomain.SetSchedulerSystemId(booking.Id);

                await _appointmentsService.EditAppointmentAsync(appointment);
            
                _logger.LogInformation($"Create booking for appointment with [Id] = {appointment.GetId()} has been finished.");
            }
            catch (Exception e)
            {
                _logger.LogError($"Create booking for appointment with [Id] = {appointment.GetId()} has been failed with err: {e.ToString()}");
                throw;
            }
        }

        private async Task<string?> GetAppointmentNameAsync(CreateAppointmentCommand command)
        {
            if (!string.IsNullOrEmpty(command.Name))
            {
                return command.Name;
            }

            var types = await _appointmentsService.GetAllTypesAsync(command.PracticeId);

            var type = types.FirstOrDefault(x => x.Id == command.AppointmentTypeId);

            return type?.Name;
        }

        /// <summary>
        /// Trying to create item in meeting service
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="appointment"></param>
        /// <param name="recordingConsent"></param>
        /// <param name="ownerEmail"></param>
        /// <returns></returns>
        private async Task<MeetingModel?> TryCreateMeetingAsync(
            int practiceId, 
            Appointment appointment,
            bool recordingConsent,
            string? ownerEmail = null)
        {
            try
            {
                return await _schedulerMeetingsService.CreateMeetingAsync(
                    practiceId: practiceId, 
                    appointment: appointment,
                    autoRecording: recordingConsent ? ZoomConstants.AutoRecording.Cloud : ZoomConstants.AutoRecording.None,
                    ownerEmail: ownerEmail);
            }
            catch (Exception ex)
            {
                var ownerEmailValue = ownerEmail ?? "<default>";
                
                _logger.LogError($"Create meeting for [PracticeId] = {practiceId} [appointmentId] = {appointment.Id} [ownerEmail] = {ownerEmailValue} with error: {ex.ToString()}");

                return null;
            }
        }

        /// <summary>
        /// If patient selected provider, which is not assigned to him,
        /// we try to assign this provider to patient automatically
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="selectedEmployees"></param>
        /// <param name="type"></param>
        private async Task TryAssignEmployee(Patient patient, Employee[] selectedEmployees, AppointmentType? type)
        {
            if (type is { SelectEmployee: false })
            {
                return;
            }

            var notAssignedEmployeesIds = selectedEmployees
                .Where(x => patient.GetAssignedEmployees().All(t => t.GetId() != x.Id))
                .Select(x => x.GetId())
                .ToArray();

            if (!notAssignedEmployeesIds.Any())
            {
                return;
            }

            var employeesIdsToAssign = patient
                .GetAssignedEmployeesIds()
                .Concat(notAssignedEmployeesIds)
                .ToArray();

            await _patientsService.AssignToEmployeesAsync(patient, employeesIdsToAssign);
        }

        private async Task<AppointmentTypeModel?> GetTargetAppointmentTypeAsync(CreateAppointmentCommand command)
        {
            if (command.PatientId is null || command.CreatedBy == UserType.Employee)
            {
                return null;
            }
            
            var types = await _mediator.Send(new GetAppointmentTypesCommand(command.PatientId.Value));

            return types.FirstOrDefault(x => x.Id == command.AppointmentTypeId);
        }
        
        private AppointmentTypeConfigurationModel? GetTargetAppointmentTypeConfiguration(AppointmentTypeModel? type, CreateAppointmentCommand command)
        {
            if (type is null)
            {
                return null;
            }

            return type.Configurations.First(x => x.Id == command.AppointmentTypeConfigurationId);
        }
        
        private async Task<AppointmentType?> GetAbstractAppointmentTypeAsync(CreateAppointmentCommand command)
        {
            var types = await _appointmentsService.GetAllTypesAsync(command.PracticeId);

            return types.FirstOrDefault(x => x.Id == command.AppointmentTypeId);
        }

        private AppointmentTypeConfiguration? GetAbstractAppointmentConfiguration(AppointmentType? type, CreateAppointmentCommand command)
        {
            return type?.Configurations.FirstOrDefault(x => x.Id == command.AppointmentTypeConfigurationId);
        }
        
        
        private void AppointmentTypeAvailabilityValidation(
            CreateAppointmentCommand command, 
            AppointmentTypeModel? targetType, 
            AppointmentTypeConfigurationModel? targetConfiguration)
        {
            var skipValidation = command.PatientId is null || command.IsRescheduling || command.CreatedBy == UserType.Employee;
            if (skipValidation)
            {
                return;
            }

            if (targetConfiguration != null && targetConfiguration.EarliestNextDate.HasValue)
            {
                var earliestAllowed = targetConfiguration.EarliestNextDate.Value;
                if (command.StartDate < earliestAllowed)
                {
                    throw new AppException(HttpStatusCode.BadRequest,
                        $"The appointment cannot start before {earliestAllowed.ToUniversalTime()} UTC.");
                }
            } 
            
            var isTypeUnavailable = (targetType is null || targetConfiguration is null)
                || !targetType.IsCreateAvailable
                || !string.IsNullOrEmpty(targetType.UnavailabilityReason);
            
            if (isTypeUnavailable)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Cannot create appointment with this type.");
            }
        }

        private async Task ApplyProductAsync(PatientProduct? product, CreateAppointmentCommand command)
        {
            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.PatientProduct))
            {
                return;
            }
            
            if (product is null)
            {
                return;
            }
            
            await _patientProductsService.UseAsync(
                id: product.GetId(), 
                usedBy: command.CreatedById.ToString(),
                usedAt: command.StartDate
            );
        }
        
        #endregion
    }
}