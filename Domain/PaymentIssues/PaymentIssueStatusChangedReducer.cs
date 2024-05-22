using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.EventSourcing;

namespace WildHealth.Application.Domain.PaymentIssues;

[AggregateEvent("PaymentIssueStatusChanged")]
public record PaymentIssueStatusChanged(int EntityId, PaymentIssueStatusChangedData EventData) : IAggregateEvent<PaymentIssueStatusChangedData>;

public record PaymentIssueStatusChangedData(PaymentIssueStatus Status);

public class PaymentIssueStatusChangedReducer : IAggregateReducer<PaymentIssue, PaymentIssueStatusChanged, PaymentIssueStatusChangedData>
{
    public PaymentIssue Reduce(PaymentIssue entity, PaymentIssueStatusChangedData data)
    {
        entity.Status = data.Status;
        if (entity.Status == PaymentIssueStatus.PatientNotified)
            entity.RetryAttempt++;
        
        return entity;
    }
}