using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Domain.PreauthorizeRequests.Commands;
using WildHealth.Domain.Entities.Users;
using MediatR;

namespace WildHealth.Application.Domain.PreauthorizeRequests.CommandHandlers;

public class CreatePreauthorizeRequestsBulkCommandHandler : IRequestHandler<CreatePreauthorizeRequestsBulkCommand, PreauthorizeRequest[]>
{
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public CreatePreauthorizeRequestsBulkCommandHandler(
        IMediator mediator, 
        ILogger<CreatePreauthorizeRequestsBulkCommandHandler> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<PreauthorizeRequest[]> Handle(CreatePreauthorizeRequestsBulkCommand request, CancellationToken cancellationToken)
    {
        var results = new List<PreauthorizeRequest>();

        foreach (var command in request.Commands)
        {
            try
            {
                var result = await _mediator.Send(command, cancellationToken);

                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during creating preauthorized request for [Email] = {0}. {1}", command.Email, ex);
            }
        }

        return results.ToArray();
    }
}