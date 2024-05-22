using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.HealthSummaries;
using MediatR;

namespace WildHealth.Application.CommandHandlers.HealthSummaries
{
    public class CreateHealthSummaryCommandHandler : IRequestHandler<CreateHealthSummaryCommand, HealthSummaryValue>
    {
        private readonly IHealthSummaryService _healthSummaryService;
        private readonly IPatientsService _patientsService;
        
        public CreateHealthSummaryCommandHandler(
            IHealthSummaryService healthSummaryService,
            IPatientsService patientsService)
        {
            _healthSummaryService = healthSummaryService;
            _patientsService = patientsService;
        }

        public async Task<HealthSummaryValue> Handle(CreateHealthSummaryCommand request, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(request.PatientId);

            var healthSummary = new HealthSummaryValue(
                patient: patient,
                key: request.Key,
                name: request.Name,
                value: request.Value);

            var result = await _healthSummaryService.CreateAsync(healthSummary);

            return result;
        }
    }
}