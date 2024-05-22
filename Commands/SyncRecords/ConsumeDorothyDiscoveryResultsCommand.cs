using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.SyncRecords;

public class ConsumeDorothyDiscoveryResultsCommand : IRequest
{
    public IFormFile File { get; }

    public ConsumeDorothyDiscoveryResultsCommand(IFormFile file)
    {
        File = file;
    }
}