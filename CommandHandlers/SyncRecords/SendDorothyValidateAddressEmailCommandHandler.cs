using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Application.Services.Emails;
using WildHealth.Application.Services.States;
using WildHealth.Application.Services.SyncRecords;
using WildHealth.Common.Models.SyncRecords;
using WildHealth.Common.Options;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.SyncRecords;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Infrastructure.EmailFactory.Models.SyncRecords;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.SyncRecords;

public class SendDorothyValidateAddressEmailCommandHandler : IRequestHandler<SendDorothyValidateAddressEmailCommand>
{
    private const string Subject = "[UPDATE] Issue with your shipping address.";

    private readonly ISyncRecordsService _syncRecordsService;
    private readonly IEmailService _emailService;
    private readonly IEmailFactory _emailFactory;
    private readonly IStatesService _statesService;
    private readonly ILogger<SendDorothyValidateAddressEmailCommandHandler> _logger;
    private readonly IOptions<PracticeOptions> _options;

    public SendDorothyValidateAddressEmailCommandHandler(
        ISyncRecordsService syncRecordsService,
        IEmailService emailService,
        IEmailFactory emailFactory,
        IStatesService statesService,
        ILogger<SendDorothyValidateAddressEmailCommandHandler> logger,
        IOptions<PracticeOptions> options)
    {
        _syncRecordsService = syncRecordsService;
        _emailService = emailService;
        _emailFactory = emailFactory;
        _statesService = statesService;
        _logger = logger;
        _options = options;
    }

    public async Task Handle(SendDorothyValidateAddressEmailCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Send dorothy validate address email for record id = {command.SyncRecordId} has: started");

        var syncRecord = await _syncRecordsService.GetById<SyncRecordDorothy>(command.SyncRecordId);

        if (syncRecord.SyncRecord.Status != SyncRecordStatus.DorothyUnshippableAddress)
        {
            throw new AppException(HttpStatusCode.BadRequest,
                "Sync record is not a valid state for a validate address email.");
        }

        var state = await _statesService.GetByName(syncRecord.State);

        var model = new EmailDataModel<DorothyValidateAddressEmailModel>
        {
            Data = new DorothyValidateAddressEmailModel(
                patientFirstName: syncRecord.FirstName,
                patientLastName: syncRecord.LastName,
                submissionDate: syncRecord.SyncRecord.CreatedAt.ToString("d"),
                streetAddress1: syncRecord.StreetAddress1,
                streetAddress2: syncRecord.StreetAddress2,
                city: syncRecord.City,
                state: state.Abbreviation,
                zipCode: syncRecord.ZipCode,
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
        
        syncRecord.SetStatus(SyncRecordStatus.DorothyUnshippableAddressContacted);

        await _syncRecordsService.UpdateAsync(syncRecord);
        
        _logger.LogInformation($"Send dorothy validate address email for record id = {command.SyncRecordId} has: finished");
    }
}