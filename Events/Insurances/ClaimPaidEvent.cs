using System;
using MediatR;

namespace WildHealth.Application.Events.Insurances;

public class ClaimPaidEvent : INotification
{
    public int ClaimId { get; }
    
    public int PracticeId { get; }
    
    public DateTime Date { get; }

    public ClaimPaidEvent(int claimId, int practiceId, DateTime date)
    {
        ClaimId = claimId;
        PracticeId = practiceId;
        Date = date;
    }
}