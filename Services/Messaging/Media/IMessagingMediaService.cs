using System.Collections.Generic;
using System.Threading.Tasks;

namespace WildHealth.Application.Services.Messaging.Media
{
    /// <summary>
    /// Provides methods for work with conversations media
    /// </summary>
    public interface IMessagingMediaService
    {
        /// <summary>
        /// Returns media resource download link
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="mediaSid"></param>
        /// <returns></returns>
        Task<string> RetrieveMediaResourceLinkAsync(int practiceId, string mediaSid);

        /// <summary>
        /// Returns media resource download links
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="mediaSid"></param>
        /// <returns></returns>
        Task<IDictionary<string, string>> RetrieveMediaResourceLinksAsync(int practiceId, string[] mediaSid);
    }
}
