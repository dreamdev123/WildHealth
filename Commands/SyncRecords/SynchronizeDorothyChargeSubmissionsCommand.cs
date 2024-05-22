using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class SynchronizeDorothyChargeSubmissionsCommand : IRequest
{
    public int ShardSize { get; }
    
    public int PracticeId { get; }
    
    public SynchronizeDorothyChargeSubmissionsCommand(int shardSize, int practiceId)
    {
        ShardSize = shardSize;
        PracticeId = practiceId;
    }
}