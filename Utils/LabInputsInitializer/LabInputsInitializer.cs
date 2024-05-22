using System;
using System.Linq;
using System.Threading.Tasks;
using WildHealth.Application.Utils.LabNameProvider;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.User;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Application.Services.Inputs;

namespace WildHealth.Application.Utils.LabInputsInitializer
{
    /// <summary>
    /// <see cref="ILabInputsInitializer"/>
    /// </summary>
    public class LabInputsInitializer : ILabInputsInitializer
    {
        private readonly ILabNameProvider _labNameProvider;
        private readonly ILabInputRangeProvider _labInputRangeProvider;
        private readonly ILabNameRangesService _labNameRangesService;
        public LabInputsInitializer(
            ILabNameProvider labNameProvider,
            ILabInputRangeProvider labInputRangeProvider,
            ILabNameRangesService labNameRangesService
            )
        {
            _labNameProvider = labNameProvider;
            _labInputRangeProvider = labInputRangeProvider;
            _labNameRangesService = labNameRangesService;
        }

        /// <summary>
        /// Receives an InputsAggregator and initializes it with LabInputs if none are found
        /// </summary>
        /// <param name="aggregator"></param>
        /// <returns></returns>
        public async Task<InputsAggregator> Initialize(InputsAggregator aggregator, LabVendor vendor, Gender gender, DateTime birthday, bool? shouldForce)
        {
            if(!aggregator.LabInputs.Any() || (shouldForce.HasValue && shouldForce.Value))
            {
                var groups = await _labNameProvider.Groups();

                foreach (var (key, labNameObjects) in groups)
                {
                    if(key.Equals("Other")) {
                        continue;
                    }

                    foreach (var labNameObject in labNameObjects)
                    {
                        var name = labNameObject.WildHealthName;

                        // If we already have it then skip over this
                        if(aggregator.LabInputs.Any(o => o.Name.Equals(name))) {
                            continue;
                        }

                        var labInputRange = await _labInputRangeProvider.GetRange(name, vendor, gender, birthday);

                        var input = new LabInput(
                            name: name,
                            group: Enum.Parse<LabGroup>(key),
                            range: labInputRange
                        );

                        aggregator.LabInputs.Add(input);
                    }
                }
            }

            return aggregator;
        }
    }
}