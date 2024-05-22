using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Inputs;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// Provides methods for working with lab names
    /// </summary>
    public interface ILabNamesService
    {
        /// <summary>
        /// Returns all LabName entries
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<LabName>> All();


        /// <summary>
        /// <see cref="ILabNamesService.Create"/>
        /// </summary>
        /// <param name="labName"></param>
        /// <returns></returns>
        Task<LabName> Create(LabName labName);

        /// <summary>
        /// <see cref="ILabNamesService.Update"/>
        /// </summary>
        /// <param name="labName"></param>
        /// <returns></returns>
        Task<LabName> Update(LabName labName);

        /// <summary>
        /// <see cref="ILabNamesService.Get"/>
        /// </summary>
        /// <param name="wildHealthName"></param>
        /// <returns></returns>
        Task<LabName?> Get(string wildHealthName);
    }
}