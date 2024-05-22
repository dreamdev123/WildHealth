using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Timeline.Subscription;
using WildHealth.IntegrationEvents.PaymentIssue;
using WildHealth.IntegrationEvents.PaymentIssue.Payloads;
using static WildHealth.Domain.Enums.Payments.PaymentIssueStatus;

namespace WildHealth.Application.Domain.PaymentIssues;

public record ProcessPaymentIssueFlow(
    PaymentIssue PaymentIssue, 
    PaymentIssueStatus? NewStatus,
    PaymentIssueNotificationTimeWindow NotificationTimeWindow,
    DateTime Now,
    PaymentIssueOptions Config,
    string? PaymentLink = null,
    string? PatientProfileLink = null) : IMaterialisableFlow
{
    // Subscription Payment Issue state machine matrix where Key is current state and value is an array of states available to transition to 
    private readonly Dictionary<PaymentIssueStatus, PaymentIssueStatus[]> SubscriptionStateMachineTransitionMatrix = new()
    {
        {WaitingPatientNotification, new []{ WaitingPatientNotification, PatientNotified, CareCoordinatorNotified, UserCancelled, Resolved }},
        {PatientNotified, new []{ PatientNotified, WaitingPatientNotification, CareCoordinatorNotified, UserCancelled, Resolved }},
        {CareCoordinatorNotified, new []{ UserCancelled, Resolved, Expired }},
        {UserCancelled, Array.Empty<PaymentIssueStatus>()}, // final status
        {Expired, Array.Empty<PaymentIssueStatus>()}, // final status
        {Resolved, Array.Empty<PaymentIssueStatus>()}, // final status
    };

    private readonly Dictionary<PaymentIssueStatus, PaymentIssueStatus[]> OrderStateMachineTransitionMatrix = new()
    {
        { PatientNotified, new[] { Resolved, UserCancelled } }
    };

    private readonly Dictionary<PaymentIssueStatus, PaymentIssueStatus[]> InsuranceStateMachineTransitionMatrix = new()
    {
        { PatientNotified, new[] { Resolved, UserCancelled } }
    };
    
    private readonly Dictionary<PaymentIssueStatus, PaymentIssueStatus[]> PremiumSubscriptionStateMachineTransitionMatrix = new()
    {
        { WaitingPatientNotification, new[] { PatientNotified } }
    };

    private bool IsValidNewStatus(PaymentIssueStatus currentStatus, PaymentIssueStatus? newStatus)
    {
        return newStatus.HasValue && PaymentIssue.Type switch
        {
            PaymentIssueType.Subscription => SubscriptionStateMachineTransitionMatrix
                .TryGetValue(currentStatus, out var allowedStatuses) && allowedStatuses.Contains(newStatus.Value),
            PaymentIssueType.Order => OrderStateMachineTransitionMatrix
                .TryGetValue(currentStatus, out var allowedStatuses) && allowedStatuses.Contains(newStatus.Value),
            PaymentIssueType.InsuranceResponsibility => InsuranceStateMachineTransitionMatrix
                .TryGetValue(currentStatus, out var allowedStatuses) && allowedStatuses.Contains(newStatus.Value),
            PaymentIssueType.PremiumSubscription => PremiumSubscriptionStateMachineTransitionMatrix
                .TryGetValue(currentStatus, out var allowedStatuses) && allowedStatuses.Contains(newStatus.Value),
            _ => false
        };
    }

    public MaterialisableFlowResult Execute()
    {
        var newStatus = CalcNextStatus();
        
        if (!IsValidNewStatus(PaymentIssue.Status, newStatus))
            return MaterialisableFlowResult.Empty;
        
        return (newStatus, PaymentIssue.Type) switch
        {
            (PatientNotified, PaymentIssueType.PremiumSubscription) => FirePremiumFailureAlert(), 
            (WaitingPatientNotification, PaymentIssueType.Subscription) when NotificationTimeWindow.IsInWindow(Now) => NotifyPatient(),
            (WaitingPatientNotification, PaymentIssueType.Subscription) => ScheduleNotification(),
            (PatientNotified, PaymentIssueType.Subscription) when NotificationTimeWindow.IsInWindow(Now) => NotifyPatient(),
            (PatientNotified, PaymentIssueType.Subscription) when PaymentIssue.Status == PatientNotified => ScheduleNotification(),
            (CareCoordinatorNotified, PaymentIssueType.Subscription) when PaymentIssueLasts(Config.DaysBeforeNotifyCareCoordinator) => NotifyCareCoordinator(),
            (Expired, PaymentIssueType.Subscription) when PaymentIssueLasts(Config.DaysBeforeExpire) => Expire(),
            (Resolved, _) => ResolveIssue(), // payment succeeded 
            (UserCancelled, _) => PaymentIssue.UserCancel(),
            _ => MaterialisableFlowResult.Empty
        };
    }

    private PaymentIssueStatus? CalcNextStatus()
    {
        return NewStatus ?? (PaymentIssue.Status, PaymentIssue.Type) switch
        {
            (_, PaymentIssueType.Subscription) when PaymentIssueLasts(Config.DaysBeforeExpire) => Expired,
            (_, PaymentIssueType.Subscription) when PaymentIssueLasts(Config.DaysBeforeNotifyCareCoordinator) => CareCoordinatorNotified,
            (WaitingPatientNotification, PaymentIssueType.Subscription) => PatientNotified,
            _ => null // current status should not be changed
        };
    }
    
    private MaterialisableFlowResult NotifyPatient()
    {
        if (string.IsNullOrEmpty(PaymentLink))
            throw new DomainException("Invalid payment link");

        // we fire integration event only when current status is PatientNotified. If current status is WaitingPatientNotification then this event has already been fired
        MaterialisableFlowResult FireIntegrationEvent() =>
            PaymentIssue.Status == PatientNotified ? BuildPaymentFailedIntegrationEvent().ToFlowResult() : MaterialisableFlowResult.Empty;

        var notification = new SubscriptionPaymentIssuePatientNotification(PaymentIssue.Patient, PaymentLink);
        return BuildAggregateEvent(PatientNotified).ToFlowResult() + notification + FireIntegrationEvent();
    }
    
    private MaterialisableFlowResult FirePremiumFailureAlert()
    {
        var notification = new PremiumPaymentFailedNotification(PatientProfileLink, PaymentIssue.Patient.User.PracticeId, Now);
        
        return BuildAggregateEvent(PatientNotified).ToFlowResult() + notification;
    }
    
    private MaterialisableFlowResult NotifyCareCoordinator()
    {
        var patient = PaymentIssue.Patient;
        
        if (string.IsNullOrEmpty(PatientProfileLink))
        {
            throw new DomainException("Invalid patient profile link");
        }
        
        var outstandingAmount = PaymentIssue.Integration.SubscriptionIntegration.CurrentSubscription().Price;

        var notification = new SubscriptionPaymentIssueCareCoordinatorNotification(patient, PatientProfileLink, outstandingAmount);

        return BuildAggregateEvent(CareCoordinatorNotified).ToFlowResult() + notification;
    }
    
    /// <summary>
    /// Assign WaitingPatientNotification status when notification time window is not satisfied
    /// </summary>
    private MaterialisableFlowResult ScheduleNotification()
    {
        var (userId, outstandingPrice) = (PaymentIssue.Patient.User.UniversalId.ToString(), PaymentIssue.OutstandingAmount);
        var integrationEvent = new PaymentIssueIntegrationEvent(new PaymentFailedPayload(userId, outstandingPrice, 0), Now);
        return BuildAggregateEvent(WaitingPatientNotification).ToFlowResult() + integrationEvent + BuildPaymentFailedIntegrationEvent();
    }

    private MaterialisableFlowResult ResolveIssue()
    {
        var (userId, outstandingPrice) = (PaymentIssue.Patient.User.UniversalId.ToString(), PaymentIssue.OutstandingAmount);
        var integrationEvent = new PaymentIssueIntegrationEvent(new PaymentIssueResolved(userId, outstandingPrice, PaymentIssue.DaysOverdue(Now)), Now);
        return BuildAggregateEvent(Resolved).ToFlowResult() + integrationEvent;
    }

    private MaterialisableFlowResult Expire() => 
        new SubscriptionDidNotRenewTimelineEvent(PaymentIssue.PatientId, Now).Added() + // Log renewal failure timeline event
        new PaymentIssueExpiredEvent(PaymentIssue.GetId()) + 
        BuildAggregateEvent(Expired);

    private bool PaymentIssueLasts(int days) => 
        (Now.Date - PaymentIssue.CreatedAt.Date).Days >= days;

    private PaymentIssueStatusChanged BuildAggregateEvent(PaymentIssueStatus status) =>
        new(PaymentIssue.GetId(), new PaymentIssueStatusChangedData(status));

    private PaymentIssueIntegrationEvent BuildPaymentFailedIntegrationEvent()
    {
        var (userId, outstandingPrice) = (PaymentIssue.Patient.User.UniversalId.ToString(), PaymentIssue.OutstandingAmount);
        return new PaymentIssueIntegrationEvent(new PaymentFailedPayload(userId, outstandingPrice, PaymentIssue.DaysOverdue(Now)), Now);
    }
}