using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Polly;
using WildHealth.Application.CommandHandlers.Challenges.Flows;
using WildHealth.Application.Commands.Challenges;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Challenges;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.CommandHandlers.Challenges;

public class LikeChallengeCommandHandler : IRequestHandler<LikeChallengeCommand, Unit>
{
    private readonly IChallengesService _challengesService;
    private readonly MaterializeFlow _materializer;

    public LikeChallengeCommandHandler(IChallengesService challengesService, MaterializeFlow materializer)
    {
        _challengesService = challengesService;
        _materializer = materializer;
    }

    public async Task<Unit> Handle(LikeChallengeCommand request, CancellationToken cancellationToken)
    {
        var challengeTitle = await _challengesService.GetTitleById(request.ChallengeId);
        var policy = Policy
            .Handle<DbUpdateException>()
            .WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(30), TimeSpan.FromMilliseconds(40), TimeSpan.FromMilliseconds(50) });

        await policy.ExecuteAsync(async () =>
        {
            var patientChallenge = await _challengesService.GetPatientChallenge(request.PatientId, request.ChallengeId).ToOption();
            await new LikeChallengeFlow(patientChallenge,
                request.ChallengeId,
                request.PatientId,
                challengeTitle,
                request.UniversalId,
                DateTime.UtcNow).Materialize(_materializer);
        });
        
        return Unit.Value;
    }
}