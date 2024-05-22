using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Schedulers.Availability;
using WildHealth.Application.Services.Users;
using WildHealth.Common.Enums.Patients;
using WildHealth.Common.Models.LeadSources;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Common.Models.Users;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Domain.Exceptions;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Enums;
using AutoMapper;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Patients;

public class ChangeHealthCoachCommandHandler : IRequestHandler<ChangeStaffCommand>
{
    private readonly ISchedulerAvailabilityService _schedulerAvailabilityService;
    private readonly IAppointmentsService _appointmentsService;
    private readonly IUsersService _usersService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;
    
    public ChangeHealthCoachCommandHandler(
        ISchedulerAvailabilityService schedulerAvailabilityService,
        IAppointmentsService appointmentsService,
        IUsersService usersService,
        IMediator mediator,
        IMapper mapper,
        ILogger<ChangeHealthCoachCommandHandler> logger)
    {
        _schedulerAvailabilityService = schedulerAvailabilityService;
        _appointmentsService = appointmentsService;
        _usersService = usersService;
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task Handle(ChangeStaffCommand command, CancellationToken cancellationToken)
    {
        var patientEmail = command.PatientEmail;
        var priorHealthCoachEmail = command.FromHealthCoachEmail;
        var newHealthCoachEmail = command.ToHealthCoachEmail;
        var priorProviderEmail = command.FromProviderEmail;
        var newProviderEmail = command.ToProviderEmail;
        
        var reassignmentType = 
            !string.IsNullOrEmpty(priorHealthCoachEmail) && !string.IsNullOrEmpty(priorProviderEmail) ? PatientReassignmentType.Both :
            !string.IsNullOrEmpty(priorHealthCoachEmail) ? PatientReassignmentType.HealthCoachOnly :
            !string.IsNullOrEmpty(priorProviderEmail) ? PatientReassignmentType.ProviderOnly :
            throw new Exception($"Unable to determine assignment of [PatientEmail] = {patientEmail}");
        
        _logger.LogInformation(
            $"Attempting to change health coach for [PatientEmail] = {patientEmail}");

        var patientUser =
            await _usersService.GetByEmailAsync(patientEmail, UserSpecifications.UserWithPatientAndAppointments);

        var patient = patientUser.Patient;

        var staffReplacementSummaries = new List<NewStaffSummaryModel>();

        if (!String.IsNullOrEmpty(priorHealthCoachEmail))
        {
            staffReplacementSummaries.Add(new NewStaffSummaryModel()
            {
                priorEmail = priorHealthCoachEmail,
                newEmail = newHealthCoachEmail
            });
        }

        if (!String.IsNullOrEmpty(priorProviderEmail))
        {
            staffReplacementSummaries.Add(new NewStaffSummaryModel()
            {
                priorEmail = priorProviderEmail,
                newEmail = newProviderEmail
            });
        }

        var newEmployeeIds = patient.GetAssignedEmployeesIds().ToList();

        foreach (var summary in staffReplacementSummaries)
        {
            var priorEmail = summary.priorEmail;
            var newEmail = summary.newEmail;
            
            var priorUser =
                await _usersService.GetByEmailAsync(priorEmail,
                    UserSpecifications.UserWithEmployee);

            var newUser =
                await _usersService.GetByEmailAsync(newEmail,
                    UserSpecifications.UserWithEmployee);

            summary.priorUser = priorUser;
            summary.newUser = newUser;
            
            // Remove the old coach
            newEmployeeIds.Remove(priorUser.Employee.GetId());
        
            // Add the new one
            newEmployeeIds.Add(newUser.Employee.GetId());
        }
        
        var updatePatientCommand = new UpdatePatientCommand(
            id: patient.GetId(),
            firstName: patientUser.FirstName,
            lastName: patientUser.LastName,
            birthday: patientUser.Birthday,
            gender: patientUser.Gender,
            email: patientUser.Email,
            phoneNumber: patientUser.PhoneNumber,
            billingAddress: _mapper.Map<AddressModel>(patientUser.BillingAddress),
            shippingAddress: _mapper.Map<AddressModel>(patientUser.ShippingAddress),
            options: _mapper.Map<PatientOptionsModel>(patient.Options),
            employeeIds: newEmployeeIds.ToArray(),
            leadSource: patient.PatientLeadSource == null ? null : _mapper.Map<PatientLeadSourceModel>(patient.PatientLeadSource)
        );

        await _mediator.Send(updatePatientCommand, cancellationToken);

        if (command.AttemptToRescheduleIndividualAppointments)
        {
            foreach (var summary in staffReplacementSummaries)
            {
                var priorUser = summary.priorUser;
                var newUser = summary.newUser;

                var roleId = priorUser.Employee.RoleId;

                var withType =
                    roleId == WildHealth.Domain.Constants.Roles.CoachId ? AppointmentWithType.HealthCoach :
                    roleId == WildHealth.Domain.Constants.Roles.ProviderId ? AppointmentWithType.HealthCoachAndProvider :
                    throw new DomainException($"Unexpected [RoleId] when migrating staff");
                
                // Only appointments that are of the following
                // 1. Of just the type relevant to the person being moved
                // 2. Not cancelled (submitted)
                // 3. In the future
                // 4. does not include the new staff member
                foreach (var appointment in patient.Appointments.Where(o =>
                             o.StartDate >= DateTime.UtcNow &&
                             o.Status == AppointmentStatus.Submitted &&
                             o.WithType == withType &&
                             !o.Employees.Select(e => e.EmployeeId).Contains(newUser.Employee.GetId())))
                {
                    if (appointment.ConfigurationId is null)
                    {
                        _logger.LogError($"Problem handling [AppointmentId] = {appointment.GetId()} because we could not locate the appointment configuration");
                        
                        continue;
                    }
                    
                    var (type, configuration) = await _appointmentsService.GetTypeByConfigurationIdAsync(
                        practiceId: patientUser.PracticeId,
                        configurationId: appointment.ConfigurationId.Value
                    );

                    try
                    {
                        // Create the appointment
                        var newAppointment = await TryCreateNewStaffAppointment(
                            priorAppointment: appointment, 
                            type: type,
                            configuration: configuration,
                            newStaffUser: newUser
                        );
                        
                        var newAppointmentDomain = AppointmentDomain.Create(newAppointment);
                        
                        // Cancel prior appointment
                        await _mediator.Send(new CancelAppointmentCommand(
                            id: appointment.GetId(),
                            cancelledBy: 0,
                            cancellationReason: AppointmentCancellationReason.Reschedule
                        ), cancellationToken);

                        summary.IsSameAppointmentSlot = true;
                        summary.AppointmentDateTime = newAppointmentDomain.GetTimeZoneStartTime();
                        summary.TimeZoneId = newAppointment.TimeZoneId;
                    }
                    catch (AppException ex)
                    {
                        if (ex.StatusCode == HttpStatusCode.BadRequest && ex.Message == "This time is not available.")
                        {
                            if (command.AttemptToRescheduleInDifferentTimeSlot)
                            {
                                // If error creating in the same slot, then let's just reschedule the next available slot
                                var newAppointment = await RescheduleNewStaffAppointment(
                                    priorAppointment: appointment, 
                                    type: type,
                                    configuration: configuration,
                                    newStaffUser: newUser
                                );

                                if (newAppointment is not null)
                                {
                                    var newAppointmentDomain = AppointmentDomain.Create(newAppointment);

                                    summary.IsSameAppointmentSlot = false;
                                    summary.AppointmentDateTime = newAppointmentDomain.GetTimeZoneStartTime();
                                    summary.TimeZoneId = newAppointment.TimeZoneId;
                                }
                            }
                            else
                            {
                                _logger.LogInformation($"Failed to schedule appointment for [PatientEmail] = {patientEmail}, on [StartDate] = {appointment.StartDate}, this will need to manually be rescheduled");
                            }
                        }
                    }
                }
            }

            if (command.ShouldSendChangeMessageToPatient)
            {
                // Publish the event and take action accordingly
                await _mediator.Publish(new PatientReassignedEvent(
                    PatientReassignmentType: reassignmentType,
                    PatientId: patient.GetId(),
                    SummaryModels: staffReplacementSummaries.ToArray()
                ), cancellationToken);
            }
        }
    }

    private async Task<Appointment?> RescheduleNewStaffAppointment(
        Appointment priorAppointment, 
        AppointmentType type,
        AppointmentTypeConfiguration configuration,
        User newStaffUser)
    {
        var priorAppointmentDomain = AppointmentDomain.Create(priorAppointment);
        var availability = await _schedulerAvailabilityService.GetAvailabilityAsync(
                practiceId: priorAppointmentDomain.GetPracticeId(), 
                configurationId: configuration.GetId(),
                employeeIds: new [] { newStaffUser.Employee.GetId() }, 
                from: priorAppointmentDomain.GetTimeZoneStartTime(), 
                to: priorAppointmentDomain.GetTimeZoneStartTime().AddDays(90)
        );

        var firstAvailableSlot = availability.FirstOrDefault();

        if (firstAvailableSlot is not null)
        {
            return await _mediator.Send(new RescheduleAppointmentCommand(
                cancelledAppointmentId: priorAppointment.GetId(),
                practiceId: priorAppointmentDomain.GetPracticeId(),
                employeeIds: new[] {newStaffUser.Employee.GetId()},
                patientId: priorAppointment.PatientId,
                locationId: priorAppointment.LocationId,
                startDate: firstAvailableSlot.Start,
                endDate: firstAvailableSlot.End,
                locationType: priorAppointment.LocationType,
                appointmentTypeId: type.Id,
                appointmentTypeConfigurationId: configuration.Id,
                comment: priorAppointment.Comment,
                timeZoneId: priorAppointment.TimeZoneId,
                name: priorAppointment.Name,
                userType: UserType.Employee,
                createdById: priorAppointment.CreatedBy,
                reason: priorAppointment.Reason,
                reasonType: priorAppointment.ReasonType,
                isPatientRequesting: false));
        }

        return null;
    }

    private async Task<Appointment> TryCreateNewStaffAppointment(
        Appointment priorAppointment, 
        AppointmentType type,
        AppointmentTypeConfiguration configuration,
        User newStaffUser)
    {
        // Creating a replacement will always be created with just a single employee
        var employeeIds = GetEmployeeIdsForReplacementAppointment(priorAppointment, newStaffUser);
        var priorAppointmentDomain = AppointmentDomain.Create(priorAppointment);
        
        return (await _mediator.Send(new CreateAppointmentCommand(
            practiceId: priorAppointmentDomain.GetPracticeId(),
            employeeIds: employeeIds,
            patientId: priorAppointment.PatientId, 
            locationId: priorAppointment.LocationId, 
            startDate: priorAppointment.StartDate, 
            endDate: priorAppointment.EndDate, 
            locationType: priorAppointment.LocationType, 
            appointmentTypeId: type.Id,
            appointmentTypeConfigurationId: configuration.Id,
            name: priorAppointment.Name,
            comment: priorAppointment.Comment,
            timeZoneId: priorAppointment.TimeZoneId,
            userType: UserType.Employee,
            createdById: priorAppointment.CreatedBy,
            reason: priorAppointment.Reason,
            reasonType: priorAppointment.ReasonType,
            isRescheduling: true,
            replacedAppointmentId: priorAppointment.GetId() 
        )))!;
    }

    private int[] GetEmployeeIdsForReplacementAppointment(Appointment appointment, User newStaffUser)
    {
        if (appointment.WithType == AppointmentWithType.HealthCoachAndProvider ||
            appointment.WithType == AppointmentWithType.HealthCoach)
        {
            return new[] {newStaffUser.Employee.GetId()};
        }
        
        // Anticipate new A

        throw new DomainException(
            $"Unable to transfer patient, unexpected [AppointmentWithType] = {appointment.WithType}");
    }
}