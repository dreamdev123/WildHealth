using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Domain.Entities.Inputs;

namespace WildHealth.Application.Utils.LabNameProvider
{
    /// <summary>
    /// Provides mapper between test codes and lab names
    /// </summary>
    public interface ILabNameProvider
    {
        /// <summary>
        /// Returns the LabName for the given result code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<LabNameAlias?> LabNameAliasForResultCode(string code);

        /// <summary>
        /// Returns the WildHealth name of the lab with the corresponding result code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<string?> WildHealthNameForResultCode(string code);

        /// <summary>
        /// Returns the WildHealth display name of the lab with the corresponding result code
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<string?> WildHealthDisplayNameForResultCode(string code);

        /// <summary>
        /// Returns the list of groups
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<string>> GroupNames();

        /// <summary>
        /// Returns the list of groups as keys and associated LabNames as values
        /// </summary>
        /// <returns></returns>
        Task<IDictionary<string, IEnumerable<LabName>>> Groups();


        /// <summary>
        /// Resets the cache
        /// </summary>
        /// <returns></returns>
        void ResetResultCodesMap();
    }
}