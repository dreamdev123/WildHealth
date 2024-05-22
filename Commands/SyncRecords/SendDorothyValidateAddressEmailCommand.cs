using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class SendDorothyValidateAddressEmailCommand : IRequest
{
    public int SyncRecordId { get; }

    public SendDorothyValidateAddressEmailCommand(int syncRecordId)
    {
        SyncRecordId = syncRecordId;
    }
}