using System.Threading.Tasks;
using WildHealth.Domain.Entities.Reports;

namespace WildHealth.Application.Services.Reports.Celiac
{
    public interface ICeliacReportService
    {   
        /// <summary>   
        /// Gets latest Celiac Report for patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<PatientReport> GetLatestAsync(int patientId);

        /// <summary>   
        /// Creates new Celiac Report for patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<PatientReport> CreateAsync(int patientId);
        
    }
}