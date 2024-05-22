using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.CommandHandlers.Recommendations.Flows;
using WildHealth.Application.Commands.Recommendations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Recommendations;
using WildHealth.Domain.Entities.Recommendations;

namespace WildHealth.Application.CommandHandlers.Recommendations;

public class VerifyPatientRecommendationCommandHandler : IRequestHandler<VerifyPatientRecommendationCommand, PatientRecommendation>
{
    private readonly IPatientRecommendationsService _patientRecommendationsService;
    private readonly MaterializeFlow _materialize;

    public VerifyPatientRecommendationCommandHandler(IPatientRecommendationsService patientRecommendationsService, MaterializeFlow materialize)
    {
        _patientRecommendationsService = patientRecommendationsService;
        _materialize = materialize;
    }

    public async Task<PatientRecommendation> Handle(VerifyPatientRecommendationCommand command, CancellationToken cancellationToken)
    {
        var patientRecommendation = await _patientRecommendationsService.GetByIdAsync(command.PatientRecommendationId);

        await new VerifyPatientRecommendationFlow(patientRecommendation).Materialize(_materialize);

        return patientRecommendation;
    }
}
