using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Domain.Enums.User;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Application.Utils.LabNameRangeProvider;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// <see cref="ILabNameRangesService"/>
    /// </summary>
    public class LabNameRangesService : ILabNameRangesService
    {
        
        private readonly IGeneralRepository<LabNameRange> _labNameRangesRepository;
        private readonly ILabNameRangeProvider _labNameRangeProvider;

        public LabNameRangesService(
            IGeneralRepository<LabNameRange> labNameRangesRepository,
            ILabNameRangeProvider labNameRangeProvider
            )
        {
            _labNameRangesRepository = labNameRangesRepository;
            _labNameRangeProvider = labNameRangeProvider;
        }
        
        /// <summary>
        /// <see cref="ILabNameRangesService.Create"/>
        /// </summary>
        /// <param name="labNameRange"></param>
        /// <returns></returns>
        public async Task<LabNameRange> Create(LabNameRange labNameRange)
        {
            await _labNameRangesRepository.AddAsync(labNameRange);

            await _labNameRangesRepository.SaveAsync();

            return labNameRange;
        }

        /// <summary>
        /// <see cref="ILabNameRangesService.Get"/>
        /// </summary>
        /// <param name="labName"></param>
        /// <param name="labVendor"></param>
        /// <param name="gender"></param>
        /// <param name="birthday"></param>
        /// <returns></returns>
        public async Task<LabNameRange?> Get(LabName labName, LabVendor labVendor, Gender gender, DateTime birthday)
        {
            return await _labNameRangesRepository
                .All()
                .ByVendor(labVendor)
                .ByLabName(labName)
                .ByGender(gender)
                .ByBirthday(birthday)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets ranges for the labName and labVendor
        /// </summary>
        /// <param name="labName"></param>
        /// <param name="labVendor"></param>
        /// <returns></returns>
        public async Task<IEnumerable<LabNameRange>> Get(LabName labName, LabVendor labVendor)
        {
            return await _labNameRangesRepository
                .All()
                .ByVendor(labVendor)
                .ByLabName(labName)
                .AsNoTracking()
                .ToArrayAsync();
        }

        public async Task<LabNameRange> GetOrCreate(LabName labName, LabVendor labVendor, Gender gender, DateTime birthday, string dimension, string rangeString)
        {
            var labNameRange = await Get(labName, labVendor, gender, birthday);

            if(labNameRange == null && !String.IsNullOrEmpty(dimension))
            {
                var (ageUnit, age) = _labNameRangeProvider.GetAgeInfo(birthday);
                var (from, to, labRangeType) = _labNameRangeProvider.ParseRangeInfo(rangeString);

                // Create the LabNameRange
                labNameRange = await Create(new LabNameRange() {
                    LabVendorId = labVendor.GetId(),
                    Gender = gender,
                    AgeUnit = ageUnit,
                    Age = age,
                    RangeFrom = from,
                    RangeTo = to,
                    RangeType = labRangeType,
                    RangeDimension = dimension,
                    LabNameId = labName.GetId()
                });
            }

            return labNameRange!;
        }
    }
}
        