using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Shared.Data.Repository;
using WildHealth.Common.Models.Reports;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Application.Extensions.Query;

namespace WildHealth.Application.Services.Reports.Template
{
    public class ReportTemplateService : IReportTemplateService
    {
        private readonly IGeneralRepository<ReportTemplate> _templatesRepository;

        public ReportTemplateService(
            IGeneralRepository<ReportTemplate> templatesRepository
        )
        {
            _templatesRepository = templatesRepository;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.GetLatestTemplate"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ReportTemplate> GetLatest(ReportType reportType)
        {
            var template =  await _templatesRepository
                .All()
                .Where(x => x.ReportType == reportType)
                .OrderByDescending(x => x.CreatedAt)
                .Include(x => x.ReportTemplateMetrics)
                .ThenInclude(x => x.Metric)
                .FindAsync();

            return template;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.GetByIdAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ReportTemplate> GetByIdAsync(int id)
        {
            var template =  await _templatesRepository
                .All()
                .ById(id)
                .FindAsync();

            return template;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.CreateTemplate"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ReportTemplate> Create(string version, string comment, string templateJson, ReportType reportType)
        {
            var healthReportTemplate = new ReportTemplate(version, templateJson, comment, reportType);

            await _templatesRepository.AddAsync(healthReportTemplate);

            await _templatesRepository.SaveAsync();

            return healthReportTemplate;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.GetAllHealthReportTemplates"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ReportTemplate[]> GetTemplates(ReportType reportType)
        {
            var templates =  await _templatesRepository
                .All()
                .Where(x => x.ReportType == reportType)
                .OrderByDescending(x => x.CreatedAt)
                .ToArrayAsync();

            return templates;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.UpdateAsync"/>
        /// </summary>
        /// <param name="templateModel"></param>
        /// <returns></returns>
        public async Task<ReportTemplate> UpdateAsync(ReportTemplateModel templateModel)
        {
            var template = await GetByIdAsync(templateModel.Id);

            template.Version = templateModel.Version;
            template.Comment = templateModel.Comment;
            template.TemplateJson = templateModel.TemplateString;
            template.ReportType = templateModel.ReportType;

            _templatesRepository.Edit(template);

            await _templatesRepository.SaveAsync();

            return template;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.CloneAsync"/>
        /// </summary>
        /// <param name="templateId"></param>
        /// <returns></returns>
        public async Task<ReportTemplate> CloneAsync(int templateId)
        {
            var template = await GetByIdAsync(templateId);

            var result = await Create(template.Version, template.Comment, template.TemplateJson, template.ReportType);

            return result;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.DeleteAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<ReportTemplate> DeleteAsync(int id)
        {
            var template = await GetByIdAsync(id);

            _templatesRepository.Delete(template);

            await _templatesRepository.SaveAsync();

            return template;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.GetByStrategyAsync"/>
        /// </summary>
        /// <param name="reportStrategy"></param>
        /// <returns></returns>
        public async Task<List<ReportTemplate>> GetByStrategyAsync(ReportStrategy reportStrategy)
        {
            var reports = await _templatesRepository
                .All()
                .ByStrategy(reportStrategy)
                .IncludeReportMetrics()
                .ToListAsync();

            return reports;
        }

        /// <summary>
        /// <see cref="IReportTemplateService.GetByStrategiesAsync"/>
        /// </summary>
        /// <param name="reportStrategies"></param>
        /// <returns></returns>
        public async Task<List<ReportTemplate>> GetByStrategiesAsync(ReportStrategy[] reportStrategies)
        {
            var reports = await _templatesRepository
                .All()
                .ByStrategies(reportStrategies)
                .IncludeReportMetrics()
                .ToListAsync();

            return reports;
        }
    }
}