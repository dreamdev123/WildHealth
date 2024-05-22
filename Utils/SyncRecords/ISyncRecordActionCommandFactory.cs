using MediatR;
using WildHealth.Domain.Enums.SyncRecords;

namespace WildHealth.Application.Utils.SyncRecords;

public interface ISyncRecordActionCommandFactory
{
    /// <summary>
    /// Create a sync record action command
    /// </summary>
    /// <param name="action"></param>
    /// <param name="syncRecordId"></param>
    /// <returns></returns>
    IRequest Create(SyncRecordAction action, int syncRecordId);
}