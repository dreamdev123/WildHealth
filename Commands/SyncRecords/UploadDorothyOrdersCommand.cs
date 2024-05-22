using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class UploadDorothyOrdersCommand : IRequest
{
    public int PracticeId { get; }

    public UploadDorothyOrdersCommand(int practiceId)
    {
        PracticeId = practiceId;
    }
}