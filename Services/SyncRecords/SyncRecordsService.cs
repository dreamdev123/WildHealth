using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WildHealth.Application.Serializers;
using WildHealth.Common.Attributes;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.SyncRecords;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Domain.Exceptions;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.SyncRecords;

public class SyncRecordsService : ISyncRecordsService
{
    private readonly SyncRecordStatus[] _discardedStatuses = new[]
    {
        SyncRecordStatus.DorothyInvalidBirthdayDiscarded, 
        SyncRecordStatus.DorothyRequiresDiscoveryDiscarded,
        SyncRecordStatus.DorothyBlankZipCodeDiscarded, 
        SyncRecordStatus.DorothyDuplicateBirthdateZipCodeDiscarded,
        SyncRecordStatus.DorothyDuplicatePolicyIdDiscarded,
        SyncRecordStatus.DorothyInvalidAddressDiscarded,
        SyncRecordStatus.DorothyInvalidNameDiscarded,
        SyncRecordStatus.DorothyUnshippableAddressDiscarded
    };
    
    private readonly IGeneralRepository<SyncRecord> _syncRecordsRepository;
    private readonly IGeneralRepository<SyncData> _syncDatumRepository;
    private readonly ISyncRecordSerializer _syncRecordSerializer;


    public SyncRecordsService(IGeneralRepository<SyncRecord> syncRecordsRepository, IGeneralRepository<SyncData> syncDatumRepository, ISyncRecordSerializer syncRecordSerializer)
    {
        _syncRecordsRepository = syncRecordsRepository;
        _syncDatumRepository = syncDatumRepository;
        _syncRecordSerializer = syncRecordSerializer;
    }

