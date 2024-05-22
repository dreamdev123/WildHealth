using System;
using System.Collections.Generic;
using System.Linq;
using OneOf.Monads;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Orders;
using WildHealth.IntegrationEvents.PaymentIssue;
using WildHealth.IntegrationEvents.PaymentIssue.Payloads;

namespace WildHealth.Application.Domain.PaymentIssues;

public record CreatePaymentIssueFlow(
    WildHealth.Domain.Entities.Integrations.Integration Integration,
    DateTime Now, 
    Option<int> PatientId) : IMaterialisableFlow
{
    public MaterialisableFlowResult Execute()
    {
        var paymentIssue = CreatePaymentIssue();
        if (paymentIssue is null)
            return MaterialisableFlowResult.Empty;
        
        Integration.PaymentIssues.Add(paymentIssue);
        return Integration.Updated() + BuildIntegrationEvent();
    }

    private PaymentIssue? CreatePaymentIssue()
    {
        if (Integration.SubscriptionIntegration is not null && Integration.SubscriptionIntegration.Any() && !IsSigningUp(Integration.SubscriptionIntegration))
            return new PaymentIssue
            {
                IntegrationId = Integration.GetId(), 
                Status = PaymentIssueStatus.WaitingPatientNotification,
                Type = PaymentIssueType.Subscription,
                OutstandingAmount = GetPaymentData().outstandingAmount,
                PatientId = Integration.SubscriptionIntegration.CurrentSubscription().PatientId
            };
        else if (Integration.OrderInvoiceIntegration is not null)
            return new PaymentIssue
            {
                IntegrationId = Integration.GetId(),
                Status = PaymentIssueStatus.PatientNotified,
                Type = PaymentIssueType.Order,
                OutstandingAmount = GetPaymentData().outstandingAmount,
                PatientId = Integration.OrderInvoiceIntegration.Order.PatientId
            };
        else if (Integration.ClaimIntegration is not null)
        {
            if (PatientId.IsNone())
                throw new DomainException($"PatientId should be provided when Insurance payment issue occurs. Integration ID: {Integration.GetId()}");
                
            return new PaymentIssue
            {
                IntegrationId = Integration.GetId(),
                Status = PaymentIssueStatus.PatientNotified,
                Type = PaymentIssueType.InsuranceResponsibility,
                OutstandingAmount = 0, // Due to technical limitations we can't get calc InsuranceResponsibilityAmount because it's not stored in our system
                PatientId = PatientId.Value()
            };
        }
        else if (Integration.PaymentScheduleItemIntegration is not null)
        {  
            return new PaymentIssue
            {
                IntegrationId = Integration.GetId(),
                Status = PaymentIssueStatus.WaitingPatientNotification, 
                Type = PaymentIssueType.PremiumSubscription,
                OutstandingAmount = Integration.PaymentScheduleItemIntegration.PaymentScheduleItem.Amount,
                PatientId = PatientId.Value()
            };
        }
        
        else
            return null;
    }

    /// <summary>
    /// Returns true if a patient is signing up right now. Otherwise, false
    /// </summary>
    private bool IsSigningUp(ICollection<SubscriptionIntegration> integrationSubscriptionIntegration)
    {
        return 
            integrationSubscriptionIntegration.CurrentSubscription().Patient.Subscriptions.Count == 1 && 
            integrationSubscriptionIntegration.CurrentSubscription().CreatedAt.Date == Now.Date;
    }

    private MaterialisableFlowResult BuildIntegrationEvent()
    {
        if (Integration.ClaimIntegration is not null)
            return MaterialisableFlowResult.Empty; // We don't fire integration event for insurance payment issue because we don't have enough info like InsuranceResponsibilityAmount
        
        var (userId, outstandingPrice) = GetPaymentData();
        return new PaymentIssueIntegrationEvent(new PaymentFailedPayload(userId.ToString(), outstandingPrice, 0), Now).ToFlowResult();
    }
    
    private (Guid universalId, decimal outstandingAmount) GetPaymentData()
    {
        if (Integration.SubscriptionIntegration is not null && Integration.SubscriptionIntegration.Any())
            return (Integration.SubscriptionIntegration.CurrentSubscription().Patient.User.UniversalId, Integration.SubscriptionIntegration.CurrentSubscription().Price);
        else if (Integration.OrderInvoiceIntegration is not null)
            return (Integration.OrderInvoiceIntegration.Order.Patient.User.UniversalId, OrderDomain.Create(Integration.OrderInvoiceIntegration.Order).TotalPrice());
        else if (Integration.PaymentScheduleItemIntegration is not null)
            return (Integration.PaymentScheduleItemIntegration.PaymentScheduleItem.Payment.Subscriptions.First().Patient.User.UniversalId, Integration.PaymentScheduleItemIntegration.PaymentScheduleItem.Amount);
        else if (Integration.ClaimIntegration is not null)
            throw new DomainException($"Payment info is not available for ClaimIntegration. {Integration.GetId()}");
        else
            throw new DomainException($"Unknown integration. ID: {Integration.GetId()}");
    }
}