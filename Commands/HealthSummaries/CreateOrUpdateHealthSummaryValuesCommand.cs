using MediatR;
using WildHealth.Common.Models.HealthSummaries;

namespace WildHealth.Application.Commands.HealthSummaries;

public class CreateOrUpdateHealthSummaryValuesCommand : IRequest
{
    public HealthSummaryValueModel[] Values { get; set; }
    public int PatientId { get; set; }

    public CreateOrUpdateHealthSummaryValuesCommand(HealthSummaryValueModel[] values, int patientId)
    {
        Values = values;
        PatientId = patientId;
    }
}