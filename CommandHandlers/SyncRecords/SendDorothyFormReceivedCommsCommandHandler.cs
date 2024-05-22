using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.SMS;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Infrastructure.EmailFactory.Models.SyncRecords;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class SendDorothyFormReceivedCommsCommandHandler : IRequestHandler<SendDorothyFormReceivedCommsCommand>
{
    private const string Subject = "Thank you for your request!";
    
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IEmailFactory _emailFactory;
    private readonly IEmailService _emailService;
    private readonly ISMSService _smsService;
    private readonly ILogger<SendDorothyFormReceivedCommsCommandHandler> _logger;
    private readonly IOptions<PracticeOptions> _options;

    public SendDorothyFormReceivedCommsCommandHandler(
        ISyncRecordsService syncRecordsService,
        IEmailFactory emailFactory,
        IEmailService emailService,
        ISMSService smsService,
        ILogger<SendDorothyFormReceivedCommsCommandHandler> logger,
        IOptions<PracticeOptions> options)
    {
        _syncRecordsService = syncRecordsService;
        _emailFactory = emailFactory;
        _emailService = emailService;
        _smsService = smsService;
        _logger = logger;
        _options = options;
    }

    public async Task Handle(SendDorothyFormReceivedCommsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Sending of dorothy form received comms for submission id = {command.SubmissionId} has: started");

        var keys = new Dictionary<string, string>
        {
            {nameof(SyncRecordDorothy.SubmissionId), command.SubmissionId}
        };

        var syncRecord = (await _syncRecordsService.GetByKeys<SyncRecordDorothy>(keys))?.FirstOrDefault();

        if (syncRecord is null)
        {
            _logger.LogInformation($"Unable to find sync record dorothy for submission id = {command.SubmissionId}");
            return;
        }

        if (syncRecord.SyncRecord.PracticeId != _options.Value.MurrayMedicalId)
        {
            _logger.LogInformation($"Invalid practice id = {syncRecord.SyncRecord.PracticeId} for form received comms");
            return;
        }

        await SendFormReceivedEmail(syncRecord);

        if (syncRecord.SmsOptIn)
        {
            await SendFormReceivedSms(syncRecord);
        }

        _logger.LogInformation($"Sending of dorothy form received comms for submission id = {command.SubmissionId} has: finished");
    }

    #region private

    private async Task SendFormReceivedEmail(SyncRecordDorothy syncRecord)
    {
        var model = new EmailDataModel<DorothyFormReceivedEmailModel>
        {
            Data = new DorothyFormReceivedEmailModel(syncRecord.FirstName, syncRecord.SyncRecord.PracticeId, _options)
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

    private async Task SendFormReceivedSms(SyncRecordDorothy syncRecord)
    {
        var organizationName = syncRecord.SyncRecord.PracticeId == _options.Value.MurrayMedicalId
            ? DorothyConstants.Emails.MurrayMedical.OrganizationName
            : DorothyConstants.Emails.MedicalTestingDirect.OrganizationName;
        var phoneNumber = syncRecord.SyncRecord.PracticeId == _options.Value.MurrayMedicalId
            ? DorothyConstants.Emails.MurrayMedical.PhoneNumber
            : DorothyConstants.Emails.MedicalTestingDirect.PhoneNumber;
        
        var smsMessage = $"Thank you for requesting COVID tests from {organizationName}!\n\nOur next step is to verify eligibility. Check your email for detailed updates.\n\nContact us at {phoneNumber}\nReply STOP to opt out. Msg&Data rates may apply.";

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