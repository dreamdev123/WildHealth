using System;

namespace WildHealth.Application.Domain.PaymentIssues;

public record PaymentIssueNotificationTimeWindow(TimeSpan From, TimeSpan To)
{
    public bool IsInWindow(DateTime timestamp)
    {
        var (from, to) = (timestamp.Date + From, timestamp + To);
        return timestamp >= from && timestamp <= to;
    }

    public static PaymentIssueNotificationTimeWindow Default => new(new TimeSpan(16, 0, 0), new TimeSpan(22, 0, 0)); // 12-6PM EST
    public static PaymentIssueNotificationTimeWindow AllDay => new(new TimeSpan(0, 0, 0), new TimeSpan(23, 59, 59));
}