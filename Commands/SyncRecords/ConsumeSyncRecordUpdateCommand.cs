using MediatR;
using Microsoft.AspNetCore.Http;

namespace WildHealth.Application.Commands.SyncRecords;

public class ConsumeSyncRecordUpdateCommand : IRequest
{
    public IFormFile File { get; }

    public ConsumeSyncRecordUpdateCommand(IFormFile file)
    {
        File = file;
    }
}