using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Application.Services.Inputs;
using WildHealth.Shared.DistributedCache.Services;

namespace WildHealth.Application.Utils.LabNameProvider
{
    /// <summary>
    /// <see cref="ILabNameProvider"/>
    /// </summary>
    public class LabNameProvider : ILabNameProvider
    {
        private readonly IWildHealthSpecificCacheService<LabNameProvider, IDictionary<string, LabNameAlias>> _resultCodesCache;
        private readonly IWildHealthSpecificCacheService<LabNameProvider, IEnumerable<string>> _groupNamesCache;
        private readonly IWildHealthSpecificCacheService<LabNameProvider, IDictionary<string, IEnumerable<LabName>>> _groupsCache;
        private readonly ILabNamesService _labNamesService;
        private readonly ILabNameAliasesService _labNameAliasesService;
        private readonly string _resultCodesMapKey = "ResultCodesMap";
        private readonly string _groupNamesKey = "GroupNames";
        public LabNameProvider(
            IWildHealthSpecificCacheService<LabNameProvider, IDictionary<string, LabNameAlias>> resultCodesCache,
            IWildHealthSpecificCacheService<LabNameProvider, IEnumerable<string>> groupNamesCache,
            IWildHealthSpecificCacheService<LabNameProvider, IDictionary<string, IEnumerable<LabName>>> groupsCache,
            ILabNamesService labNamesService,
            ILabNameAliasesService labNameAliasesService
            )
        {
            _resultCodesCache = resultCodesCache;
            _groupNamesCache = groupNamesCache;
            _labNamesService = labNamesService;
            _labNameAliasesService = labNameAliasesService;
            _groupsCache = groupsCache;
        }


        public async Task<LabNameAlias?> LabNameAliasForResultCode(string code)
        {
            var map = await ResultCodesMap();

            if(map.ContainsKey(code))
            {
                return map[code];
            }

            return null;
        }

        public async Task<string?> WildHealthNameForResultCode(string code)
        {
            return await GetStringFromLabName(code, (LabNameAlias labNameAlias) => labNameAlias.LabName.WildHealthName);
        }

        public async Task<string?> WildHealthDisplayNameForResultCode(string code)
        {
            return await GetStringFromLabName(code, (LabNameAlias labNameAlias) => labNameAlias.LabName.WildHealthDisplayName);
        }

        public async Task<IEnumerable<string>> GroupNames()
        {
            return await _groupNamesCache.GetAsync(_groupNamesKey, async () => {
                return (await ResultCodesMap()).GroupBy(o => o.Value.LabName.GroupName).Select(o => o.Key);
            });
        }

        /// <summary>
        /// Returns the list of groups as keys and associated LabNames as values
        /// </summary>\
        /// <returns></returns>
        public async Task<IDictionary<string, IEnumerable<LabName>>> Groups()
        {
            return await _groupsCache.GetAsync("Groups", async () => {
                return (await ResultCodesMap()).GroupBy(o => o.Value.LabName.GroupName).ToDictionary(o => o.Key, o => o.ToArray().Select(o => o.Value.LabName));
            });
        }


        /// <summary>
        // Returns a dictionary where keys are result codes and values are LabName objects
        // This contains result codes for all vendors (big assumption here is that no 2 vendors have the exact same result codes)
        /// </summary>
        /// <returns></returns>
        private async Task<IDictionary<string, LabNameAlias>> ResultCodesMap()
        {
            return await _resultCodesCache.GetAsync(_resultCodesMapKey, async () => {
                var list = await _labNameAliasesService.All();
                return list.ToDictionary(o => o.ResultCode, o => o);
            });
        }

        private async Task<string?> GetStringFromLabName(string code, Func<LabNameAlias, string> del)
        {
            string? result = null;

            var map = await ResultCodesMap();

            if(map.ContainsKey(code))
            {
                var labName = map[code];

                result = del(labName);
            }

            return result;
        }

        public void ResetResultCodesMap()
        {
            _resultCodesCache.RemoveKey(_resultCodesMapKey);
            _groupNamesCache.RemoveKey(_groupNamesKey);
        }
    }
}