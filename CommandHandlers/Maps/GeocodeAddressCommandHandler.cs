using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Maps;
using WildHealth.Common.Options;
using WildHealth.Google.Maps.Credentials;
using WildHealth.Google.Maps.Models;
using WildHealth.Google.Maps.WebClients;

namespace WildHealth.Application.CommandHandlers.Maps;

public class GeocodeAddressCommandHandler : IRequestHandler<GeocodeAddressCommand, GoogleGeocodeResponse>
{
    private readonly IGoogleMapsGeocodingWebClient _webClient;
    private readonly IOptions<MapGeocodeOptions> _options;

    public GeocodeAddressCommandHandler(
        IGoogleMapsGeocodingWebClient webClient,
        IOptions<MapGeocodeOptions> options)
    {
        _webClient = webClient;
        _options = options;
    }

    public async Task<GoogleGeocodeResponse> Handle(GeocodeAddressCommand command, CancellationToken cancellationToken)
    {
        var apiKey = _options.Value.ApiKey;
        var url = _options.Value.Url;
        
        _webClient.Initialize(new CredentialsModel(apiKey, url));
        
        return await _webClient.Geocode(command.Address);
    }
}