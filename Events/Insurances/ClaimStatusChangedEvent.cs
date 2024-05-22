using System;
using MediatR;

namespace WildHealth.Application.Events.Insurances;

public class ClaimStatusChangedEvent : INotification
{
    public int ClaimId { get; }
    
    public int PracticeId { get; }
    
    public DateTime Date { get; }

    public string Status { get; }

    public string StatusCode { get; }

    public string Category { get; }
    
    public int NewStatusId { get; }

    public ClaimStatusChangedEvent(int claimId, int practiceId, DateTime date, int newStatusId, string status, string statusCode, string category)
    {
        ClaimId = claimId;
        PracticeId = practiceId;
        Date = date;
        NewStatusId = newStatusId;
        Status = status;
        StatusCode = statusCode;
        Category = category;
    }
}