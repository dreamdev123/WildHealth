using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Entities.SyncRecords;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.SyncRecords;

namespace WildHealth.Application.Services.SyncRecords;

public interface ISyncRecordsService
{
    /// <summary>
    /// Returns the specificc syncRecord
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T[]> GetByIdMultiple<T>(int id) where T : ISyncRecordInstance;

    /// <summary>
    /// Returns the sync record
    /// </summary>
    /// <param name="id"></param>
    /// <param name="includeDatum"></param>
    /// <returns></returns>
    Task<SyncRecord> GetById(long id, bool includeDatum = false);

    /// <summary>
    /// Returns the specific syncRecord
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> GetById<T>(long id) where T : ISyncRecordInstance;

    /// <summary>
    /// Returns the sync record by universal id
    /// </summary>
    /// <param name="universalId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T> GetByUniversalId<T>(Guid universalId) where T : ISyncRecordInstance;
    
    /// <summary>
    /// Get by id range and status
    /// </summary>
    /// <param name="idFrom"></param>
    /// <param name="idTo"></param>
    /// <param name="status"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T[]> GetByIdRange<T>(int idFrom, int idTo, SyncRecordStatus status) where T : ISyncRecordInstance;
    
    /// <summary>
    /// Returns sync records for the given type and status
    /// </summary>
    /// <param name="type"></param>
    /// <param name="statuses"></param>
    /// <param name="count"></param>
    /// <param name="isTracking"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<T[]> GetByTypeAndStatus<T>(
        SyncRecordType type, 
        SyncRecordStatus[] statuses, 
        int count, 
        bool isTracking = true,
        int? practiceId = null) where T : ISyncRecordInstance;

    /// <summary>
    /// Get a sync record based on the keys passed in
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="ignoreStatuses"></param>
    /// <param name="practiceId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T[]> GetByKeys<T>(IDictionary<string, string> keys,
        SyncRecordStatus[]? ignoreStatuses = null,
        int? practiceId = null) where T : ISyncRecordInstance;
    
    /// <summary>
    /// Create sync record from the generic type
    /// </summary>
    /// <param name="syncRecord"></param>
    /// <param name="type"></param>
    /// <param name="status"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<T> CreateAsync<T>(
        T syncRecord, 
        SyncRecordType type,
        int practiceId,
        SyncRecordStatus status = SyncRecordStatus.PendingCleansing) where T : ISyncRecordInstance;

    /// <summary>
    /// Create sync record
    /// </summary>
    /// <param name="syncRecord"></param>
    /// <returns></returns>
    Task<SyncRecord> CreateAsync(SyncRecord syncRecord);
    
    /// <summary>
    /// Update sync record
    /// </summary>
    /// <param name="syncRecordInstance"></param>
    /// <returns></returns>
    Task<SyncRecord> UpdateAsync<T>(T syncRecordInstance) where T : ISyncRecordInstance;

    /// <summary>
    /// Update sync record
    /// </summary>
    /// <param name="syncRecord"></param>
    /// <returns></returns>
    Task<SyncRecord> UpdateAsync(SyncRecord syncRecord);

    /// <summary>
    /// Returns sync record dorothy with the same policy id
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    Task<SyncRecordDorothy[]> GetByDuplicatePolicyId(SyncRecordDorothy record);

    /// <summary>
    /// Returns sync record dorothy with the same birthday an zip
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    Task<SyncRecordDorothy?> GetByDuplicateBirthdayZipFirstLast(SyncRecordDorothy record);

    /// <summary>
    /// Returns sync records that have duplicate policy ids
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    Task<IEnumerable<IGrouping<string, SyncRecord>>> GetRecordsWithDuplicatePolicyId(int count);

    /// <summary>
    /// Returns related records for the given record
    /// </summary>
    /// <param name="syncRecord"></param>
    /// <returns></returns>
    Task<SyncRecordDorothy[]> GetRelatedDorothyRecords(SyncRecordDorothy syncRecord);

    /// <summary>
    /// Returns the most recent record by datum
    /// </summary>
    /// <param name="type"></param>
    /// <param name="status"></param>
    /// <param name="key"></param>
    /// <param name="practiceId"></param>
    /// <param name="statuses"></param>
    /// <returns></returns>
    Task<T?> GetMostRecentRecordByDatum<T>(SyncRecordType type,
        SyncRecordStatus status,
        string key,
        int practiceId) where T : ISyncRecordInstance;

    /// <summary>
    /// Gets sync record by integration id
    /// </summary>
    /// <param name="integrationId"></param>
    /// <param name="vendor"></param>
    /// <param name="purpose"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    Task<T?> GetByIntegrationId<T>(string integrationId, IntegrationVendor vendor, string purpose) where T : ISyncRecordInstance;

    /// <summary>
    /// Gets billable dorothy records
    /// </summary>
    /// <param name="billableDate"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<SyncRecordDorothy[]> GetBillableDorothyRecords(DateTime billableDate, int practiceId);
}