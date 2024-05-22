using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using OneOf.Monads;
using WildHealth.Application.Domain.PatientJourney.Commands;
using WildHealth.Application.Domain.PatientJourney.Flows;
using WildHealth.Application.Domain.PatientJourney.Services;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Events.Questionnaires;
using WildHealth.Application.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Questionnaires;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.PatientJourney;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Enums.PatientJourney;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.Domain.PatientJourney.Handlers;

public class CompletePatientJourneyTaskCommandHandler : 
    IRequestHandler<CompletePatientJourneyTaskCommand>, 
    INotificationHandler<QuestionnaireCompletedEvent>,
    INotificationHandler<OrderStatusChangedEvent>
{
    private readonly IPatientJourneyService _patientJourneyService;
    private readonly MaterializeFlow _materializer;
    private readonly IUsersService _usersService;
    private readonly IQuestionnairesService _questionnaireService;
    private readonly IPatientJourneyTreeBuilder _journeyTreeBuilder;
    private readonly IPatientProfileService _profileService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFeatureFlagsService _featureFlagsService;

    public CompletePatientJourneyTaskCommandHandler(
        IPatientJourneyService patientJourneyService, 
        MaterializeFlow materializer, 
        IUsersService usersService, 
        IQuestionnairesService questionnaireService, 
        IPatientJourneyTreeBuilder journeyTreeBuilder, 
        IPatientProfileService profileService, 
        IDateTimeProvider dateTimeProvider, 
        IFeatureFlagsService featureFlagsService)
    {
        _patientJourneyService = patientJourneyService;
        _materializer = materializer;
        _usersService = usersService;
        _questionnaireService = questionnaireService;
        _journeyTreeBuilder = journeyTreeBuilder;
        _profileService = profileService;
        _dateTimeProvider = dateTimeProvider;
        _featureFlagsService = featureFlagsService;
    }

    public async Task Handle(CompletePatientJourneyTaskCommand request, CancellationToken cancellationToken)
    {
        var journeyTree = await _journeyTreeBuilder.Build(request.PatientId, request.PaymentPlanId, request.PracticeId);
        journeyTree.ThrowIfEmpty();
        var journeyTask = await _patientJourneyService.GetJourneyTask(request.JourneyTaskId);
        var patientJourneyTask = await _patientJourneyService.GetPatientJourneyTask(request.PatientId, request.JourneyTaskId).ToOption();
        var dashboardUrl = await _profileService.GetDashboardLink(request.PracticeId);
        var user = await _usersService.GetByPatientIdAsync(request.PatientId);
        
        await new CompletePatientJourneyTaskFlow(
            request.PatientId, 
            journeyTask, 
            patientJourneyTask, 
            JourneyTaskCompletedBy.Patient, 
            user, 
            journeyTree,
            dashboardUrl,
            DateTime.UtcNow,
            _featureFlagsService.GetFeatureFlag(FeatureFlags.PatientJourney)).Materialize(_materializer);
    }

    /// <summary>
    /// All Health Forms completed
    /// </summary>
    public async Task Handle(QuestionnaireCompletedEvent notification, CancellationToken cancellationToken)
    {
        var patientId = notification.Patient.GetId();
        if (await _questionnaireService.AnyAvailableAsync(patientId)) 
            return; // break if NOT all Health Forms completed
        
        await Complete(AutomaticCompletionPrerequisite.HealthFormsCompleted, patientId, JourneyTaskCompletedBy.System);
    }
    
    /// <summary>
    /// Labs Resulted; DNA Resulted
    /// </summary>
    public async Task Handle(OrderStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        var order = notification.Order;
        if (order is { Status: OrderStatus.Completed, Type: OrderType.Lab or OrderType.Dna })
        {
            var patientId = order.PatientId;
            var prerequisite = order.Type.ToJourneyTaskCompletionPrerequisite();

            await Complete(prerequisite, patientId, JourneyTaskCompletedBy.System);
        }
    }

    private async Task Complete(AutomaticCompletionPrerequisite prerequisite, int patientId, JourneyTaskCompletedBy completedBy)
    {
        if (prerequisite == AutomaticCompletionPrerequisite.None) 
            return;

        var membershipInfo = await _profileService.GetMembershipInfo(patientId, _dateTimeProvider.UtcNow());
        var journeyTree = await _journeyTreeBuilder.Build(patientId, membershipInfo.PaymentPlanId, membershipInfo.PracticeId);
        var taskIds = journeyTree.GetTasksQualifiedForAutoCompletion(prerequisite);
        if (taskIds.Empty()) return;
        var journeyTasks = await _patientJourneyService.GetJourneyTasks(taskIds);
        var patientJourneyTasks = await _patientJourneyService.GetPatientJourneyTasks(patientId, taskIds);
        var dashboardUrl = await _profileService.GetDashboardLink(membershipInfo.PracticeId);
        var user = await _usersService.GetByPatientIdAsync(patientId);
        
        foreach (var journeyTask in journeyTasks)
            await new CompletePatientJourneyTaskFlow(
                patientId, 
                journeyTask, 
                journeyTask.LookupPatientTask(patientJourneyTasks)!.ToOption(), 
                completedBy, 
                user, 
                journeyTree,
                dashboardUrl,
                DateTime.UtcNow,
                _featureFlagsService.GetFeatureFlag(FeatureFlags.PatientJourney)).Materialize(_materializer);
    }
}