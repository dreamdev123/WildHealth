using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Sms;
using WildHealth.Domain.Enums.Sms;

namespace WildHealth.Application.Services.SMS;

public interface ISmsConsentService
{
    public Task<SmsConsent?> GetAsync(string recipientPhoneNumber, string messagingServiceSid);
    public Task<SmsConsent?> GetAsync(Guid universalId, string recipientPhoneNumber, string messagingServiceSid);

    public Task<SmsConsent> CreateOrUpdateAsync(Guid phoneUserIdentity, string recipientPhoneNumber, string senderPhoneNumber,
        string messagingServiceSid, SmsConsentSetting setting, string integrationEventJson);
   
    public Task<SmsConsent> UpdateAsync(SmsConsent consent);
    
    public Task<SmsConsent> CreateAsync(Guid phoneUserIdentity, string recipientPhoneNumber, string messagingServiceSid, string senderPhoneNumber, SmsConsentSetting setting, string integrationEventJson);
}