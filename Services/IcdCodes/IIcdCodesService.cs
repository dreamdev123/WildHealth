using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.IcdCodes;

namespace WildHealth.Application.Services.IcdCodes
{
    /// <summary>
    /// Provides methods for working with ICD Codes
    /// </summary>
    public interface IIcdCodesService
    {
        /// <summary>
        /// Returns ICD codes by search query
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        Task<IcdCode[]> GetByQueryAsync(string searchQuery);
    }
}
