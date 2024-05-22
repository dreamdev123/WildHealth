using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Metrics;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Recommendations;
using WildHealth.Application.Services.Reports.Template;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Reports;
using WildHealth.IntegrationEvents.Reports.Payloads;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Reports.Base;

public class SleepReportService : ISleepReportService
{
    private readonly IReportTemplateService _reportTemplateService;
    private readonly IPatientReportService _patientReportService;
    private readonly IPatientMetricService _patientMetricsService;
    private readonly IPatientRecommendationsService _patientRecommendationsService;
    private readonly IPatientsService _patientsService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<SleepReportService> _logger;

    public SleepReportService(IReportTemplateService reportTemplateService,
        IPatientReportService patientReportService,
        IPatientMetricService patientMetricService,
        IPatientRecommendationsService patientRecommendationsService,
        IPatientsService patientsService,
        IEventBus eventBus, 
        ILogger<SleepReportService> logger)
    {
        _reportTemplateService = reportTemplateService;
        _patientReportService = patientReportService;
        _patientMetricsService = patientMetricService;
        _patientRecommendationsService = patientRecommendationsService;
        _patientsService = patientsService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<PatientReport> GetLatestAsync(int patientId, ReportType reportType)
    {
        return await _patientReportService.GetLatestAsync(reportType, patientId);
    }

    public async Task<PatientReport> CreateAsync(int patientId, ReportType reportType, HealthCategoryTag recommendationsTag)
    {
        var reportTemplate = await _reportTemplateService.GetLatest(reportType);

        if (reportTemplate is null)
        {
            throw new AppException(HttpStatusCode.NotImplemented, $"{reportType.ToString()} ReportTemplate not found");
        }
        
        var reportMetrics = reportTemplate.ReportTemplateMetrics.Select(x => x.Metric);
        var patientReportMetrics = new List<PatientMetric>();
        foreach (var reportMetric in reportMetrics)
        {
            var patientMetricOption = await _patientMetricsService.GetLatestAsync(patientId, reportMetric).ToOption();
            if (patientMetricOption.HasValue())
                patientReportMetrics.Add(patientMetricOption.Value());
            else
                _logger.LogError($"Could not find the metric '{reportMetric.Label}'. PatientId: {patientId}. ReportType: {reportType}");
        }

        var patientRecommendations = await _patientRecommendationsService.GetAddOnReportRecommendations(patientId, recommendationsTag);

        var report = _patientReportService.GenerateReport(patientId, reportTemplate, patientReportMetrics, patientRecommendations.ToList());

        await _patientReportService.CreateReportAsync(report);

        var patientUniversalId = await _patientsService.GetUniversalIdForPatientId(patientId);

        await _eventBus.Publish(new ReportIntegrationEvent(
            payload: new AddOnReportActivatedPayload(
                reportTypeId: (int)reportType,
                reportTypeName: reportType.ToString()
            ),
            patient: new PatientMetadataModel(patientId, patientUniversalId),
            eventDate: DateTime.UtcNow
        ));

        return report;
    }
}