using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Utils.Metrics;
using WildHealth.ClarityCore.Models.PatientSnps;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.CommandHandlers.Metrics.Flows
{
    public class CreateDnaPatientMetricsFlow : IMaterialisableFlow
    {
        private readonly Patient _patient;
        private readonly Metric[] _metrics;
        private readonly InputsAggregator _inputsAggregator;
        private readonly GenotypeClassificationModel _classifications;
        private readonly IDictionary<ClassificationType, IList<ClassificationTypeOption>> _classificationTypeOptions;
        private readonly IClassificationTypeOptionHelper _classificationTypeOptionHelper;
        private readonly ILogger _logger;

        public CreateDnaPatientMetricsFlow(
            Patient patient,
            Metric[] metrics,
            InputsAggregator inputsAggregator,
            GenotypeClassificationModel classifications,
            IDictionary<ClassificationType, IList<ClassificationTypeOption>> classificationTypeOptions,
            IClassificationTypeOptionHelper classificationTypeOptionHelper,
            ILogger logger
        )
        {
            _patient = patient;
            _metrics = metrics;
            _inputsAggregator = inputsAggregator;
            _classifications = classifications;
            _classificationTypeOptions = classificationTypeOptions;
            _classificationTypeOptionHelper = classificationTypeOptionHelper;
            _logger = logger;
        }
        
        public MaterialisableFlowResult Execute()
        {
            var newPatientMetrics = new List<PatientMetric>();

            var snpClassifications = _classifications.Snps;
            var errors = _classifications.Errors;

            foreach(var classification in snpClassifications)
            {
                var metric = _metrics.Where(x => x.Identifier == classification.Rsid).FirstOrDefault();

                if (metric is null)
                {
                    continue;
                }
                var classificationOption = _classificationTypeOptionHelper.GetClassificationTypeOption(_classificationTypeOptions, metric.ClassificationType, classification.GenotypeClassification);

                newPatientMetrics.Add(new PatientMetric(
                    patientId: _patient.GetId(),
                    metricId: metric.Id ?? 0,
                    value: classification.Genotype,
                    valueUnits: "genotype",
                    classificationTypeOptionId: classificationOption.Id ?? 0
                ));
            }

            foreach(var error in errors)
            {
                _logger.LogInformation($"Could not create Patient Metric for {error.Rsid} with error {error.Error}");
            }

            if (newPatientMetrics.Any())
            {
                return newPatientMetrics.Select(x => x.Added()).ToFlowResult();
            }

            return MaterialisableFlowResult.Empty;
        }
    }
}