using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf.Monads;
using WildHealth.Application.Domain.PatientJourney;
using WildHealth.Application.Domain.PatientJourney.Flows;
using WildHealth.Application.Domain.PatientJourney.Services;
using WildHealth.Application.EventHandlers.Scheduler.Flows;
using WildHealth.Application.Events.Scheduler;
using WildHealth.Application.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.PatientJourney;

namespace WildHealth.Application.EventHandlers.Scheduler;

public class SchedulerBookingCompletedEventHandler : INotificationHandler<SchedulerBookingCompletedEvent>
{
    private readonly IAppointmentsService _appointmentsService;
    private readonly IEmployeeService _employeeService;
    private readonly MaterializeFlow _materializeFlow;
    private readonly IPatientJourneyService _patientJourneyService;
    private readonly IPatientProfileService _profileService;
    private readonly IPatientJourneyTreeBuilder _journeyTreeBuilder;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUsersService _usersService;
    private readonly IFeatureFlagsService _featureFlagsService;

    public SchedulerBookingCompletedEventHandler(
        IAppointmentsService appointmentsService, 
        IEmployeeService employeeService, 
        MaterializeFlow materializeFlow, 
        IPatientJourneyService patientJourneyService, 
        IPatientProfileService profileService, 
        IPatientJourneyTreeBuilder journeyTreeBuilder, 
        IDateTimeProvider dateTimeProvider, 
        IUsersService usersService, 
        IFeatureFlagsService featureFlagsService)
    {
        _appointmentsService = appointmentsService;
        _employeeService = employeeService;
        _materializeFlow = materializeFlow;
        _patientJourneyService = patientJourneyService;
        _profileService = profileService;
        _journeyTreeBuilder = journeyTreeBuilder;
        _dateTimeProvider = dateTimeProvider;
        _usersService = usersService;
        _featureFlagsService = featureFlagsService;
    }

    public async Task Handle(SchedulerBookingCompletedEvent notification, CancellationToken cancellationToken)
    {
        var appointment = await _appointmentsService.GetBySchedulerSystemIdAsync(notification.BookingId);
        if (appointment is null) return;
        var employee = await _employeeService.GetBySchedulerAccountIdAsync(notification.SchedulerUserId);
        var patientId = appointment.PatientId!.Value;
        var prerequisite = appointment.WithType.ToJourneyTaskCompletionPrerequisite();
        var membershipInfo = await _profileService.GetMembershipInfo(patientId, _dateTimeProvider.UtcNow());
        var journeyTree = await _journeyTreeBuilder.Build(patientId, membershipInfo.PaymentPlanId, membershipInfo.PracticeId);
        var taskIds = journeyTree.GetTasksQualifiedForAutoCompletion(prerequisite);
        if (taskIds.Empty()) return;
        var journeyTasks = await _patientJourneyService.GetJourneyTasks(taskIds);
        var patientJourneyTasks = await _patientJourneyService.GetPatientJourneyTasks(patientId, taskIds);
        var dashboardUrl = await _profileService.GetDashboardLink(membershipInfo.PracticeId);
        var user = await _usersService.GetByPatientIdAsync(patientId);
        
        await new CompleteAppointmentFlow(
            appointment, 
            employee, 
            notification.Completed, 
            DateTime.UtcNow).Materialize(_materializeFlow);
        
        foreach (var journeyTask in journeyTasks)
            await new CompletePatientJourneyTaskFlow(
                patientId, 
                journeyTask, 
                journeyTask.LookupPatientTask(patientJourneyTasks)!.ToOption(), 
                JourneyTaskCompletedBy.System, 
                user, 
                journeyTree,
                dashboardUrl,
                DateTime.UtcNow,
                _featureFlagsService.GetFeatureFlag(FeatureFlags.PatientJourney)).Materialize(_materializeFlow);
    }
}