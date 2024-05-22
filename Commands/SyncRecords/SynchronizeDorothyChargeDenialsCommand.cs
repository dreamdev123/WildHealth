using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class SynchronizeDorothyChargeDenialsCommand : IRequest
{
    public int PracticeId { get; }
    
    public SynchronizeDorothyChargeDenialsCommand(int practiceId)
    {
        PracticeId = practiceId;
    }
}