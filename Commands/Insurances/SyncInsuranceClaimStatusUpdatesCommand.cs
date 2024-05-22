using MediatR;

namespace WildHealth.Application.Commands.Insurances;

public class SyncInsuranceClaimStatusUpdatesCommand : IRequest
{
    public int PracticeId { get; set; }
    
    public SyncInsuranceClaimStatusUpdatesCommand(int practiceId)
    {
        PracticeId = practiceId;
    }
}