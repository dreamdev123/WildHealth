using System;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Utils.LabNameRangeProvider
{
    /// <summary>
    /// Provides methods for helping get lab name range information
    /// </summary>
    public class LabNameRangeProvider : ILabNameRangeProvider
    {
        /// <summary>
        /// Returns age unit and age information for a given birthday
        /// </summary>
        /// <param name="birthday"></param>
        /// <returns></returns>
        public (string ageUnit, int age) GetAgeInfo(DateTime birthday)
        {
            var zeroTime = new DateTime(1, 1, 1);
            var span = DateTime.Now - birthday;
            var spanTime = zeroTime + span; 
            var years = spanTime.Year - 1;
            var ageUnit = years > 0 ? "Year" : "Month";
            var age = ageUnit == "Year" ? years : spanTime.Month;

            return (ageUnit, age);
        }

        /// <summary>
        /// Parses dimension, from, and to information from a reference range
        /// </summary>
        /// <param name="referenceRange"></param>
        /// <returns></returns>
        public (decimal? from, decimal? to, LabRangeType labRangeType) ParseRangeInfo(string referenceRange)
        {
            var referenceRangeTrimmed = referenceRange.Trim();

            if(referenceRangeTrimmed.IndexOf("<=") > -1) {
                return (null, Convert.ToDecimal(referenceRangeTrimmed.Split("<=")[1]), LabRangeType.LessThanOrEqual);
            } 
            else if(referenceRangeTrimmed.IndexOf(">=") > -1) {
                return (null, Convert.ToDecimal(referenceRangeTrimmed.Split(">=")[1]), LabRangeType.MoreThanOrEqual);
            }
            if(referenceRangeTrimmed.IndexOf("<") > -1) {
                return (null, Convert.ToDecimal(referenceRangeTrimmed.Split("<")[1]), LabRangeType.LessThen);
            } 
            else if(referenceRangeTrimmed.IndexOf(">") > -1) {
                return (null, Convert.ToDecimal(referenceRangeTrimmed.Split(">")[1]), LabRangeType.MoreThen);
            }
            else if(referenceRangeTrimmed.IndexOf("-") > -1) {
                var split = referenceRangeTrimmed.Split("-");
                return (Convert.ToDecimal(split[0]), Convert.ToDecimal(split[1]), LabRangeType.FromTo);
            }
            else {

                // Known to have a value of "Not Estab." - this will be accounted for here
                return (null, null, LabRangeType.None);
            }
        }

        /// <summary>
        /// Returns the availbale range values from a LabInputValue or LabInput
        /// </summary>
        /// <param name="labInputValue"></param>
        /// <returns> labInputRange </returns>
        public LabInputRange GetRange(LabInputValue labInputValue)
        {
            return labInputValue.GetPriorityRange();
        }
    }
}


