using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Challenges.Flows;
using WildHealth.Application.Commands.Challenges;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Challenges;

namespace WildHealth.Application.CommandHandlers.Challenges;

public class CompleteChallengeCommandHandler : IRequestHandler<CompleteChallengeCommand, Unit>
{
    private readonly IChallengesService _challengesService;
    private readonly MaterializeFlow _materializer;

    public CompleteChallengeCommandHandler(IChallengesService challengesService, MaterializeFlow materializer)
    {
        _challengesService = challengesService;
        _materializer = materializer;
    }

    public async Task<Unit> Handle(CompleteChallengeCommand request, CancellationToken cancellationToken)
    {
        var patientChallenge = await _challengesService.GetPatientChallenge(request.PatientId, request.ChallengeId);

        await new CompleteChallengeFlow(patientChallenge, DateTime.UtcNow, request.UniversalId).Materialize(_materializer);

        return Unit.Value;
    }
}