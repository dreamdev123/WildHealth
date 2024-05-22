using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// Provides methods for working with lab name ranges
    /// </summary>
    public interface ILabNameRangesService
    {
        /// <summary>
        /// <see cref="ILabNameRangesService.Create"/>
        /// </summary>
        /// <param name="labNameRange"></param>
        /// <returns></returns>
        Task<LabNameRange> Create(LabNameRange labNameRange);

        /// <summary>
        /// <see cref="ILabNameRangesService.Get"/>
        /// </summary>
        /// <param name="labName"></param>
        /// <param name="labVendor"></param>
        /// <param name="gender"></param>
        /// <param name="birthday"></param>
        /// <returns></returns>
        Task<LabNameRange?> Get(LabName labName, LabVendor labVendor, Gender gender, DateTime birthday);

        /// <summary>
        /// Gets ranges for the labName and labVendor
        /// </summary>
        /// <param name="labName"></param>
        /// <param name="labVendor"></param>
        /// <returns></returns>
        Task<IEnumerable<LabNameRange>> Get(LabName labName, LabVendor labVendor);


        /// <summary>
        /// <see cref="ILabNameRangesService.GetOrCreate"/>
        /// </summary>
        /// <param name="labName"></param>
        /// <param name="labVendor"></param>
        /// <param name="gender"></param>
        /// <param name="birthday"></param>
        /// <param name="dimension"></param>
        /// <param name="rangeString">This is the raw string that comes from CHC (i.e. 0.1-2.0 or <1000)</param>
        /// <returns></returns>
        Task<LabNameRange> GetOrCreate(LabName labName, LabVendor labVendor, Gender gender, DateTime birthday, string dimension, string rangeString);
    }
}