    /// <summary>
    /// Returns sync records for the given type and status
    /// </summary>
    /// <param name="type"></param>
    /// <param name="statuses"></param>
    /// <param name="count"></param>
    /// <param name="isTracking"></param>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    public async Task<T[]> GetByTypeAndStatus<T>(
        SyncRecordType type, 
        SyncRecordStatus[] statuses, 
        int count,
        bool isTracking = true,
        int? practiceId = null) where T : ISyncRecordInstance
    {
        var query = _syncRecordsRepository
            .All()
            .Where(o => o.Type == type && statuses.Contains(o.Status))
            .Include(o => o.SyncDatum)
            .IncludeIntegrations<SyncRecord, SyncRecordIntegration>()
            .OrderBy(o => o.CreatedAt)
            .Take(count);

        if (practiceId.HasValue)
        {
            query = query.Where(o => o.PracticeId == practiceId);
        }
        
        if (!isTracking)
        {
            query = query.AsNoTracking();
        }

        var syncRecords = await query.ToArrayAsync();

        return syncRecords.Select(o => _syncRecordSerializer.Deserialize<T>(o)).ToArray();
    }

    /// <summary>
    /// Returns the specificc syncRecord
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T[]> GetByIdMultiple<T>(int id) where T : ISyncRecordInstance
    {
        var syncRecords = await _syncRecordsRepository
            .All()
            .Where(o => o.Id == id)
            .Include(o => o.SyncDatum)
            .ToArrayAsync();
        
        return syncRecords
            .Select(o => _syncRecordSerializer.Deserialize<T>(o)).ToArray();
    }

    /// <summary>
    /// Returns the sync record
    /// </summary>
    /// <param name="id"></param>
    /// <param name="includeDatum"></param>
    /// <returns></returns>
    public async Task<SyncRecord> GetById(long id, bool includeDatum = false)
    {
        var query = _syncRecordsRepository
            .All()
            .Where(o => o.Id == id);

        if (includeDatum)
        {
            query = query.Include(o => o.SyncDatum);
        }
        
        return await query.FirstAsync();
    }

    /// <summary>
    /// Returns the specific syncRecord
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T> GetById<T>(long id) where T : ISyncRecordInstance
    {
        var syncRecord = await _syncRecordsRepository
            .All()
            .Where(o => o.Id == id)
            .Include(o => o.SyncDatum)
            .FirstOrDefaultAsync();

        if (syncRecord is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Unable to locate a [SyncRecord] with [Id] = {id}");
        }
        
        return _syncRecordSerializer.Deserialize<T>(syncRecord);
    }

    public async Task<T> GetByUniversalId<T>(Guid universalId) where T : ISyncRecordInstance
    {
        var syncRecord = await _syncRecordsRepository
            .All()
            .Where(o => o.UniversalId == universalId)
            .Include(o => o.SyncDatum)
            .FirstOrDefaultAsync();

        if (syncRecord is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Unable to locate a [SyncRecord] with [UniversalId] = {universalId}");
        }
        
        return _syncRecordSerializer.Deserialize<T>(syncRecord); 
    }

    /// <summary>
    /// Get by id range and status
    /// </summary>
    /// <param name="idFrom"></param>
    /// <param name="idTo"></param>
    /// <param name="status"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T[]> GetByIdRange<T>(int idFrom, int idTo, SyncRecordStatus status) where T : ISyncRecordInstance
    {
        var syncRecords = await _syncRecordsRepository
            .All()
            .Where(o => o.Id >= idFrom && o.Id <= idTo)
            .Where(o => o.Status == status)
            .Include(o => o.SyncDatum)
            .ToArrayAsync();
        
        return syncRecords
            .Select(o => _syncRecordSerializer.Deserialize<T>(o)).ToArray();
    }
    
    /// <summary>
    /// Create sync record from the generic type
    /// </summary>
    /// <param name="syncRecordInstance"></param>
    /// <param name="practiceId"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public async Task<T> CreateAsync<T>(
        T syncRecordInstance, 
        SyncRecordType type,
        int practiceId,
        SyncRecordStatus status = SyncRecordStatus.PendingCleansing) where T : ISyncRecordInstance
    {
        var result = new SyncRecord(type, practiceId, status);
        
        foreach (var property in typeof(T).GetProperties()
                     .Where(prop => prop.IsDefined(typeof(SyncRecordProperty), false)))
        {
            var value = property.GetValue(syncRecordInstance);
            
            if (value is not null)
            {
                result.SyncDatum.Add(new SyncData(property.Name, Convert.ToString(value)));
            }
        }

        var record = await CreateAsync(result);

        return _syncRecordSerializer.Deserialize<T>(record);
    }

    /// <summary>
    /// Get a sync record based on the keys passed in
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="ignoreStatuses"></param>
    /// <param name="practiceId"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<T[]> GetByKeys<T>(IDictionary<string, string> keys,
        SyncRecordStatus[]? ignoreStatuses = null,
        int? practiceId = null) where T : ISyncRecordInstance
    {
        var instance = (T)Activator.CreateInstance(typeof(T))!;
        var type = instance.SyncRecordType;

        var query = _syncRecordsRepository
            .All()
            .Include(o => o.SyncDatum)
            .Where(o => o.Type == type);

        if (ignoreStatuses is not null)
        {
            query = query.Where(o => !ignoreStatuses.Contains(o.Status));
        }

        if (practiceId is not null)
        {
            query = query.Where(o => o.PracticeId == practiceId);
        }

        foreach (var kvp in keys)
        {
            if (kvp.Key == nameof(SyncData.SyncRecordId))
            {
                query = query.Where(o => o.Id == Convert.ToInt64(kvp.Value));

                continue;
            }

            query = query.Where(o => o.SyncDatum.Any(a => a.Key == kvp.Key && a.Value == kvp.Value));
        }

        var syncRecords = await query
            .ToArrayAsync();

        return syncRecords
            .Select(o => _syncRecordSerializer.Deserialize<T>(o)).ToArray();
    }
    
    /// <summary>
    /// Create sync record
    /// </summary>
    /// <param name="syncRecord"></param>
    /// <returns></returns>
    public async Task<SyncRecord> CreateAsync(SyncRecord syncRecord)
    {
        await _syncRecordsRepository.AddAsync(syncRecord);
        await _syncRecordsRepository.SaveAsync();

        return syncRecord;
    }

    /// <summary>
    /// Update sync record
    /// </summary>
    /// <param name="syncRecord"></param>
    /// <returns></returns>
    public async Task<SyncRecord> UpdateAsync(SyncRecord syncRecord)
    {
        _syncRecordsRepository.Edit(syncRecord);

        await _syncRecordsRepository.SaveAsync();

        return syncRecord;
    }

    /// <summary>
    /// Update sync record
    /// </summary>
    /// <param name="syncRecordInstance"></param>
    /// <returns></returns>
    public async Task<SyncRecord> UpdateAsync<T>(T syncRecordInstance) where T : ISyncRecordInstance
    {
        var result = await _syncRecordsRepository
            .Get(x => x.Id == syncRecordInstance.SyncRecord.Id)
            .Include(o => o.SyncDatum)
            .FirstOrDefaultAsync();


        if (result is null)
        {
            throw new DomainException($"Sync record not found for sync record instance with id: {syncRecordInstance.SyncRecord.Id}");
        }

        // Store all the specific properties
        _syncRecordSerializer.Serialize<T>(syncRecordInstance, result);
        
        // Handle generic properties
        result.Status = syncRecordInstance.SyncRecord.Status;

        _syncRecordsRepository.Edit(result);

        await _syncRecordsRepository.SaveAsync();

        return result;
    }

    /// <summary>
    /// <see cref="ISyncRecordsService.GetByDuplicatePolicyId"/>
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    public async Task<SyncRecordDorothy[]> GetByDuplicatePolicyId(SyncRecordDorothy record)
    {
        var results = await _syncRecordsRepository
            .All()
            .Where(o => o.Type == SyncRecordType.Dorothy &&
                        o.PracticeId == record.SyncRecord.PracticeId &&
                        !_discardedStatuses.Contains(o.Status) &&
                        o.Id != record.SyncRecord.Id)
            .Include(o => o.SyncDatum)
            .Where(o => o.SyncDatum.Any(x => 
                x.Key == nameof(SyncRecordDorothy.PolicyId) && 
                x.Value != null &&
                x.Value == record.PolicyId))
            .ToArrayAsync();
        

        return  results.Select(o => _syncRecordSerializer.Deserialize<SyncRecordDorothy>(o)).ToArray();
    }

    /// <summary>
    /// <see cref="ISyncRecordsService.GetByDuplicateBirthdayAndZip"/>
    /// </summary>
    /// <param name="record"></param>
    /// <returns></returns>
    public async Task<SyncRecordDorothy?> GetByDuplicateBirthdayZipFirstLast(SyncRecordDorothy record)
    {
        var result = await _syncRecordsRepository
            .All()
            .Where(o => o.Type == SyncRecordType.Dorothy && 
                        o.PracticeId == record.SyncRecord.PracticeId &&
                        !_discardedStatuses.Contains(o.Status) &&
                        o.Id != record.SyncRecord.Id)
            .Include(o => o.SyncDatum)
            .Where(o => 
                o.SyncDatum.Any(x => x.Key == nameof(SyncRecordDorothy.Birthday) && x.Value == record.Birthday) &&
                o.SyncDatum.Any(x => x.Key == nameof(SyncRecordDorothy.ZipCode) && x.Value == record.ZipCode) &&
                o.SyncDatum.Any(x => x.Key == nameof(SyncRecordDorothy.FirstName) && x.Value == record.FirstName) &&
                o.SyncDatum.Any(x => x.Key == nameof(SyncRecordDorothy.LastName) && x.Value == record.LastName))
            .FirstOrDefaultAsync();

        if (result is null)
        {
            return null;
        }

        return _syncRecordSerializer.Deserialize<SyncRecordDorothy>(result);
    }

    public async Task<SyncRecordDorothy[]> GetBillableDorothyRecords(DateTime billableDate, int practiceId)
    {
        var results = await _syncRecordsRepository
            .All()
            .Where(o => o.PracticeId == practiceId && o.Status == SyncRecordStatus.SyncComplete)
            .Include(o => o.Claims)
            .Include(o => o.SyncDatum)
            .Where(o =>
                // The sync record has never been billed a claim
                o.Claims.Count == 0 /*||
                // OR the sync record is subscription opt in and the last time it was billed was before billable date
                (
                    o.SyncDatum.Any(syncDatum => syncDatum.Key == nameof(SyncRecordDorothy.SubscriptionOptIn) && Convert.ToBoolean(syncDatum.Value)) &&
                    o.Claims.Max(claim => claim.CreatedAt.Date) <= billableDate.Date
                )*/)
            .ToArrayAsync();

        return results.Select(o => _syncRecordSerializer.Deserialize<SyncRecordDorothy>(o)).ToArray();
    }

    /// <summary>
    /// <see cref="ISyncRecordsService.GetRecordsWithDuplicatePolicyId"/>
    /// <param name="count"></param>
    /// </summary>
    /// <returns></returns>
    public async Task<IEnumerable<IGrouping<string, SyncRecord>>> GetRecordsWithDuplicatePolicyId(int count)
    {
        var results = await _syncRecordsRepository
            .All()
            .Include(o => o.SyncDatum)
            .Where(o =>
                o.Status == SyncRecordStatus.DorothyDuplicatePolicyId &&
                o.SyncDatum.Any(x =>
                    x.Key == nameof(SyncRecordDorothy.PolicyId) &&
                    !string.IsNullOrEmpty(x.Value)))
            .OrderBy(o => o.ModifiedAt)
            .Take(count)
            .ToArrayAsync();

        return results
            .GroupBy(o => o.SyncDatum.FirstOrDefault(x => x.Key == nameof(SyncRecordDorothy.PolicyId))?.Value!)
            .Where(o => o.Count() > 1);
    }

    /// <summary>
    /// Returns related records for the given record
    /// </summary>
    /// <param name="syncRecord"></param>
    /// <returns></returns>
    public async Task<SyncRecordDorothy[]> GetRelatedDorothyRecords(SyncRecordDorothy syncRecord)
    {
        var keys = AlikeKeysForRecord(syncRecord);
    
        // Not concerned with statuses that are discarded
        return (await GetByKeys<SyncRecordDorothy>(keys: keys, practiceId: syncRecord.SyncRecord.PracticeId))
            .Where(o => !_discardedStatuses.Contains(o.SyncRecord.Status)).ToArray();
    }

    private IDictionary<string, string> AlikeKeysForRecord(SyncRecordDorothy syncRecordDorothy)
    {
        switch (syncRecordDorothy.SyncRecord.Status)
        {
            case SyncRecordStatus.DorothyDuplicatePolicyId when syncRecordDorothy.PolicyId != null:
                return new Dictionary<string, string>()
                {
                    {nameof(SyncRecordDorothy.PolicyId), syncRecordDorothy.PolicyId}
                };
            
            case SyncRecordStatus.DorothyDuplicateBirthdateZipCodeFirstLast:
                return new Dictionary<string, string>()
                {
                    {nameof(SyncRecordDorothy.Birthday), syncRecordDorothy.Birthday},
                    {nameof(SyncRecordDorothy.ZipCode), syncRecordDorothy.ZipCode},
                    {nameof(SyncRecordDorothy.FirstName), syncRecordDorothy.FirstName},
                    {nameof(SyncRecordDorothy.LastName), syncRecordDorothy.LastName},
                };
            
            
            default:
                return new Dictionary<string, string>()
                {
                    {nameof(SyncData.SyncRecordId), syncRecordDorothy.SyncRecord.GetId().ToString()}
                };
                
        }
    }
    
    public async Task<T?> GetMostRecentRecordByDatum<T>(SyncRecordType type,
        SyncRecordStatus status,
        string key,
        int practiceId) where T : ISyncRecordInstance
    {
        var property = typeof(T).GetProperty(key);

        if (property is null)
        {
            throw new DomainException($"{key} does not exist on sync record type {type}");
        }
        
        var results = await _syncRecordsRepository
            .All()
            .Include(o => o.SyncDatum)
            .Where(o =>
                o.Type == type &&
                o.PracticeId == practiceId &&
                o.Status == status)
            .ToArrayAsync();

        var result = results
            .MaxBy(o =>
            {
                var value = o.SyncDatum.FirstOrDefault(x => x.Key == property.Name)?.Value;

                return value is not null
                    ? Convert.ChangeType(value, property.PropertyType)
                    : property.PropertyType.GetField("MinValue")?.GetValue(null);
            });

        if (result is null)
        {
            return default;
        }

        return _syncRecordSerializer.Deserialize<T>(result);
    }

    public async Task<T?> GetByIntegrationId<T>(string integrationId, IntegrationVendor vendor, string purpose) where T : ISyncRecordInstance
    {
        var result = await _syncRecordsRepository
            .All()
            .ByIntegrationId<SyncRecord, SyncRecordIntegration>(integrationId, vendor, purpose)
            .Include(o => o.SyncDatum)
            .FirstOrDefaultAsync();

        if (result is null)
        {
            return default;
        }
        
        return _syncRecordSerializer.Deserialize<T>(result);
    }
}