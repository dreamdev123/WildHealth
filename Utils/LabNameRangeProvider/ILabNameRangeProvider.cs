using System;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Entities.Inputs;

namespace WildHealth.Application.Utils.LabNameRangeProvider
{
    /// <summary>
    /// Provides methods for helping get lab name range information
    /// </summary>
    public interface ILabNameRangeProvider
    {
        /// <summary>
        /// Returns age unit and age information for a given birthday
        /// </summary>
        /// <param name="birthday"></param>
        /// <returns>ageUnit and age information for the given birthday</returns>
        (string ageUnit, int age) GetAgeInfo(DateTime birthday);

        /// <summary>
        /// Parses dimension, from, and to information from a reference range
        /// </summary>
        /// <param name="referenceRange"></param>
        /// <returns>from, to, and labRange type information</returns>
        (decimal? from, decimal? to, LabRangeType labRangeType) ParseRangeInfo(string referenceRange);

        /// <summary>
        /// Returns the availbale range values from a LabInputValue or LabInput
        /// </summary>
        /// <param name="labInputValue"></param>
        /// <returns> labInputRange </returns>
        LabInputRange GetRange(LabInputValue labInputValue);
    }
}