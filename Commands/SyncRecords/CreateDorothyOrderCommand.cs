using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class CreateDorothyOrderCommand : IRequest
{
    public int ClaimId { get; }
    
    public CreateDorothyOrderCommand(int claimId)
    {
        ClaimId = claimId;
    }
}