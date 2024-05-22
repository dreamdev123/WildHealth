using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.PhoneMetadata;

namespace WildHealth.Application.Services.PhoneLookupRecords;

public interface IPhoneLookupRecordService
{
    public Task<PhoneLookupRecord?> GetLookupAsync(Guid universalId, string phoneNumber, string countryCode = "US");
    public Task<PhoneLookupRecord> GetOrCreateLookupAsync(Guid universalId, string phoneNumber, string countryCode = "US");
}