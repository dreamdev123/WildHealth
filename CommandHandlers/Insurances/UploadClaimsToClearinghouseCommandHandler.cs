using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Integration.Factories.IntegrationServiceFactory;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class UploadClaimsToClearinghouseCommandHandler : IRequestHandler<UploadClaimsToClearinghouseCommand>
{
    private readonly IClearinghouseIntegrationServiceFactory _clearinghouseIntegrationServiceFactory;
    private readonly ILogger<UploadClaimsToClearinghouseCommandHandler> _logger;

    public UploadClaimsToClearinghouseCommandHandler(
        IClearinghouseIntegrationServiceFactory clearinghouseIntegrationServiceFactory,
        ILogger<UploadClaimsToClearinghouseCommandHandler> logger)
    {
        _clearinghouseIntegrationServiceFactory = clearinghouseIntegrationServiceFactory;
        _logger = logger;
    }

    public async Task Handle(UploadClaimsToClearinghouseCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Uploading {command.Claims.Length} claims for practice id = {command.PracticeId} to clearinghouse has: started");

        var clearinghouseService = await _clearinghouseIntegrationServiceFactory.CreateAsync(command.PracticeId);

        await clearinghouseService.UploadClaimsAsync(
            claims: command.Claims,
            practiceId: command.PracticeId);
            
        _logger.LogInformation($"Uploading {command.Claims.Length} claims for practice id = {command.PracticeId} to clearinghouse has: finished");
    }
}