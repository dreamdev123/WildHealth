using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Reports.Base;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;
using WildHealth.Report.Providers.HealthReportVersion;
using WildHealth.Report.Services.ReportGenerator;

namespace WildHealth.Application.Services.HealthReports
{
    /// <summary>
    /// <see cref="IHealthReportService"/>
    /// </summary>
    public class HealthReportService : ReportServiceBase, IHealthReportService
    {
        private readonly IPatientsService _patientsService;
        private readonly IGeneralRepository<HealthReport> _reportsRepository;
        private readonly IHealthReportGenerator<HealthReport> _reportGenerator;
        private readonly IHealthReportVersionProvider _healthReportVersionProvider;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly ILogger<HealthReportService> _logger;
 
        public HealthReportService(
            IPatientsService patientsService,
            IGeneralRepository<HealthReport> reportsRepository,
            IHealthReportGenerator<HealthReport> reportGenerator,
            IHealthReportVersionProvider healthReportVersionProvider,
            IFeatureFlagsService featureFlagsService,
            ILogger<HealthReportService> logger)
        {
            _patientsService = patientsService;
            _reportsRepository = reportsRepository;
            _reportGenerator = reportGenerator;
            _healthReportVersionProvider = healthReportVersionProvider;
            _featureFlagsService = featureFlagsService;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IHealthReportService.GetReportsReviewingByAsync"/>
        /// </summary>
        /// <param name="reviewingBy"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<(IEnumerable<HealthReport> reports, int totalCount)> GetReportsReviewingByAsync(
            int reviewingBy,
            int? skip,
            int? take)
        {
            return await GetReportsByStatusAsync(
                reviewingBy: reviewingBy, 
                completedBy: null, 
                status: HealthReportStatus.UnderReview, 
                skip: skip, 
                take: take
            );
        }

        /// <summary>
        /// <see cref="IHealthReportService.GetReportsCompletedByAsync"/>
        /// </summary>
        /// <param name="completedBy"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        public async Task<(IEnumerable<HealthReport> reports, int totalCount)> GetReportsCompletedByAsync(
            int completedBy,
            int? skip,
            int? take)
        {
            return await GetReportsByStatusAsync(
                reviewingBy: null, 
                completedBy: completedBy, 
                status: HealthReportStatus.UnderReview, 
                skip: skip, 
                take: take
            );
        }

        /// <summary>
        /// <see cref="IHealthReportService.ReportsUnderReviewExist"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public async Task<bool> ReportsUnderReviewExist(int employeeId)
        {
            return await _reportsRepository
                .All()
                .ReviewingBy(employeeId)
                .ByStatus(HealthReportStatus.UnderReview)
                .AnyAsync();
        }

        /// <summary>
        /// <see cref="IHealthReportService.GetReportsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<IEnumerable<HealthReport>> GetReportsAsync(int patientId, HealthReportStatus? status = null)
        {
            var reports = await _reportsRepository
                .All()
                .ByStatus(status)
                .RelatedToPatient(patientId)
                .IncludePatient()
                .OrderBy(x => x.CreatedAt)
                .AsNoTracking()
                .ToArrayAsync();

            return reports;
        }

        /// <summary>
        /// <see cref="IHealthReportService.GetLatestOrCreateAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<HealthReport> GetLatestOrCreateAsync(int patientId)
        {
            var report = await _reportsRepository
                .All()
                .RelatedToPatient(patientId)
                .OrderBy(x => x.CreatedAt)
                .IncludeRecommendations()
                .IncludeSubReports()
                .IncludePatient()
                .IncludeReviewer()
                .LastOrDefaultAsync();

            if (report is not null)
            {
                return report;
            }

            return await CreateAsync(patientId);
        }

        /// <summary>
        /// <see cref="IHealthReportService.GetLatestAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<HealthReport> GetLatestAsync(int patientId, HealthReportStatus? status = null)
        {
            var report = await _reportsRepository
                .All()
                .ByStatus(status)
                .RelatedToPatient(patientId)
                .OrderBy(x => x.CreatedAt)
                .IncludeSubReports()
                .IncludeRecommendations()
                .IncludePatient()
                .IncludeReviewer()
                .LastOrDefaultAsync();

            if (report is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Health Report related to patient does not exist.", exceptionParam);
            }
            
            return report;
        }

        /// <summary>
        /// <see cref="IHealthReportService.GetAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<HealthReport> GetAsync(int id, int patientId)
        {
            var report = await _reportsRepository
                .All()
                .RelatedToPatient(patientId)
                .ById(id)
                .IncludeSubReports()
                .IncludeRecommendations()
                .IncludePatient()
                .IncludeReviewer()
                .FirstOrDefaultAsync();

            if (report is not null)
            {
                return report;
            }
            
            _logger.LogError($"Health Report related to patient with [Id] = {patientId} does not exist.");
            var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
            throw new AppException(HttpStatusCode.NotFound, "Health Report related to patient does not exist.", exceptionParam);
        }

