using System;
using System.Collections.Generic;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using WildHealth.Application.Services.Metrics;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Recommendations;
using WildHealth.Application.Services.Reports.Base;
using WildHealth.Application.Services.Reports.Template;
using WildHealth.ClarityCore.WebClients.Patients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Reports;
using WildHealth.IntegrationEvents.Reports.Payloads;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Reports.Celiac
{
    public class CeliacReportService : ReportServiceBase, ICeliacReportService
    {
        private readonly IReportTemplateService _reportTemplateService;
        private readonly IPatientReportService _patientReportService;
        private readonly IPatientMetricService _patientMetricsService;
        private readonly IPatientRecommendationsService _patientRecommendationsService;
        private readonly IPatientsService _patientsService;
        private readonly IEventBus _eventBus;

        public CeliacReportService(
            IReportTemplateService reportTemplateService,
            IPatientReportService patientReportService,
            IPatientMetricService patientMetricService,
            IPatientRecommendationsService patientRecommendationsService,
            IPatientsService patientsService,
            IEventBus eventBus
        )
        {
            _reportTemplateService = reportTemplateService;
            _patientReportService = patientReportService;
            _patientMetricsService = patientMetricService;
            _patientRecommendationsService = patientRecommendationsService;
            _patientsService = patientsService;
            _eventBus = eventBus;
        }

        public async Task<PatientReport> GetLatestAsync(int patientId)
        {
            return await _patientReportService.GetLatestAsync(ReportType.Celiac, patientId);
        }

        public async Task<PatientReport> CreateAsync(int patientId)
        {
            var reportTemplate = await _reportTemplateService.GetLatest(ReportType.Celiac);

            if (reportTemplate is null)
            {
                throw new AppException(HttpStatusCode.NotImplemented, "Celiac ReportTemplate not found");
            }

            var canGeneratePatientReport = await _patientReportService.TryPrepareForGeneration(patientId, reportTemplate);

            if(!canGeneratePatientReport.Item1)
            {
                throw new AppException(HttpStatusCode.BadRequest, $"Cannot generate Celiac Report for PatientID {patientId}. Does not have required metrics");
            }

            var reportMetrics = reportTemplate.ReportTemplateMetrics.Select(x => x.Metric);
            var patientReportMetrics = new List<PatientMetric>();
            foreach (var reportMetric in reportMetrics)
            {
                patientReportMetrics.Add(await _patientMetricsService.GetLatestAsync(patientId, reportMetric));
            }

            var patientRecommendations = await _patientRecommendationsService.GetAddOnReportRecommendations(patientId, HealthCategoryTag.Celiac);

            if (patientRecommendations is null || !patientRecommendations.Any())
            {
                throw new AppException(HttpStatusCode.BadRequest, $"Cannot generate Celiac Report for PatientID {patientId}. Does not have required recommendations");
            }

            var report = _patientReportService.GenerateReport(patientId, reportTemplate, patientReportMetrics, patientRecommendations.ToList());

            await _patientReportService.CreateReportAsync(report);

            var patientUniversalId = await _patientsService.GetUniversalIdForPatientId(patientId);

            await _eventBus.Publish( new ReportIntegrationEvent(
                payload: new AddOnReportActivatedPayload(
                    reportTypeId: (int)ReportType.Celiac,
                    reportTypeName: ReportType.Celiac.ToString()
                ),
                patient: new PatientMetadataModel(patientId, patientUniversalId),
                eventDate: DateTime.UtcNow
            ));

            return report;
        }
    }
}