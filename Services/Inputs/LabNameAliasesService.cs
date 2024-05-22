
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// <see cref="ILabNameAliasesService"/>
    /// </summary>
    public class LabNameAliasesService : ILabNameAliasesService
    {
        
        private readonly IGeneralRepository<LabNameAlias> _labNameAliasRepository;

        public LabNameAliasesService(
            IGeneralRepository<LabNameAlias> labNameAliasRepository
            )
        {
            _labNameAliasRepository = labNameAliasRepository;
        }
        
        /// <summary>
        /// <see cref="ILabNameAliasesService.All"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<LabNameAlias>> All()
        {
            return await _labNameAliasRepository
                .All()
                .Include(o => o.LabName)
                .ToArrayAsync();
        }
        
        /// <summary>
        /// <see cref="ILabNameAliasService.Create"/>
        /// </summary>
        /// <param name="labNameAlias"></param>
        /// <returns></returns>
        public async Task<LabNameAlias> Create(LabNameAlias labNameAlias)
        {
            await _labNameAliasRepository.AddAsync(labNameAlias);

            await _labNameAliasRepository.SaveAsync();

            return labNameAlias;
        }

        /// <summary>
        /// <see cref="ILabNameAliasService.GetByName"/>
        /// </summary>
        /// <returns></returns>
        public async Task<LabNameAlias?> GetByVendorAndLabName(int vendorId, int labNameId)
        {
            return await _labNameAliasRepository
                .All()
                .ByVendorId(vendorId)
                .ByLabNameId(labNameId)
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task<LabNameAlias> GetOrCreate(int vendorId, int labNameId, string resultCode)
        {
            // Create the alias
            var labNameAliasObject = await GetByVendorAndLabName(vendorId, labNameId);

            if(labNameAliasObject == null || labNameAliasObject.ResultCode != resultCode) {
                labNameAliasObject = await Create(new LabNameAlias() {
                    LabVendorId = vendorId,
                    ResultCode = resultCode,
                    LabNameId = labNameId
                });
            }

            return labNameAliasObject;
        }
    }
}
        