        /// <summary>
        /// Generates report
        /// </summary>
        /// <param name="report"></param>
        /// <param name="aggregator"></param>
        /// <returns></returns>
        public async Task<HealthReport> GenerateAsync(HealthReport report, InputsAggregator aggregator)
        {
            if (!report.CanBeModified())
            {
                _logger.LogWarning($"Report with [Id] = {report.Id} can not be modified.");
                
                throw new AppException(HttpStatusCode.BadRequest, $"Report can not be modified.");
            }
            
            report.MarkAsInProgress(DateTime.UtcNow);
            _reportsRepository.Edit(report);
            await _reportsRepository.SaveAsync();

            try
            {
                _logger.LogInformation($"Generation of Health Report with [Id] = {report.Id} started.");

                var version = _featureFlagsService.GetFeatureFlag("WH-ALL-Report-Recommendations")
                    ? _healthReportVersionProvider.Get()
                    : "2.0.0.1";

                var recommendationVersion = _featureFlagsService.GetFeatureFlag(Common.Constants.FeatureFlags.RecommendationsV2);
                report = await _reportGenerator.Generate(report, aggregator, recommendationVersion);
                report.MarkAsCompleted(DateTime.UtcNow, version);
                _reportsRepository.Edit(report);
                await _reportsRepository.SaveAsync();
                
                _logger.LogInformation($"Generation of Health Report with [Id] = {report.Id} finished.");
            }
            catch(Exception ex)
            {
                _logger.LogError($"Generation of Health Report with [Id] = {report.Id} failed. {ex}");
                report.MarkAsFailed(DateTime.UtcNow, ex.Message);
                _reportsRepository.Edit(report);
                await _reportsRepository.SaveAsync();
            }

            return report;
        }

        /// <summary>
        /// <see cref="IHealthReportService.UpdateAsync"/>
        /// </summary>
        /// <param name="report"></param>
        /// <returns></returns>
        public async Task<HealthReport> UpdateAsync(HealthReport report)
        {
            _reportsRepository.Edit(report);
            await _reportsRepository.SaveAsync();

            _logger.LogInformation($"Report with [Id] = {report.Id} updated.");
            
            return report;
        }

        /// <summary>
        /// <see cref="IHealthReportService.CreateAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<HealthReport> CreateAsync(int patientId)
        {
            var patient = await _patientsService.GetByIdAsync(patientId);
            
            var report = new HealthReport
            {
                PatientId = patient.GetId()
            };
            
            await _reportsRepository.AddAsync(report);
            await _reportsRepository.SaveAsync();
            
            _logger.LogInformation($"Report with [Id] = {report.Id} created.");

            return report;
        }

        /// <summary>
        /// <see cref="IHealthReportService.SendToReviewAsync"/>
        /// </summary>
        /// <param name="report"></param>
        /// <param name="reviewer"></param>
        /// <returns></returns>
        public async Task<HealthReport> SendToReviewAsync(HealthReport report, Employee reviewer)
        {
            report.MarkAsUnderReview(report.Status.Date, reviewer);
            
            _reportsRepository.Edit(report);
            
            await _reportsRepository.SaveAsync();

            _logger.LogInformation($"Report with [Id] = {report.Id} was sent to review.");
            
            return report;
        }

        /// <summary>
        /// <see cref="IHealthReportService.SubmitAsync"/>
        /// </summary>
        /// <param name="report"></param>
        /// <param name="reviewer"></param>
        /// <returns></returns>
        public async Task<HealthReport> SubmitAsync(HealthReport report, Employee reviewer)
        {
            report.MarkAsSubmitted(report.Status.Date, reviewer);
            
            _reportsRepository.Edit(report);
            
            await _reportsRepository.SaveAsync();

            _logger.LogInformation($"Report with [Id] = {report.Id} submitted.");
            
            return report;
        }
        /// <summary>
        /// <see cref="IHealthReportService.GetReportsReviewingByAsync"/>
        /// </summary>
        /// <param name="reviewingBy"></param>
        /// <param name="completedBy"></param>
        /// <param name="status"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        private async Task<(IEnumerable<HealthReport> reports, int totalCount)> GetReportsByStatusAsync(
            int? reviewingBy,
            int? completedBy,
            HealthReportStatus status,
            int? skip,
            int? take)
        {
            var query = _reportsRepository
                .All()
                .ReviewingBy(reviewingBy)
                .CompletedBy(completedBy)
                .IncludePatient()
                .ByStatus(status)
                .OrderBy(x => x.Status.Date)
                .AsNoTracking();

            var totalCount = await query.CountAsync();
            var reports = await query.Pagination(skip, take).ToArrayAsync();

            return (reports, totalCount);
        }
    }
}