using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Sms;
using WildHealth.Application.Services.SMS;
using WildHealth.Domain.Entities.Sms;
using WildHealth.Domain.Enums.Sms;

namespace WildHealth.Application.CommandHandlers.Sms;

public class ProvideSmsConsentCommandHandler : IRequestHandler<ProvideSmsConsentCommand, SmsConsent>
{
    private readonly ISmsConsentService _consentService;
    private readonly ILogger<ProvideSmsConsentCommandHandler> _logger;

    public ProvideSmsConsentCommandHandler(
        ISmsConsentService consentService,
        ILogger<ProvideSmsConsentCommandHandler> logger)
    {
        _consentService = consentService;
        _logger = logger;
    }

    public async Task<SmsConsent> Handle(ProvideSmsConsentCommand request, CancellationToken cancellationToken)
    {
        var consent = await _consentService.CreateOrUpdateAsync(request.PhoneUserIdentity, 
                                                                request.RecipientPhoneNumber, 
                                                                request.SenderPhoneNumber,
                                                                request.MessagingServiceSid, 
                                                                SmsConsentSetting.Allow,
                                                                request.IntegrationEventJson);
        return consent;
    }
}