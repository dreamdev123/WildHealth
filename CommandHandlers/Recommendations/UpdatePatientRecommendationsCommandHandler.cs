using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Recommendations.Flows;
using WildHealth.Application.Commands.Recommendations;
using WildHealth.Application.Events.PatientRecommendations;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Metrics;
using WildHealth.Application.Services.Recommendations;
using WildHealth.Report.DataProviders;

namespace WildHealth.Application.CommandHandlers.Recommendations;

public class UpdatePatientRecommendationsCommandHandler : IRequestHandler<UpdatePatientRecommendationsCommand>
{
    private readonly IRecommendationsService _recommendationsService;
    private readonly IPatientRecommendationsService _patientRecommendationsService;
    private readonly IPatientMetricService _patientMetricService;
    private readonly MaterializeFlow _materialize;
    private readonly ILogger<UpdatePatientRecommendationsCommandHandler> _logger;

    public UpdatePatientRecommendationsCommandHandler(
        IRecommendationsService recommendationsService,
        IPatientRecommendationsService patientRecommendationsService,
        IPatientMetricService patientMetricService,
        MaterializeFlow materialize,
        ILogger<UpdatePatientRecommendationsCommandHandler> logger)
    {
        _recommendationsService = recommendationsService;
        _patientRecommendationsService = patientRecommendationsService;
        _patientMetricService = patientMetricService;
        _materialize = materialize;
        _logger = logger;
    }

    public async Task Handle(UpdatePatientRecommendationsCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Updating of patient recommendations for patient id = {command.PatientId} and metric sources = {command.Sources} has: started");
            
        var recommendations = await _recommendationsService.GetRecommendationsByMetricSourceAsync(command.Sources);

        var patientRecommendations = await _patientRecommendationsService.GetByPatientIdAsync(command.PatientId);
        
        var patientMetrics = await _patientMetricService.GetByPatientIdAsync(command.PatientId);
        
        var data = new RecommendationDataProvider().Get(patientMetrics).Data;

        var interimResult = MaterialisableFlowResult.Empty;

        foreach (var recommendation in recommendations)
        {
            var existingPatientRecommendation =
                patientRecommendations.FirstOrDefault(o => o.RecommendationId == recommendation.GetId());

            interimResult += new ProcessRecommendationFlow(
                command.PatientId,
                recommendation,
                existingPatientRecommendation,
                patientMetrics,
                data).Execute();
        }
        
        // Aggregate all of the events
        var aggregateResult = MaterialisableFlowResult.Empty;
        
        // Want to keep all of the categories we are not aggregating
        aggregateResult += interimResult.EntityActions;
        aggregateResult += interimResult.IntegrationEvents;

        var mediatorEventsToAggregate =
            interimResult.MediatorEvents.Where(o => o.GetType() == typeof(PatientRecommendationVerifiedEvent));
        
        var mediatorEventsToKeep =
            interimResult.MediatorEvents.Where(o => o.GetType() != typeof(PatientRecommendationVerifiedEvent));
        
        // Keep all of the events we are not aggregating
        aggregateResult += mediatorEventsToKeep;
        
        // Aggregate the new events
        aggregateResult += new PatientRecommendationVerifiedEvent(
            mediatorEventsToAggregate
                .SelectMany(o => ((PatientRecommendationVerifiedEvent) o).PatientRecommendations).ToArray());
        
        await _materialize.Invoke(aggregateResult);
        
        
        _logger.LogInformation($"Updating of patient recommendations for patient id = {command.PatientId} and metric sources = {command.Sources} has: finished");
    }
}