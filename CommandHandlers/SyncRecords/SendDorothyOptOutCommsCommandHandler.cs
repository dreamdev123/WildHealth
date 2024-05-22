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
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Infrastructure.EmailFactory.Models.SyncRecords;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class SendDorothyOptOutCommsCommandHandler : IRequestHandler<SendDorothyOptOutCommsCommand>
{
    private const string Subject = "Opt-out Confirmation";
    
    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IEmailFactory _emailFactory;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendDorothyOptOutCommsCommandHandler> _logger;
    private readonly IOptions<PracticeOptions> _options;

    public SendDorothyOptOutCommsCommandHandler(
        ISyncRecordsService syncRecordsService,
        IEmailFactory emailFactory,
        IEmailService emailService,
        ILogger<SendDorothyOptOutCommsCommandHandler> logger,
        IOptions<PracticeOptions> options)
    {
        _syncRecordsService = syncRecordsService;
        _emailFactory = emailFactory;
        _emailService = emailService;
        _logger = logger;
        _options = options;
    }

    public async Task Handle(SendDorothyOptOutCommsCommand command, CancellationToken cancellationToken)
    {
        var syncRecord = await _syncRecordsService.GetByUniversalId<SyncRecordDorothy>(command.SyncRecordUniversalId);

        if (syncRecord is null)
        {
            _logger.LogInformation($"Unable to find sync record dorothy for universal id = {command.SyncRecordUniversalId}");
            return;
        }

        await SendOptOutEmail(syncRecord);
    }

    #region private

    private async Task SendOptOutEmail(SyncRecordDorothy syncRecord)
    {
        var model = new EmailDataModel<DorothyOptOutEmailModel>
        {
            Data = new DorothyOptOutEmailModel(syncRecord.FirstName, syncRecord.SyncRecord.PracticeId, _options)
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

    #endregion
}