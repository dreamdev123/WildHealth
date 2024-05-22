using System;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using Newtonsoft.Json;
using WildHealth.Application.Commands.Metrics;
using WildHealth.Application.Services.Metrics;
using WildHealth.Application.Services.Reports.Template;
using WildHealth.Application.Utils.Reports;
using WildHealth.ClarityCore.WebClients.Patients;
using WildHealth.Common.Models.Reports;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Metrics;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Recommendations;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Enums.Patient;

namespace WildHealth.Application.Services.Reports
{
    public class PatientReportService : IPatientReportService
    {
        private readonly IPatientMetricService _patientMetricService;
        private readonly IReportTemplateService _reportTemplateService;
        private readonly IPatientsWebClient _patientsWebClient;
        private readonly IGeneralRepository<Patient> _patientsRepository;
        private readonly IGeneralRepository<PatientReport> _patientReportRepository;
        private readonly IMediator _mediator;

        public PatientReportService(
            IPatientMetricService patientMetricService,
            IReportTemplateService reportTemplateService,
            IPatientsWebClient patientsWebClient,
            IGeneralRepository<PatientReport> patientReportRepository,
            IGeneralRepository<Patient> patientsRepository,
            IMediator mediator
        )
        {
            _patientMetricService = patientMetricService;
            _reportTemplateService = reportTemplateService;
            _patientsWebClient = patientsWebClient;
            _patientReportRepository = patientReportRepository;
            _patientsRepository = patientsRepository;
            _mediator = mediator;
        }

        public async Task<PatientReport> GetLatestAsync(ReportType reportType, int patientId)
        {
            var report = await _patientReportRepository
                .All()
                .ByPatient(patientId)
                .ByReportType(reportType)
                .Include(x => x.ReportTemplate)
                .FirstOrDefaultAsync();
            if (report is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, $"{reportType.ToString()} Report related to patient does not exist.", exceptionParam);
            }
            return report; 
        }

        public async Task<List<PatientReport>> GetAllPatientReportAsync(int patientId)
        {
            var reports = await _patientReportRepository
                .All()
                .ByPatient(patientId)
                .ToListAsync();

            return reports; 
        }

        public async Task<PatientReport> GetByTypeAndIdAsync(ReportType reportType, int patientReportId)
        {
            var report = await _patientReportRepository
                .All()
                .ByReportType(reportType)
                .ById(patientReportId)
                .FirstOrDefaultAsync();
            if (report is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientReportId), patientReportId);
                throw new AppException(HttpStatusCode.NotFound, $"{reportType.ToString()} Report with ID does not exist.", exceptionParam);
            }
            return report; 
        }

        public async Task<PatientReport> CreateReportAsync(PatientReport patientReport)
        {
            await _patientReportRepository.AddAsync(patientReport);

            await _patientReportRepository.SaveAsync();

            return patientReport;
        }

        public PatientReport GenerateReport( int patientId, ReportTemplate reportTemplate, 
                                            List<PatientMetric> reportPatientMetrics, List<PatientRecommendation> patientRecommendations)
        {
            var reportParser = new PatientReportTemplateParser(reportTemplate, reportPatientMetrics, patientRecommendations);
            var report = reportParser.GenerateReportContent();
            return new PatientReport() {
                ReportTemplate = reportTemplate,
                PatientId = patientId,
                ContentJson = JsonConvert.SerializeObject(report)
            };
        }
        public async Task<(bool, string)> TryPrepareForGeneration(int patientId, ReportTemplate reportTemplate)
        {
            var reportMetrics = reportTemplate.ReportTemplateMetrics.Select(x => x.Metric).ToList();
            
            if (reportMetrics is null || !reportMetrics.Any())
            {
                throw new AppException(HttpStatusCode.NotFound, $"No metrics configured for ReportTemplateId {reportTemplate.GetId()}");
            }

            var patientMetrics = await _patientMetricService.FindAsync(patientId, reportMetrics);

            var (hasMetrics, textResponse) = AssertPatientHasMetrics(reportMetrics, patientMetrics);

            // If we have the metrics available, return true
            // If we do not have the metrics for the patient, try creating them and check assertion again
            if (hasMetrics)
            {
                return (true, string.Empty);
            }

            patientMetrics = await TryCreatePatientMetrics(patientId, reportMetrics);

            return AssertPatientHasMetrics(reportMetrics, patientMetrics);
        }

        public async Task<List<PatientAddOnReportModel>> GetAvailableReports(ReportStrategy reportStrategy, int patientId)
        {
            var reports = await _reportTemplateService.GetByStrategyAsync(reportStrategy);
            var patientReports = await GetAllPatientReportAsync(patientId);
            var availableReports = new List<PatientAddOnReportModel>();
            foreach (var report in reports)
            {
                var patientReport = patientReports.OrderByDescending(x => x.CreatedAt).Where(x => x.ReportTemplate == report).FirstOrDefault();

                var canGeneratePatientReport = await TryPrepareForGeneration(patientId, report);
                // Check to see DNA exists
                var dnaExists = await DoesPatientHaveDnaResulted(patientId);

                availableReports.Add(new PatientAddOnReportModel() {
                    ReportStrategy = reportStrategy.ToString(),
                    ReportType = (int)report.ReportType,
                    ReportTypeName = report.ReportType.ToString(),
                    ReportTemplateVersion = report.Version,
                    PatientReportId = patientReport?.GetId(),
                    CreatedAt = patientReport?.CreatedAt,
                    CanGeneratePatientReport = canGeneratePatientReport.Item1,
                    CannotGeneratePatientReportReason = !dnaExists ? "We are missing a DNA file to generate this report for you. Please reach out to support@wildhealth.com if you feel like this is an error and they will help you resolve this." : canGeneratePatientReport.Item2,
                    HasDnaResults = dnaExists
                });
            }
            return availableReports;
        }
        
        private (bool, string) AssertPatientHasMetrics(List<Metric> reportMetrics, List<PatientMetric> patientMetrics)
        {
            var populatedMetrics = patientMetrics.Select(x => x.Metric);

            var missingMetrics = reportMetrics.Where(x => !populatedMetrics.Contains(x)).Select(x => x.Source).Distinct().ToArray();
            
            if (missingMetrics.Any())
            {
                return (false, $"We are missing {missingMetrics.Length} metrics to generate this report for you. Please reach out to support@wildhealth.com and they will help you resolve this.");
            }

            return (true, string.Empty);
        }

        private async Task<List<PatientMetric>> TryCreatePatientMetrics(int patientId, List<Metric> reportMetrics)
        {
            return await _mediator.Send(new CreatePatientMetricsCommand(
                patientId: patientId,
                metrics: reportMetrics.ToArray()
            ));
        }

        private async Task<bool> DoesPatientHaveDnaResulted(int patientId)
        {
            var patient = await _patientsRepository.GetAsync(patientId);

            return patient.DnaStatus == PatientDnaStatus.Completed;
        }
    }
}