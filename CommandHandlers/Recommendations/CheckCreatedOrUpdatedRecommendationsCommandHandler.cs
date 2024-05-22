using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Recommendations.Flows;
using WildHealth.Application.Commands.Recommendations;
using WildHealth.Application.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Metrics;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Recommendations;
using WildHealth.Report.DataProviders;

namespace WildHealth.Application.CommandHandlers.Recommendations;

public class CheckCreatedOrUpdatedRecommendationsCommandHandler : IRequestHandler<CheckCreatedOrUpdatedRecommendationsCommand>
{
    private readonly IRecommendationsService _recommendationsService;
    private readonly IPatientRecommendationsService _patientRecommendationsService;
    private readonly IPatientsService _patientsService;
    private readonly IPatientMetricService _patientMetricService;
    private readonly MaterializeFlow _materialize;
    private readonly ILogger<CheckCreatedOrUpdatedRecommendationsCommandHandler> _logger;

    public CheckCreatedOrUpdatedRecommendationsCommandHandler(
        IRecommendationsService recommendationsService,
        IPatientRecommendationsService patientRecommendationsService,
        IPatientsService patientsService,
        IPatientMetricService patientMetricService,
        MaterializeFlow materialize,
        ILogger<CheckCreatedOrUpdatedRecommendationsCommandHandler> logger)
    {
        _recommendationsService = recommendationsService;
        _patientRecommendationsService = patientRecommendationsService;
        _patientsService = patientsService;
        _patientMetricService = patientMetricService;
        _materialize = materialize;
        _logger = logger;
    }

    public async Task Handle(CheckCreatedOrUpdatedRecommendationsCommand command,
        CancellationToken cancellationToken)
    {
        // Grab all recently added or updated recommendations
        var from = DateTime.UtcNow.Date;
        var recommendations = await _recommendationsService.GetRecentlyAddedOrUpdatedRecommendationsAsync(from);
        
        // If none have been added or updated, just return
        if (recommendations.IsNullOrEmpty())
        {
            return;
        }
        
        // Grab all active patients
        var patients = await _patientsService.GetAllWithActiveSubscription();
        
        // For each added or updated recommendation go through each patient and determine if they have that recommendation
        foreach (var patient in patients)
        {
            try
            {
                var patientRecommendations = await _patientRecommendationsService.GetByPatientIdAsync(patient.GetId());

                var patientMetrics = await _patientMetricService.GetByPatientIdAsync(patient.GetId());

                var data = new RecommendationDataProvider().Get(patientMetrics).Data;

                foreach (var recommendation in recommendations)
                {
                    try
                    {
                        var existingPatientRecommendation =
                            patientRecommendations.FirstOrDefault(o => o.RecommendationId == recommendation.GetId());

                        await new ProcessRecommendationFlow(
                                patient.GetId(), 
                                recommendation,
                                existingPatientRecommendation,
                                patientMetrics: patientMetrics,
                                data).Materialize(_materialize);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(
                            $"Failed to process recommendation for recommendation id = {recommendation.GetId()} and patient id = {patient.GetId()} with error = {e}");
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(
                    $"Failed to process recommendation for patient id = {patient.GetId()} with error = {e}");
            }
        }
    }
}