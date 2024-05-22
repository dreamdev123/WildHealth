using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Services.Patients;
using WildHealth.ClarityCore.WebClients.Patients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Reports.Alpha;
using WildHealth.Domain.Entities.Reports.DietAndNutrition;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class UpdatePatientApoeHealthSummaryCommandHandler : IRequestHandler<UpdatePatientApoeHealthSummaryCommand>
    {
        private readonly IHealthSummaryService _healthSummaryService;
        private readonly ILogger<UpdatePatientApoeHealthSummaryCommandHandler> _logger;
        private readonly IPatientsService _patientsService;
        
        public UpdatePatientApoeHealthSummaryCommandHandler(
            ILogger<UpdatePatientApoeHealthSummaryCommandHandler> logger,
            IPatientsService patientsService,
            IHealthSummaryService healthSummaryService)
        {
            _logger = logger;
           _healthSummaryService = healthSummaryService;
           _patientsService = patientsService;
        }

        public async Task Handle(UpdatePatientApoeHealthSummaryCommand command, CancellationToken cancellationToken)
        {
            var healthSummaryRecords = await _healthSummaryService.GetByPatientAsync(command.PatientId);

            await UpdateRecordsWithApoeValues(healthSummaryRecords, command);
        }

        private async Task UpdateRecordsWithApoeValues(HealthSummaryValue[] healthSummaryRecords,
            UpdatePatientApoeHealthSummaryCommand command)
        {
            try
            {
                var screeningHealthMaintenance =
                    await _healthSummaryService.GetMapByKeyAsync(HealthSummaryConstants.SCREENING_HEALTH_MAINTENANCE);
                var patient = await _patientsService.GetByIdAsync(command.PatientId);
                var apoeScore = command.Report.DietAndNutritionReport.ApoeAccuracyScore;
                var scorePercent = command.Report.DietAndNutritionReport.ApoeAccuracyScore.ScorePercent;

                if (apoeScore is not null && !apoeScore.Message.Equals(String.Empty))
                {
                    //if the data exist we proceed to update these records.
                    var filteredItems = screeningHealthMaintenance[0].Items
                        .Where(hsi => hsi.Key.Contains(ApoeConstants.APOE_SUFIX));

                    foreach (var item in filteredItems)
                    {
                        await _healthSummaryService
                            .UpdateAsync(await CreateOrUpdateApoeSummaryValue(apoeScore, item, patient));
                    }
                }
            }
            catch (Exception err)
            {
                _logger.LogInformation(
                    $"Error updating Health summary for APOE information for patient [id]: {command.PatientId} with error: {err.Message}");
            }
        }

        private async Task<HealthSummaryValue> CreateOrUpdateApoeSummaryValue(ApoeAccuracyScore apoe, HealthSummaryMapItem item,
            Patient patient)
        {
            var itemValue = await _healthSummaryService.GetByKeyAsync(patient.Id ?? 0, item.Key);
            if (itemValue is null)
            {
                switch (item.Key)
                {
                    case ApoeConstants.HEALTH_SUMMARY_ITEM_APOE_STATUS_LABEL_KEY:
                        return new HealthSummaryValue(patient, item.Key, item.Name);
                    case ApoeConstants.HEALTH_SUMMARY_ITEM_APOE_STATUS_KEY:
                        return new HealthSummaryValue(patient, item.Key, apoe.Apoe);
                    case ApoeConstants.HEALTH_SUMMARY_ITEM_APOE_RECOMMENDATION:
                        return new HealthSummaryValue(patient, item.Key, apoe.Message);
                    case ApoeConstants.HEALTH_SUMMARY_ITEM_APOE_ACCURACY_SCORE_VALUE:
                        return new HealthSummaryValue(patient, item.Key, apoe.ScorePercent.ToString());
                    default:
                        return new HealthSummaryValue(patient, "NOT_DEFINED", "N/A");
                }
            }
            else
            {
                switch (item.Key)
                {
                    case ApoeConstants.HEALTH_SUMMARY_ITEM_APOE_STATUS_LABEL_KEY:
                    {
                        itemValue.SetValue(item.Name);
                        return itemValue;
                    }
                    case ApoeConstants.HEALTH_SUMMARY_ITEM_APOE_STATUS_KEY:
                        itemValue.SetValue(apoe.Apoe);
                        return itemValue;
                    case ApoeConstants.HEALTH_SUMMARY_ITEM_APOE_RECOMMENDATION:
                        itemValue.SetValue(apoe.Message);
                        return itemValue;
                    case ApoeConstants.HEALTH_SUMMARY_ITEM_APOE_ACCURACY_SCORE_VALUE:
                        itemValue.SetValue(apoe.ScorePercent.ToString());
                        return itemValue;
                    default:
                        return itemValue;
                }
            }
        }
        
    }
}