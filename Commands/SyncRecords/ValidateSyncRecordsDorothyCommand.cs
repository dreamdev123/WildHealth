using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class ValidateSyncRecordsDorothyCommand : IRequest
{
    public int NumberOfRecordsToValidate { get; set; }
    
    public int ShardSize { get; set; }
    
    public ValidateSyncRecordsDorothyCommand(int numberOfRecordsToValidate, int shardSize)
    {
        NumberOfRecordsToValidate = numberOfRecordsToValidate;
        ShardSize = shardSize;
    }
}