using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Reports;
using WildHealth.Domain.Enums.Reports;
using WildHealth.Common.Models.Reports;

namespace WildHealth.Application.Services.Reports.Template
{
    public interface IReportTemplateService
    {
        /// <summary>
        /// Returns most recent template
        /// </summary>
        /// <returns></returns>
        Task<ReportTemplate> GetLatest(ReportType reportType);
    
        /// <summary>
        /// Returns template by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ReportTemplate> GetByIdAsync(int id);

        /// <summary>
        /// creates template using provided paramters
        /// <param name="version"></param>
        /// <param name="comment"></param>
        /// <param name="template"></param>
        /// <param name="reportType"></param>
        /// </summary>
        /// <returns></returns>
        Task<ReportTemplate> Create(string version, string comment, string templateJson, ReportType reportType);

        /// <summary>
        /// Returns Report templates by ReportType
        /// </summary>
        /// <returns></returns>
        Task<ReportTemplate[]> GetTemplates(ReportType reportType);

        /// <summary>
        /// Returns an updated Report Template
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<ReportTemplate> UpdateAsync(ReportTemplateModel model);

        /// <summary>
        /// Returns a cloned Report Template
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<ReportTemplate> CloneAsync(int id);

        /// <summary>
        /// Deletes Template
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ReportTemplate> DeleteAsync(int id);

        /// <summary>
        /// Gets all report templates for a given strategy
        /// </summary>
        /// <param name="reportStrategy"></param>
        /// <returns></returns>
        Task<List<ReportTemplate>> GetByStrategyAsync(ReportStrategy reportStrategy);

        /// <summary>
        /// Gets all report templates for a given list of strategies
        /// </summary>
        /// <param name="reportStrategies"></param>
        /// <returns></returns>
        Task<List<ReportTemplate>> GetByStrategiesAsync(ReportStrategy[] reportStrategies);
    }
}