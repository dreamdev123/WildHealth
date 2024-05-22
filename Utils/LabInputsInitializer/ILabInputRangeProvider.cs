using System;
using System.Threading.Tasks;
using WildHealth.Domain.Enums.User;
using WildHealth.Domain.Entities.Inputs;

namespace WildHealth.Application.Utils.LabInputsInitializer
{
    /// <summary>
    /// Provides mapper between test codes and lab names
    /// </summary>
    public interface ILabInputRangeProvider
    {
        /// <summary>
        /// Returns lab input range for given wild health lab name
        /// </summary>
        /// <param name="vendor"></param>
        /// <param name="withHealthLabName"></param>
        /// <param name="gender"></param>
        /// <param name="birthday"></param>
        /// <returns></returns>
        Task<LabInputRange> GetRange(string wildHealthLabName, LabVendor vendor, Gender gender, DateTime birthday);
    }
}