using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.IcdCodes;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.DistributedCache.Services;

namespace WildHealth.Application.Services.IcdCodes
{
    /// <summary>
    /// <see cref="IIcdCodesService"/>
    /// </summary>
    public class IcdCodesService: IIcdCodesService
    {
        private readonly IWildHealthSpecificCacheService<IcdCodesService, ICollection<IcdCode>> _cacheService;
        private readonly IGeneralRepository<IcdCode> _icdCodesRepository;
        
        public IcdCodesService(
            IWildHealthSpecificCacheService<IcdCodesService, ICollection<IcdCode>> cacheService,
            IGeneralRepository<IcdCode> icdCodesRepository)
        {
            _cacheService = cacheService;
            _icdCodesRepository = icdCodesRepository;
        }

        /// <summary>
        /// <see cref="IIcdCodesService.GetByQueryAsync"/>
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public async Task<IcdCode[]> GetByQueryAsync(string searchQuery)
        {
            // Call out to WHO to get synonyms
            
            // for each synonym, do the following:
            // select many at the end
            
            // high blood pressure
            // Essential hypertension
            
            var icdCodes = await _cacheService.GetAsync(
                key: nameof(IcdCode), 
                getter: async () => await _icdCodesRepository.All().ToArrayAsync()
            );

            var terms = searchQuery.Split(" ");

            var termResults = terms.AsParallel().Select(o => icdCodes.Where(x =>
                x.Code.Contains(o, StringComparison.OrdinalIgnoreCase) ||
                x.Description.Contains(o, StringComparison.OrdinalIgnoreCase)));

            return termResults
                .SelectMany(o => o)
                .GroupBy(o => o.Id)
                .Where(o => o.Count() == terms.Length)  //  { 132 -> [IcdCode,IcdCode,IcdCode] }
                .Select(o => o.First())
                .ToArray();
        }
    }
}
