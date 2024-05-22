using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Sms;
using WildHealth.Domain.Enums.Sms;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.SMS;

public class SmsConsentService : ISmsConsentService
{
    private readonly IGeneralRepository<SmsConsent> _smsRepository;
    private readonly ILogger<SmsConsentService> _logger;

    public SmsConsentService(
        IGeneralRepository<SmsConsent> smsRepository,
        ILogger<SmsConsentService> logger): base()
    {
        _smsRepository = smsRepository;
        _logger = logger;
    }

    public async Task<SmsConsent?> GetAsync(string recipientPhoneNumber, string messagingServiceSid)
    {
        var matches = await _smsRepository.All().Where(stat =>
            stat.RecipientPhoneNumber.Equals(recipientPhoneNumber) &&
            stat.MessagingServiceSid.Equals(messagingServiceSid)
        ).ToListAsync();

        if (!matches.Any())
        {
            return null;
        }
        
        if (matches.Count() > 1)
        {
            _logger.LogWarning($"Multiple consent entries match {recipientPhoneNumber} and service {messagingServiceSid}.");
        }
    
        var first = matches.FirstOrDefault();
        return first;
    }

    public async Task<SmsConsent?> GetAsync(Guid universalId, string recipientPhoneNumber, string messagingServiceSid)
    {
        var matches = await _smsRepository.All().Where(stat =>
            stat.PhoneUserIdentity.Equals(universalId) &&
            stat.RecipientPhoneNumber.Equals(recipientPhoneNumber) &&
            stat.MessagingServiceSid.Equals(messagingServiceSid)
        ).ToListAsync();
            
        _logger.LogInformation($"Searching for SmsConsent entries for id {universalId} phone {recipientPhoneNumber} and sid {messagingServiceSid}.");

        if (matches.Count() == 0)
        {
            var message = $"There are no SmsConsent entries for id {universalId} phone {recipientPhoneNumber} and sid {messagingServiceSid}.";
            _logger.LogInformation(message);
            return null;
        }
        
        if (matches.Count() > 1)
        {
            var message = $"There are multiple SmsConsent entries for id {universalId} phone {recipientPhoneNumber} and sid {messagingServiceSid}.";
            _logger.LogError(message);
            throw new AppException(HttpStatusCode.Conflict, message);
        }
        
        return matches.ElementAt(0);
    }


    public async Task<SmsConsent> CreateOrUpdateAsync(Guid phoneUserIdentity,
        string recipientPhoneNumber,
        string senderPhoneNumber,
        string messagingServiceSid,
        SmsConsentSetting setting,
        string integrationEventJson)
    {
        var consent = await GetAsync(recipientPhoneNumber, messagingServiceSid);
        
        if (consent == null)
        {
            return await CreateAsync(phoneUserIdentity, recipientPhoneNumber, senderPhoneNumber, messagingServiceSid, setting, integrationEventJson);
        }

        if (consent.Setting == setting)
        {
            //There is no change.
            return consent;
        }
        
        consent.Setting = setting;
        return await UpdateAsync(consent);
    }

    public async Task<SmsConsent> UpdateAsync(SmsConsent consent)
    {
        _smsRepository.Edit(consent);
        await _smsRepository.SaveAsync();
        return consent;
    }

    public async Task<SmsConsent> CreateAsync(Guid phoneUserIdentity,
                                              string recipientPhoneNumber,
                                              string senderPhoneNumber,
                                              string messagingServiceSid,
                                              SmsConsentSetting setting,
                                              string integrationEventJson)
    {
        var consent = new SmsConsent()
        {
            PhoneUserIdentity = phoneUserIdentity,
            RecipientPhoneNumber = recipientPhoneNumber,
            SenderPhoneNumber = senderPhoneNumber,
            MessagingServiceSid = messagingServiceSid,
            Setting = setting,
            IntegrationEventJson = integrationEventJson
        };
        await _smsRepository.AddAsync(consent);
        await _smsRepository.SaveAsync();
        return consent;
    }
}