using System.Collections.Generic;
using System.Threading.Tasks;

namespace WildHealth.Application.Services.Links
{
    public interface ILinkShortenService
    {
        Task<string> ShortenAsync(string url, IEnumerable<string>? tags);
        //{
        //  "url": "https://www.example.com/my-really-long-link-that-I-need-to-shorten/84378949",
        //  "domain": "tiny.one",
        //  "alias": "myexamplelink",
        //  "tags": "example,link",
        //  "expires_at": "2024-10-25 10:11:12"
        //}
    }
}
