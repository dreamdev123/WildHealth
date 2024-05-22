using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Application.Services.SMS;
using WildHealth.Domain.Entities.PhoneMetadata;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using WildHealth.Twilio.Clients.Exceptions;
using WildHealth.Twilio.Clients.Models.Lookup;

namespace WildHealth.Application.Services.PhoneLookupRecords;

public class PhoneLookupRecordService : IPhoneLookupRecordService 
{
    private readonly IGeneralRepository<PhoneLookupRecord> _phoneLookupRecordRepository;
    private readonly ISMSLookupService _lookupService;
    private readonly ILogger<PhoneLookupRecordService> _logger;

    public PhoneLookupRecordService(
        IGeneralRepository<PhoneLookupRecord> phoneLookupRecordRepository,
        ISMSLookupService lookupService,
        ILogger<PhoneLookupRecordService> logger
    )
    {
        _phoneLookupRecordRepository = phoneLookupRecordRepository;
        _lookupService = lookupService;
        _logger = logger;
    }

    /// <summary>
    /// This implementation actually performs the twilio Lookup call, and then calls
    /// CreateOrUpdateAsync with the result.
    /// </summary>
    /// <param name="universalId">the universal id, or other identity guid</param>
    /// <param name="phoneNumber">the phone number to search</param>
    /// <param name="countryCode">the country code, defaults to US</param>
    /// <returns></returns>
    private async Task<PhoneLookupRecord> CreateOrUpdateWithLookupAsync(Guid universalId, string phoneNumber, string countryCode = "US")
    {
        LookupResponseModel lookupResponse = await DoLookupAsync(phoneNumber);
        if (lookupResponse == null)
        {
            return await CreateUnknownRecordAsync(universalId, phoneNumber);
        }
        var lookupRecord = await CreateOrUpdateAsync(universalId, phoneNumber, lookupResponse);
        return lookupRecord;
    }

    private async Task<PhoneLookupRecord> CreateUnknownRecordAsync(Guid universalId, string phoneNumber)
    {
        var notFound = new PhoneLookupRecord()
        {
            PhoneUserIdentity = universalId,
            InputPhoneNumber = phoneNumber,
            ApiResult = "{}",
            Type = "unknown",
        };
        await _phoneLookupRecordRepository.AddAsync(notFound);
        await _phoneLookupRecordRepository.SaveAsync();
        return notFound;
    }

    public async Task<PhoneLookupRecord?> GetLookupAsync(Guid universalId, string phoneNumber, string countryCode = "US")
    {
        var result = await _phoneLookupRecordRepository.All().Where(lur => 
            lur.PhoneUserIdentity.Equals(universalId) &&
            (lur.E164PhoneNumber.Equals(phoneNumber) 
            || 
             lur.InputPhoneNumber.Equals(phoneNumber)
            || 
             lur.NationalFormatPhoneNumber.Equals(phoneNumber))).ToListAsync();
        
        if (result.Count == 0)
        {
            return null;
        }

        if (result.Count == 1)
        {
            return result.ElementAt(0);
        }
        
        throw new AppException(HttpStatusCode.Conflict,
            $"There are multiple lookup records for user {universalId} with the same e164 number.");
    }

    public async Task<PhoneLookupRecord> GetOrCreateLookupAsync(Guid universalId, string phoneNumber, string countryCode = "US")
    {
        var result = await GetLookupAsync(universalId, phoneNumber, countryCode);
        if (result == null)
        {
            return await CreateOrUpdateWithLookupAsync(universalId, phoneNumber, countryCode);
        }

        return result;
    }

    /// <summary>
    /// Creates or Updates the data with the given parameters.  It does not perform the twilio Lookup API call.
    /// </summary>
    /// <param name="universalId"></param>
    /// <param name="phoneNumber"></param>
    /// <param name="lookup"></param>
    /// <returns></returns>
    /// <exception cref="AppException"></exception>
    private async Task<PhoneLookupRecord> CreateOrUpdateAsync(Guid universalId, string phoneNumber, LookupResponseModel lookup)
    {
        var result = _phoneLookupRecordRepository.All().Where(lur => 
            lur.PhoneUserIdentity.Equals(universalId) &&
            lur.E164PhoneNumber.Equals(lookup.PhoneNumber)).ToList();
        
        if (result.Count() == 0)
        {
            var entity = new PhoneLookupRecord()
            {
                PhoneUserIdentity = universalId,
                Type = lookup.Carrier.Type,
                InputPhoneNumber = phoneNumber,
                E164PhoneNumber = lookup.PhoneNumber,
                NationalFormatPhoneNumber = lookup.NationalFormat,
                CountryCode = lookup.CountryCode,
                ApiResult = JsonConvert.SerializeObject(lookup)
            };
            await _phoneLookupRecordRepository.AddAsync(entity);
            await _phoneLookupRecordRepository.SaveAsync();
            return entity;
        }

        if (result.Count() == 1)
        {
            var entity = result.ElementAt(0);
            entity.PhoneUserIdentity = universalId;
            entity.Type = lookup.Carrier.Type;
            entity.InputPhoneNumber = phoneNumber;
            entity.E164PhoneNumber = lookup.PhoneNumber;
            entity.NationalFormatPhoneNumber = lookup.NationalFormat;
            entity.CountryCode = lookup.CountryCode;
            entity.ApiResult = JsonConvert.SerializeObject(lookup);
                
            await _phoneLookupRecordRepository.AddAsync(entity);
            await _phoneLookupRecordRepository.SaveAsync();
            return entity;
        }

        throw new AppException(HttpStatusCode.Conflict,
            $"There are multiple lookup records for user {universalId} with the same e164 number.");
    }

    private async Task<LookupResponseModel> DoLookupAsync(string phoneNumber, string countryCode = "US")
    {
        var result = await DoLookupE164Async(phoneNumber);
        if (result == null)
        {
            result = await DoLookupNationalFormatAsync(phoneNumber, countryCode);
        }
        return result!;
    }

    private async Task<LookupResponseModel?> DoLookupE164Async(string phoneNumber)
    {
        try
        {
            return await _lookupService.LookupE164Async(phoneNumber);
        }
        catch (TwilioException te)
        {
            if (te.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw te;
        }
        catch (Exception e)
        {
            var message = $"Could not look up {phoneNumber} as E164 format: {e}";
            _logger.LogError(message);
            throw new AppException(HttpStatusCode.BadRequest, message, e);
        }
    }

    private async Task<LookupResponseModel?> DoLookupNationalFormatAsync(string phoneNumber, string countryCode)
    {
        try
        {
            return await _lookupService.LookupNationalFormatAsync(phoneNumber, countryCode);
        }
        catch (TwilioException te)
        {
            if (te.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            throw te;
        }
        catch (Exception e)
        {
            var message = $"Could not look up {phoneNumber} as national format: {e}";
            _logger.LogError(message);
            throw new AppException(HttpStatusCode.BadRequest, message, e);
        }
    }
}
