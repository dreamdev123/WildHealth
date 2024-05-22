using MediatR;
using WildHealth.Application.Commands.SyncRecords;
using WildHealth.Domain.Enums.SyncRecords;
using System;

namespace WildHealth.Application.Utils.SyncRecords;

public class SyncRecordActionCommandFactory : ISyncRecordActionCommandFactory
{
    public SyncRecordActionCommandFactory() { }


    public IRequest Create(SyncRecordAction action, int syncRecordId)
    {
        return action switch
        {
            SyncRecordAction.DorothySendValidateAddressEmail => new SendDorothyValidateAddressEmailCommand(syncRecordId),
            SyncRecordAction.DorothySubscriptionOptOut => new OptOutOfDorothySubscriptionCommand(syncRecordId),
            _ => throw new ArgumentOutOfRangeException(nameof(SyncRecordAction), action, null)
        };
    }
    
}