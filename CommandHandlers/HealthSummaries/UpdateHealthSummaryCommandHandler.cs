using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.HealthSummaries;
using MediatR;

namespace WildHealth.Application.CommandHandlers.HealthSummaries;
public class UpdateHealthSummaryCommandHandler : IRequestHandler<UpdateHealthSummaryCommand, HealthSummaryValue>
{
    private readonly IHealthSummaryService _healthSummaryService;
    private readonly IPatientsService _patientsService;
    
    public UpdateHealthSummaryCommandHandler(
        IHealthSummaryService healthSummaryService,
        IPatientsService patientsService)
    {
        _healthSummaryService = healthSummaryService;
        _patientsService = patientsService;
    }

    public async Task<HealthSummaryValue> Handle(UpdateHealthSummaryCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByIdAsync(request.PatientId);

        var healthSummary = await _healthSummaryService.GetByKeyAsync(patient.GetId(), request.Key);
        
        healthSummary.SetValue(request.Value);
        healthSummary.SetName(request.Name);

        var result = await _healthSummaryService.UpdateAsync(healthSummary);

        return result;
    }
}