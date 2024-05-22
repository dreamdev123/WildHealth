using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using WildHealth.Application.Commands.Sms;
using WildHealth.Application.Services.SMS;
using WildHealth.Domain.Entities.Sms;
using WildHealth.Domain.Enums.Sms;

namespace WildHealth.Application.CommandHandlers.Sms;
public class RevokeSmsConsentCommandHandler : IRequestHandler<RevokeSmsConsentCommand, SmsConsent>
{
    private readonly ISmsConsentService _consentService;
    private readonly ILogger<RevokeSmsConsentCommandHandler> _logger;

    public RevokeSmsConsentCommandHandler(
        ISmsConsentService consentService,
        ILogger<RevokeSmsConsentCommandHandler> logger)
    {
        _consentService = consentService;
        _logger = logger;
    }
    public async Task<SmsConsent> Handle(RevokeSmsConsentCommand request, CancellationToken cancellationToken)
    {
        var consent = await _consentService.CreateOrUpdateAsync(request.PhoneUserIdentity, 
                                                                request.RecipientPhoneNumber, 
                                                                request.SenderPhoneNumber,
                                                                request.MessagingServiceSid, 
                                                                SmsConsentSetting.Disallow,
                                                                request.IntegrationEventJson);
        return consent;
    }
}