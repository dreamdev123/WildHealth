using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Infrastructure.EmailFactory.Models.SyncRecords;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class SendDorothyChargeSubmittedCommsCommandHandler : IRequestHandler<SendDorothyChargeSubmittedCommsCommand>
{
    private const string Subject = "Your claim has successfully been submitted!";
    
    private readonly IEmailService _emailService;
    private readonly IEmailFactory _emailFactory;
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly ILogger<SendDorothyChargeSubmittedCommsCommandHandler> _logger;
    private readonly IOptions<PracticeOptions> _options;

    public SendDorothyChargeSubmittedCommsCommandHandler(
        IEmailService emailService,
        IEmailFactory emailFactory,
        ISyncRecordsService syncRecordsService,
        ILogger<SendDorothyChargeSubmittedCommsCommandHandler> logger,
        IOptions<PracticeOptions> options)
    {
        _emailService = emailService;
        _emailFactory = emailFactory;
        _syncRecordsService = syncRecordsService;
        _logger = logger;
        _options = options;
    }

    public async Task Handle(SendDorothyChargeSubmittedCommsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Sending of dorothy charge submitted id = {command.EncounterId} comms has: started");
        
        var keys = new Dictionary<string, string>()
        {
            {nameof(SyncRecordFhirCharge.EncounterId), command.EncounterId}
        };
        
        var syncRecordFhirCharges = await _syncRecordsService.GetByKeys<SyncRecordFhirCharge>(keys);
        var charge = syncRecordFhirCharges.FirstOrDefault();

        if (charge is null)
        {
            _logger.LogError($"Sending of dorothy charge submitted id = {command.EncounterId} comms has: failed to find a charge");
            return;
        }

        var fhirPatientId = charge.PatientId;
        var syncRecordDorothy = await _syncRecordsService.GetByIntegrationId<SyncRecordDorothy>(
            fhirPatientId.ToString(),
            command.Vendor, 
            IntegrationPurposes.SyncRecord.ExternalId);

        if (syncRecordDorothy is null)
        {
            _logger.LogError($"Sending of dorothy charge submitted id = {command.EncounterId} comms has: failed to find a patient");
            return;
        }
        
        var model = new EmailDataModel<DorothyChargeSubmittedEmailModel>
        {
            Data = new DorothyChargeSubmittedEmailModel(syncRecordDorothy.FirstName, syncRecordDorothy.SyncRecord.PracticeId, _options)
        };

        var email = await _emailFactory.Create(model);

        await _emailService.SendAsync(
            to: syncRecordDorothy.Email,
            subject: Subject,
            body: email.Html,
            practiceId: syncRecordDorothy.SyncRecord.PracticeId,
            fromEmail: model.Data.PracticeEmail,
            fromName: model.Data.PracticeName);
        
        _logger.LogInformation($"Sending of dorothy charge submitted id = {command.EncounterId} comms has: finished");
    }
}