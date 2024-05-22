using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Metrics;
using WildHealth.Application.CommandHandlers.Metrics.Flows;
using WildHealth.Application.Events.Reports;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Services.Metrics;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.Metrics;
using WildHealth.Application.Utils.LabNameRangeProvider;
using WildHealth.ClarityCore.Models.PatientSnps;
using WildHealth.ClarityCore.WebClients.Patients;
using WildHealth.ClarityCore.WebClients.PatientSnps;
using WildHealth.Common.Models.Metrics;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Metrics;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Recommendations;
using WildHealth.IntegrationEvents.Recommendations.Payloads;
namespace WildHealth.Application.CommandHandlers.Metrics
{
    public class CreatePatientMetricsCommandHandler : IRequestHandler<CreatePatientMetricsCommand, List<PatientMetric>>
    {
        private readonly IClassificationTypeOptionService _classificationOptionService;
        private readonly IInputsService _inputsService;
        private readonly IMetricService _metricService;
        private readonly IPatientsService _patientsService;
        private readonly IPatientMetricService _patientMetricService;
        private readonly IPatientSnpsWebClient _patientSnpsWebClient;
        private readonly IPatientsWebClient _patientsWebClient;        
        private readonly ILogger<CreatePatientMetricsCommandHandler> _logger;
        private readonly ILabNameRangeProvider _labNameRangeProvider;
        private readonly IWildHealthRangeClassifier _rangeClassifier;
        private readonly IEventBus _eventBus;
        private readonly ICalculatorMetricClassifier _calculatorMetricClassifier;
        private readonly IAddOnReportMetricRetriever _addOnReportMetricRetriever;
        private readonly IClassificationTypeOptionHelper _classificationTypeOptionHelper;
        private readonly IMediator _mediator;
        private readonly MaterializeFlow _materializer;

        public CreatePatientMetricsCommandHandler(
            IClassificationTypeOptionService classificationOptionService,
            IInputsService inputsService,
            IMetricService metricService,
            IPatientsService patientsService,
            IPatientMetricService patientMetricService,
            IPatientSnpsWebClient patientSnpsWebClient,
            IPatientsWebClient patientsWebClient,
            ILogger<CreatePatientMetricsCommandHandler> logger,
            ILabNameRangeProvider labNameRangeProvider,
            IWildHealthRangeClassifier rangeClassifier,
            IEventBus eventBus,
            ICalculatorMetricClassifier calculatorMetricClassifier,
            IAddOnReportMetricRetriever addOnReportMetricRetriever,
            IClassificationTypeOptionHelper classificationTypeOptionHelper,
            IMediator mediator,
            MaterializeFlow materializer
        )
        {
            _classificationOptionService = classificationOptionService;
            _inputsService = inputsService;
            _metricService = metricService;
            _patientsService = patientsService;
            _patientMetricService = patientMetricService;
            _patientSnpsWebClient = patientSnpsWebClient;
            _patientsWebClient = patientsWebClient;
            _logger = logger;
            _labNameRangeProvider = labNameRangeProvider;
            _rangeClassifier = rangeClassifier;
            _eventBus = eventBus;
            _calculatorMetricClassifier = calculatorMetricClassifier;
            _addOnReportMetricRetriever = addOnReportMetricRetriever;
            _classificationTypeOptionHelper = classificationTypeOptionHelper;
            _mediator = mediator;
            _materializer = materializer;
        }

