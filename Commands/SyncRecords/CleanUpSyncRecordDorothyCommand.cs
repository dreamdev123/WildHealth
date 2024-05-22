using MediatR;
using WildHealth.Domain.Enums.SyncRecords;

namespace WildHealth.Application.Commands.SyncRecords;

public class CleanUpSyncRecordDorothyCommand : IRequest
{
    public SyncRecordStatus[] StatusesToClean { get; }
    
    public int NumberOfRecordsToClean { get; }
    
    public CleanUpSyncRecordDorothyCommand(SyncRecordStatus[] statusesToClean, int numberOfRecordsToClean)
    {
        NumberOfRecordsToClean = numberOfRecordsToClean;

        StatusesToClean = statusesToClean;
    }
}