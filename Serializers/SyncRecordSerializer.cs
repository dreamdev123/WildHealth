using System;
using System.Linq;
using WildHealth.Common.Attributes;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Entities.SyncRecords;

namespace WildHealth.Application.Serializers;

public class SyncRecordSerializer : ISyncRecordSerializer
{
    public T Deserialize<T>(SyncRecord syncRecord) where T : ISyncRecordInstance
    {
        var instance = (T)Activator.CreateInstance(typeof(T))!;

        foreach (var property in typeof(T).GetProperties()
                     .Where(prop => prop.IsDefined(typeof(SyncRecordProperty), false)))
        {
            var value = syncRecord.SyncDatum.Where(o => o.Key == property.Name).FirstOrDefault()?.Value;

            if (value is not null)
            {
                property.SetValue(instance, Convert.ChangeType(value, property.PropertyType));
            }
        }

        instance.SyncRecord = syncRecord;

        return instance;
    }
    
    /// <summary>
    /// Convert data from specific instance to provided SyncRecord
    /// </summary>
    /// <param name="syncRecordInstance"></param>
    /// <param name="syncRecord"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public SyncRecord Serialize<T>(T syncRecordInstance, SyncRecord syncRecord) where T : ISyncRecordInstance
    {
        foreach (var property in typeof(T).GetProperties()
                     .Where(prop => prop.IsDefined(typeof(SyncRecordProperty), false)))
        {
            var value = Convert.ToString(property.GetValue(syncRecordInstance));

            var didUpdate = false;
            foreach (var data in syncRecord.SyncDatum.Where(o => o.Key == property.Name))
            {
                data.Value = value;
                didUpdate = true;
            }

            if (!didUpdate && !string.IsNullOrEmpty(value))
            {
                syncRecord.SyncDatum.Add(new SyncData(property.Name, value));
            }
            
        }

        return syncRecord;
    }
}