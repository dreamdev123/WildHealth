using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.HealthSummaries;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Managers.TransactionManager;
using MediatR;

namespace WildHealth.Application.CommandHandlers.HealthSummaries;

public class BatchUpdateHealthSummaryCommandHandler : IRequestHandler<BatchUpdateHealthSummaryCommand>
{
    private readonly IHealthSummaryService _healthSummaryService;
    private readonly ITransactionManager _transactionManager;
    private readonly IPatientsService _patientsService;

    public BatchUpdateHealthSummaryCommandHandler(
        IHealthSummaryService healthSummaryService,
        ITransactionManager transactionManager,
        IPatientsService patientsService)
    {
        _healthSummaryService = healthSummaryService;
        _transactionManager = transactionManager;
        _patientsService = patientsService;
    }
    
    public async Task Handle(BatchUpdateHealthSummaryCommand request, CancellationToken cancellationToken)
    {
        var values = request.Values;
        if (!values.Any())
        {
            return;
        }

        var patientId = values.First().PatientId;
        var patient = await _patientsService.GetByIdAsync(patientId, PatientSpecifications.PatientUserSpecification);

        var currentValues = await _healthSummaryService.GetByPatientAsync(patientId);

        async Task Transaction()
        {
            foreach (var valueModel in values)
            {
                var current = currentValues.FirstOrDefault(x => x.Key == valueModel.Key);

                if (valueModel.IsNew)
                {
                    await CreateAsync(patient, valueModel);
                    continue;
                }

                if (current is null)
                {
                    continue;
                }
                
                if (valueModel.IsDeleted)
                {
                    await DeleteAsync(current);
                    continue;
                }
                
                await UpdateAsync(current, valueModel.Value);
            }
        }

        await _transactionManager.Run(Transaction);
    }

    private async Task CreateAsync(Patient patient, HealthSummaryValueModel model)
    {
        var newValue = new HealthSummaryValue(
            patient: patient,
            key: model.Key,
            name: model.Name,
            value: model.Value);

         await _healthSummaryService.CreateAsync(newValue);
    }

    private async Task UpdateAsync(HealthSummaryValue summaryValue, string? newValue)
    {
        summaryValue.SetValue(newValue);

        await _healthSummaryService.UpdateAsync(summaryValue);
    }

    private async Task DeleteAsync(HealthSummaryValue summaryValue)
    {
        await _healthSummaryService.DeleteAsync(summaryValue.PatientId, summaryValue.Key);
    }
}