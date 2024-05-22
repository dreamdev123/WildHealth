using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.Insurances;
using WildHealth.Application.Services.SMS;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Infrastructure.EmailFactory.Models.SyncRecords;
using WildHealth.IntegrationEvents._Base;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class SendDorothyClaimSubmittedCommsCommandHandler : IRequestHandler<SendDorothyClaimSubmittedCommsCommand>
{
    private const string Subject = "We’ve submitted your claim";
    
    private readonly IClaimsService _claimsService;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IEmailService _emailService;
    private readonly IEmailFactory _emailFactory;
    private readonly ISMSService _smsService;
    private readonly ILogger<SendDorothyClaimSubmittedCommsCommandHandler> _logger;
    private readonly IOptions<PracticeOptions> _options;

    public SendDorothyClaimSubmittedCommsCommandHandler(
        IClaimsService claimsService,
        ISyncRecordsService syncRecordsService,
        IEmailService emailService,
        IEmailFactory emailFactory,
        ISMSService smsService,
        ILogger<SendDorothyClaimSubmittedCommsCommandHandler> logger,
        IOptions<PracticeOptions> options)
    {
        _claimsService = claimsService;
        _syncRecordsService = syncRecordsService;
        _emailService = emailService;
        _emailFactory = emailFactory;
        _smsService = smsService;
        _logger = logger;
        _options = options;
    }

    public async Task Handle(SendDorothyClaimSubmittedCommsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Sending of comms for dorothy claim submitted for id = {command.ClaimId} has: started");

        var claim = await _claimsService.GetById(command.ClaimId);

        if (claim is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Unable to locate a [Claim] with [Id] = {command.ClaimId}");
        }

        if (claim.ClaimantSyncRecord is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Unable to locate a [SyncRecord] associated with [UniversalId] = {claim.ClaimantUniversalId}");
        }
        
        var syncRecord = await _syncRecordsService.GetById<SyncRecordDorothy>(claim.ClaimantSyncRecord.GetId());

        await SendClaimSubmittedEmail(syncRecord, claim.Procedure.Units);
        
        if (syncRecord.SmsOptIn)
        {
            await SendClaimSubmittedSms(syncRecord, claim.Procedure.Units);
        }

        _logger.LogInformation($"Sending of comms for dorothy claim submitted for id = {command.ClaimId} has: finished");
    }

    #region private

    
    private async Task SendClaimSubmittedEmail(SyncRecordDorothy syncRecord, int numberOfTest)
    {
        var model = new EmailDataModel<DorothyClaimSubmittedEmailModel>
        {
            Data = new DorothyClaimSubmittedEmailModel(
                patientFirstName: syncRecord.FirstName,
                numberOfTest: numberOfTest, 
                practiceId: syncRecord.SyncRecord.PracticeId,
                options: _options)
        };

        var email = await _emailFactory.Create(model);

        await _emailService.SendAsync(
            to: syncRecord.Email,
            subject: Subject,
            body: email.Html,
            practiceId: syncRecord.SyncRecord.PracticeId,
            fromEmail: model.Data.PracticeEmail,
            fromName: model.Data.PracticeName);
    }
    
    private async Task SendClaimSubmittedSms(SyncRecordDorothy syncRecord, int numberOfTest)
    {
        if (syncRecord.SyncRecord.PracticeId != _options.Value.MurrayMedicalId)
        {
            throw new AppException(HttpStatusCode.BadRequest,
                $"Dorothy sms messaging for practice id = {syncRecord.SyncRecord.PracticeId} is not supported");
        }
        
        var smsMessage = $"Hi {syncRecord.FirstName},\n\nYour claim for {numberOfTest} OTC COVID-19 tests has been successfully submitted!\n\nIf you need assistance, contact us at {DorothyConstants.Emails.MurrayMedical.PhoneNumber}\nReply STOP to opt out. Msg&Data rates may apply";

        await _smsService.SendAsync(
            messagingServiceSidType: SettingsNames.Twilio.MessagingServiceSid,
            to: syncRecord.PhoneNumber,
            body: smsMessage,
            practiceId: syncRecord.SyncRecord.PracticeId,
            universalId: syncRecord.SyncRecord.UniversalId.ToString(),
            avoidflag: null,
            sendAt: null);
    }

    #endregion
}