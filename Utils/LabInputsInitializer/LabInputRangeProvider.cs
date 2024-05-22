using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.User;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Application.Services.Inputs;
using Microsoft.Extensions.Logging;

namespace WildHealth.Application.Utils.LabInputsInitializer
{
    /// <summary>
    /// <see cref="ILabInputRangeProvider"/>
    /// </summary>
    public class LabInputRangeProvider : ILabInputRangeProvider
    {
        private readonly ILogger<LabInputRangeProvider> _logger;
        private readonly ILabNameRangesService _labNameRangesService;
        private readonly ILabNamesService _labNamesService;

        public LabInputRangeProvider(
            ILogger<LabInputRangeProvider> logger,
            ILabNameRangesService labNameRangesService,
            ILabNamesService labNamesService
            )
        {
            _logger = logger;
            _labNameRangesService = labNameRangesService;
            _labNamesService = labNamesService;
        }


        /// <summary>
        /// Returns lab input range for given wild health lab name
        /// </summary>
        /// <param name="withHealthLabName"></param>
        /// <returns></returns>
        public async Task<LabInputRange> GetRange(string wildHealthLabName, LabVendor vendor, Gender gender, DateTime birthday)
        {
            var labInputRange = new LabInputRange(
                type: LabRangeType.None,
                dimension: null,
                from: null,
                to: null
            );

            var labNameObject = await _labNamesService.Get(wildHealthLabName);

            var labNameRange = await _labNameRangesService.Get(labNameObject!, vendor, gender, birthday);

            if (labNameRange == null)
            {
                _logger.LogInformation($"Range for lab {wildHealthLabName} does not exist");
            }
            else
            {
                labInputRange = new LabInputRange(
                    type: labNameRange.RangeType,
                    dimension: labNameRange.RangeDimension,
                    from: labNameRange.RangeFrom,
                    to: labNameRange.RangeTo
                );
            }

            return labInputRange;
        }

        
    }
}


