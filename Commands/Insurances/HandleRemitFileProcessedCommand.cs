using MediatR;

namespace WildHealth.Application.Commands.Insurances;

public class HandleRemitFileProcessedCommand : IRequest
{
    public int RemitFileId { get; }

    public HandleRemitFileProcessedCommand(int remitFileId)
    {
        RemitFileId = remitFileId;
    }
}