using MediatR;

namespace WildHealth.Application.Domain.PaymentIssues;

public record PaymentIssueExpiredEvent(int PaymentIssueId) : INotification;