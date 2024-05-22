using System.Threading.Tasks;
using WildHealth.Common.Models.Metrics;
using WildHealth.Domain.Entities.Metrics;

namespace WildHealth.Application.Utils.Metrics;

delegate Task<UnclassifiedPatientMetricModel?> MetricValueRetriever(Metric metric, int patientId);

public interface IAddOnReportMetricRetriever
{
    Task<UnclassifiedPatientMetricModel?> Get(Metric metric, int patientId);
}