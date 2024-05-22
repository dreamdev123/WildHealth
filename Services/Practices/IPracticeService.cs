using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Licensing.Api.Models.Practices;
using WildHealth.Shared.Data.Helpers;

namespace WildHealth.Application.Services.Practices
{
    /// <summary>
    /// Provides service for working with practices
    /// </summary>
    public interface IPracticeService
    {
        /// <summary>
        /// Returns original practice by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<PracticeModel> GetOriginalPractice(int id);
        
        /// <summary>
        /// Returns practice by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Practice> GetAsync(int id);

        /// <summary>
        /// Returns practice by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<Practice> GetAsync(int id, ISpecification<Practice> specification);

        /// <summary>
        /// Returns practice by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Practice> GetSpecAsync(int id, ISpecification<Practice> specification);

        /// <summary>
        /// Returns all active practices
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Practice>> GetActiveAsync();

         /// <summary>
        /// <see cref="IPracticeService.GetAllAsync()"/>
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Practice>> GetAllAsync();

        /// <summary>
        /// Creates new practice
        /// </summary>
        /// <param name="practice"></param>
        /// <returns></returns>
        Task<Practice> CreateAsync(Practice practice);

        /// <summary>
        /// Updates existing practice
        /// </summary>
        /// <param name="practice"></param>
        /// <returns></returns>
        Task<Practice> UpdateAsync(Practice practice);

        /// <summary>
        /// <see cref="IPracticeService.InvalidateCache"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        void InvalidateCache(int id);

        /// <summary>
        /// <see cref="IPracticeService.GetPatientCount"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<int> GetPatientCount(int id);
    }
}