using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models._Base;
using WildHealth.Common.Models.Vitals;
using WildHealth.Domain.Entities.Vitals;
using WildHealth.Domain.Enums.Vitals;

namespace WildHealth.Application.Services.Vitals
{
    /// <summary>
    /// Represent a type for vital functionality.
    /// </summary>
    public interface IVitalService
    {
        /// <summary>
        /// Get latest added vital with values.
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IDictionary<string, VitalDetailsModel>> GetLatestAsync(int patientId);

        /// <summary>
        /// Check is Date added less than max per one date times.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        Task AssertDateAsync(int patientId, DateTime date);

        /// <summary>
        /// Get vitals by filter.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <param name="startRange"></param>
        /// <param name="endRange"></param>
        /// <returns></returns>
        Task<PaginationModel<VitalModel>> GetAsync(int patientId, int page, int pageSize, DateTime? startRange, DateTime? endRange);

        /// <summary>
        /// Return Vitals average by date range
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="dateRangeType"></param>
        /// <returns></returns>
        Task<ICollection<VitalModel>> GetByDateRangeAsync(int patientId, VitalsDateRangeType dateRangeType);

        /// <summary>
        /// Create vital value data set (for every vital will be created value with specified date).
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="dateTime"></param>
        /// <param name="sourceType"></param>
        /// <returns></returns>
        Task<ICollection<Vital>> CreateVitalsValueDataSetAsync(int patientId, DateTime dateTime, VitalValueSourceType sourceType);

        /// <summary>
        /// Create vitals with values.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="vitals"></param>
        /// <returns></returns>
        Task<ICollection<Vital>> CreateAsync(int patientId, ICollection<CreateVitalModel> vitals);

        /// <summary>
        /// Delete vital values by the date.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="vitalsValuesIds"></param>
        /// <returns></returns>
        Task<ICollection<VitalValue>> DeleteVitalValuesAsync(int patientId, int[] vitalsValuesIds);

        /// <summary>
        /// Update vital values.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="vitals"></param>
        /// <returns></returns>
        Task<ICollection<VitalValue>> UpdateVitalValueAsync(int patientId, ICollection<UpdateVitalValueModel> vitals);
        
        /// <summary>
        /// Create vitals with values.
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="vitals"></param>
        /// <returns></returns>
        Task ParseAsync(int patientId, ICollection<CreateVitalModel> vitals);
    }
}
