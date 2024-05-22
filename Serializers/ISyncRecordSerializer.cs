using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Entities.SyncRecords;

namespace WildHealth.Application.Serializers;

public interface ISyncRecordSerializer
{
    T Deserialize<T>(SyncRecord syncRecord) where T : ISyncRecordInstance;

    /// <summary>
    /// Convert data from specific instance to provided SyncRecord
    /// </summary>
    /// <param name="syncRecordInstance"></param>
    /// <param name="syncRecord"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    SyncRecord Serialize<T>(T syncRecordInstance, SyncRecord syncRecord) where T : ISyncRecordInstance;
}