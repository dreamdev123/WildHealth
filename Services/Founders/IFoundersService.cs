using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Services.Founders
{
    /// <summary>
    /// Provides methods fro working with founders
    /// </summary>
    public interface IFoundersService
    {
        /// <summary>
        /// Returns founder by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Founder> GetByIdAsync(int id);
        
        /// <summary>
        /// Returns active founders
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<IEnumerable<Founder>> GetActiveAsync(int practiceId);
    }
}