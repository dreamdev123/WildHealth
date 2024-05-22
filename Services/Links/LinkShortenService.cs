using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.TinyURL.Clients.Models;
using WildHealth.TinyURL.Clients.Web;

namespace WildHealth.Application.Services.Links
{
    public class LinkShortenService : ILinkShortenService
    {
        private readonly ITinyUrlWebClient _tinyUrl;
        private readonly ILogger<LinkShortenService> _logger;

        public LinkShortenService(ITinyUrlWebClient tinyUrl,
                                  ILogger<LinkShortenService> logger)
        {
            _tinyUrl = tinyUrl;
            _logger = logger;
        }
        
        public async Task<string> ShortenAsync(string url, IEnumerable<string>? tags)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                var m = "The URL cannot be null or empty";
                _logger.LogError(m);
                throw new InvalidOperationException(m);
            }
            
            _logger.LogInformation($"Creating short url for {url}");
            var mytags = tags == null ? Enumerable.Empty<string>() : tags;
            var request = new CreateRequest()
            {
                Url = url,
                Tags = String.Join(",", mytags)
            };
            var res = await _tinyUrl.CreateShortUrlAsync(request);
            return res.Url!;
        }
    }
}