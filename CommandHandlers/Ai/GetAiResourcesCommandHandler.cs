using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Ai;
using WildHealth.Application.Services.Ai;
using WildHealth.Common.Models.Ai;

namespace WildHealth.Application.CommandHandlers.Ai;

public class GetAiResourcesCommandHandler : IRequestHandler<GetAiResourcesCommand, AiResourceModel[]>
{
    private readonly IAiResourceService _aiResourceService;
    private readonly ILogger<GetAiResourcesCommandHandler> _logger;

    public GetAiResourcesCommandHandler(IAiResourceService aiResourceService, ILogger<GetAiResourcesCommandHandler> logger)
    {
        _aiResourceService = aiResourceService;
        _logger = logger;
    }

    public async Task<AiResourceModel[]> Handle(GetAiResourcesCommand command, CancellationToken cancellationToken)
    {
        var resources = new List<AiResourceModel>();

        foreach (var model in command.Models)
        {
            try
            {
                var resource =
                    await _aiResourceService.GetAiResourceAsync(Guid.Parse(model.UniversalId), model.ResourceType);

                resources.Add(resource);
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to fetch AI Resource with [Guid] = {model.UniversalId} and [ResourceType] = {model.ResourceType}: {e}");
            }
        }

        return resources.ToArray();
    }
}