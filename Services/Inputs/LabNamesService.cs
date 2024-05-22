using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// <see cref="ILabNamesService"/>
    /// </summary>
    public class LabNamesService : ILabNamesService
    {
        
        private readonly IGeneralRepository<LabName> _labNamesRepository;

        public LabNamesService(
            IGeneralRepository<LabName> labNamesRepository
            )
        {
            _labNamesRepository = labNamesRepository;
        }
        
        /// <summary>
        /// <see cref="ILabNamesService.All"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<LabName>> All()
        {
            return await _labNamesRepository
                .All()
                .ToArrayAsync();
        }
        
        /// <summary>
        /// <see cref="ILabNamesService.Create"/>
        /// </summary>
        /// <param name="labName"></param>
        /// <returns></returns>
        public async Task<LabName> Create(LabName labName)
        {
            await _labNamesRepository.AddAsync(labName);

            await _labNamesRepository.SaveAsync();

            return labName;
        }

        
        /// <summary>
        /// <see cref="ILabNamesService.Update"/>
        /// </summary>
        /// <param name="labName"></param>
        /// <returns></returns>
        public async Task<LabName> Update(LabName labName)
        {
            _labNamesRepository.Edit(labName);

            await _labNamesRepository.SaveAsync();

            return labName;
        }

        /// <summary>
        /// <see cref="ILabNamesService.Get"/>
        /// </summary>
        /// <param name="wildHealthName"></param>
        /// <returns></returns>
        public async Task<LabName?> Get(string wildHealthName)
        {
            return await _labNamesRepository
                .All()
                .IncludeLabNameRanges()
                .ByWildHealthName(wildHealthName)
                .FirstOrDefaultAsync();
        }
    }
}
        