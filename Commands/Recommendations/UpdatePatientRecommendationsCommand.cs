using MediatR;
using WildHealth.Domain.Enums.Metrics;

namespace WildHealth.Application.Commands.Recommendations;

public class UpdatePatientRecommendationsCommand : IRequest
{
    public int PatientId { get; set; }
    
    public MetricSource[] Sources { get; set; }

    public UpdatePatientRecommendationsCommand(int patientId, MetricSource[] sources)
    {
        PatientId = patientId;
        Sources = sources;
    }
}