        public async Task<List<PatientMetric>> Handle (CreatePatientMetricsCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(command.PatientId);

            var newPatientMetrics = new List<PatientMetric>();
            var inputsAggregator = await _inputsService.GetAggregatorAsync(patient.GetId());

            var metricsBySource = command.Metrics.GroupBy(x => x.Source);
            var classificationTypeOptions = await _classificationOptionService.GetAllByTypeAsync();

            //Iterate through all provided sources to create each metric
            foreach (var metricGroup in metricsBySource)
            {
                var metrics = metricGroup.ToArray();

                var sourcePatientMetrics = metricGroup.Key switch
                {
                    MetricSource.Labs => (await new CreateLabPatientMetricsFlow(
                            patient, 
                            metrics, 
                            inputsAggregator,
                            classificationTypeOptions,
                            _labNameRangeProvider,
                            _rangeClassifier,
                            _classificationTypeOptionHelper,
                            _logger
                        ).Materialize(_materializer)).SelectMany<PatientMetric>(),
                    MetricSource.DNA => (await new CreateDnaPatientMetricsFlow(
                            patient, 
                            metrics, 
                            inputsAggregator, 
                            (await GetDnaData(metrics, inputsAggregator, patient)),
                            classificationTypeOptions,
                            _classificationTypeOptionHelper,
                            _logger
                        ).Materialize(_materializer)).SelectMany<PatientMetric>(),
                    MetricSource.Calculator => (await new CreateCalculatorPatientMetricsFlow(
                            patient,
                            metrics,
                            inputsAggregator,
                            classificationTypeOptions,
                            _calculatorMetricClassifier,
                            _logger
                        ).Materialize(_materializer)).SelectMany<PatientMetric>(),
                    MetricSource.AddOnReport => (await new CreateAddOnReportPatientMetricsFlow(
                            patient,
                            metrics,
                            (await GetAddonMetricData(metrics, patient)),
                            classificationTypeOptions,
                            _classificationTypeOptionHelper,
                            _logger
                        ).Materialize(_materializer)).SelectMany<PatientMetric>(),
                    MetricSource.Microbiome => (await new CreateMicrobiomePatientMetricsFlow(
                            patient,
                            metrics,
                            inputsAggregator,
                            classificationTypeOptions,
                            _classificationTypeOptionHelper,
                            _logger
                        ).Materialize(_materializer)).SelectMany<PatientMetric>(),
                    MetricSource.Biometrics => Enumerable.Empty<PatientMetric>(), // Pass for now, waiting on further details
                    MetricSource.Questionnaire => Enumerable.Empty<PatientMetric>(), // Pass for now, no metrics exist with this source at the moment
                    _ => Enumerable.Empty<PatientMetric>()
                };
                
                newPatientMetrics.AddRange(sourcePatientMetrics);
            }

            var updatedMetrics = newPatientMetrics.Select(x => x.MetricId).ToArray();
            var updatedMetricSources = metricsBySource.Select(x => (int)x.Key).ToArray();

            await _eventBus.Publish(new PatientRecommendationsIntegrationEvent(
                payload: new PatientMetricsUpdatedPayload(
                    patientId: patient.GetId(),
                    metricIds: updatedMetrics,
                    metricSources: updatedMetricSources
                ),
                patient: new PatientMetadataModel(patient.GetId(), patient.User.UserId()),
                eventDate: DateTime.UtcNow
            ), cancellationToken);

            return newPatientMetrics;
        }

        private async Task<GenotypeClassificationModel> GetDnaData(IList<Metric> metrics, InputsAggregator inputsAggregator, Patient patient)
        {
            var patientSnps = new List<string>();
            var genericSnps = new List<IDictionary<string, string>>();

            foreach (var metric in metrics)
            {
                if (metric is null || metric.Source != MetricSource.DNA)
                {
                    continue;
                }
                var dna = inputsAggregator.GetDna(metric.Identifier);

                // If we did not find the DNA data in the Portal DB, attempt to get the value from Core
                if (dna is null || dna.Genotype == Genotypes.XX)
                {
                    patientSnps.Add(metric.Identifier);
                }
                // If we do have the value in the Portal DB, use that and ask Core for the classification 
                else
                {
                    genericSnps.Add(new Dictionary<string, string>() {{"rsid", metric.Identifier}, {"genotype", dna.Genotype}});
                }
            }

            var genericClassifications = await _patientSnpsWebClient.GetGenericGenotypeClassification(genericSnps);
            var patientClassifications = await _patientSnpsWebClient.GetPatientGenotypeClassification(patient.GetId(), patientSnps);

            var classifications = new GenotypeClassificationModel() {
                Snps = genericClassifications.Snps.Concat(patientClassifications.Snps).ToList(),
                Errors = genericClassifications.Errors.Concat(patientClassifications.Errors).ToList()
            };

            return classifications;
        }

        private async Task<IDictionary<Metric, UnclassifiedPatientMetricModel?>> GetAddonMetricData(IList<Metric> metrics, Patient patient)
        {
            var addonReportData = new Dictionary<Metric, UnclassifiedPatientMetricModel?>();
            foreach(var metric in metrics)
            {
                addonReportData[metric] = await _addOnReportMetricRetriever.Get(metric, patient.GetId());
            }
            return addonReportData;
        }
    }
}