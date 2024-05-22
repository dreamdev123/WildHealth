using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Challenges.Flows;
using WildHealth.Application.Commands.Challenges;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Challenges;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.CommandHandlers.Challenges;

public class ParticipateInChallengeCommandHandler : IRequestHandler<ParticipateInChallengeCommand, Unit>
{
    private readonly IChallengesService _challengesService;
    private readonly MaterializeFlow _materializer;

    public ParticipateInChallengeCommandHandler(IChallengesService challengesService, MaterializeFlow materializer)
    {
        _challengesService = challengesService;
        _materializer = materializer;
    }

    public async Task<Unit> Handle(ParticipateInChallengeCommand request, CancellationToken cancellationToken)
    {
        var patientChallenge = await _challengesService.GetPatientChallenge(request.PatientId, request.ChallengeId).ToOption();
        var targetChallenge = await _challengesService.GetById(request.ChallengeId);
        
        await new ParticipateInChallengeFlow(
            patientChallenge,
            targetChallenge, 
            DateTime.UtcNow,
            request.PatientId,
            request.UniversalId).Materialize(_materializer);
        
        return Unit.Value;
    }
}