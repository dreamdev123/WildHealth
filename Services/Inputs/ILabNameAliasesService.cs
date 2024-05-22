using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Inputs;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// Provides methods for working with lab name aliases
    /// </summary>
    public interface ILabNameAliasesService
    {
        /// <summary>
        /// Returns all LabNameAlias entries
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<LabNameAlias>> All();


        /// <summary>
        /// Creates a LabNameAlias record
        /// </summary>
        /// <param name="labNameAlias"></param>
        /// <returns></returns>
        Task<LabNameAlias> Create(LabNameAlias labNameAlias);


        /// <summary>
        /// Gets a LabNameAlias record by a given vendor and LabName.  There's a chance multiple aliases exist, we will just return the first
        /// </summary>
        /// <param name="vendorId"></param>
        /// <param name="labNameId"></param>
        /// <returns></returns>
        Task<LabNameAlias?> GetByVendorAndLabName(int vendorId, int labNameId);

        /// <summary>
        /// Helper method that either gets the first LabNameAlias that exists for the vendor/LabName.  If none exist, it creates a LabNameAlias and returns that new model.
        /// </summary>
        /// <param name="vendorId"></param>
        /// <param name="labNameId"></param>
        /// <param name="resultCode"></param>
        /// <returns></returns>
        Task<LabNameAlias> GetOrCreate(int vendorId, int labNameId, string resultCode);
    }
}