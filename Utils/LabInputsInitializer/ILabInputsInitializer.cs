using System;
using WildHealth.Domain.Enums.User;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Inputs;

namespace WildHealth.Application.Utils.LabInputsInitializer
{
    /// <summary>
    /// Provides mapper between test codes and lab names
    /// </summary>
    public interface ILabInputsInitializer
    {
        /// <summary>
        /// Receives an InputsAggregator and initializes it with LabInputs if none are found
        /// </summary>
        /// <param name="aggregator"></param>
        /// <returns></returns>
        Task<InputsAggregator> Initialize(InputsAggregator aggregator, LabVendor vendor, Gender gender, DateTime birthday, bool? shouldForce);
    }
}