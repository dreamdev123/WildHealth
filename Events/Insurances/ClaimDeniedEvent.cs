using System;
using MediatR;

namespace WildHealth.Application.Events.Insurances;

public class ClaimDeniedEvent : INotification
{
    public int ClaimId { get; }
    
    public int PracticeId { get; }
    
    public DateTime Date { get; }

    public ClaimDeniedEvent(int claimId, int practiceId, DateTime date)
    {
        ClaimId = claimId;
        PracticeId = practiceId;
        Date = date;
    }
}