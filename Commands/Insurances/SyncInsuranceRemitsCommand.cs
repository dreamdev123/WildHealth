using MediatR;

namespace WildHealth.Application.Commands.Insurances;

public class SyncInsuranceRemitsCommand : IRequest
{
    public int PracticeId { get; }

    public SyncInsuranceRemitsCommand(int practiceId)
    {
        PracticeId = practiceId;
    }
}