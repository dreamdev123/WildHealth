using System;
using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class SendDorothyOptOutCommsCommand : IRequest
{
    public Guid SyncRecordUniversalId { get; }

    public SendDorothyOptOutCommsCommand(Guid syncRecordUniversalId)
    {
        SyncRecordUniversalId = syncRecordUniversalId;
    }
}