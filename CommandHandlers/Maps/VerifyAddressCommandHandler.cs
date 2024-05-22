using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WildHealth.Application.Commands.Maps;
using WildHealth.Common.Options;
using WildHealth.Lob.Clients.Credentials;
using WildHealth.Lob.Clients.Models;
using WildHealth.Lob.Clients.WebClients;

namespace WildHealth.Application.CommandHandlers.Maps;

public class VerifyAddressCommandHandler : IRequestHandler<VerifyAddressCommand, LobVerifyAddressResponseModel>
{
    private readonly IServiceProvider _services;
    private readonly IOptions<AddressVerificationOptions> _options;

    public VerifyAddressCommandHandler(
        IServiceProvider services,
        IOptions<AddressVerificationOptions> options)
    {
        _services = services;
        _options = options;
    }

    public async Task<LobVerifyAddressResponseModel> Handle(VerifyAddressCommand command, CancellationToken cancellationToken)
    {
        var scope = _services.CreateScope();
        var scopedWebClient = scope.ServiceProvider.GetRequiredService<ILobAddressVerificationWebClient>();
        var url = _options.Value.Url;
        var apiKey = _options.Value.ApiKey;
        
        scopedWebClient.Initialize(new CredentialsModel(apiKey, url));

        if (!string.IsNullOrEmpty(command.FullAddress))
        {
            return await scopedWebClient.VerifyAddress(fullAddress: command.FullAddress);
        }

        return await scopedWebClient.VerifyAddress(
            streetAddress1: command.StreetAddress1,
            streetAddress2: command.StreetAddress2,
            city: command.City,
            state: command.StateAbbreviation,
            zipCode: command.ZipCode);
    }
}