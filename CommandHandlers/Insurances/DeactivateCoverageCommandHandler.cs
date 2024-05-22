using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Insurances.Flows;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Coverages;
using WildHealth.Domain.Entities.Insurances;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class DeactivateCoverageCommandHandler : IRequestHandler<DeactivateCoverageCommand, Coverage>
{
    private readonly ICoveragesService _coveragesService;
    private readonly MaterializeFlow _materializeFlow;
    private readonly ILogger _logger;

    public DeactivateCoverageCommandHandler(
        ICoveragesService coveragesService, 
        MaterializeFlow materializeFlow,
        ILogger<DeactivateCoverageCommandHandler> logger)
    {
        _coveragesService = coveragesService;
        _materializeFlow = materializeFlow;
        _logger = logger;
    }
    
    public async Task<Coverage> Handle(DeactivateCoverageCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Started deactivating of coverage with [Id] = {command.Id}");
        
        var coverage = await _coveragesService.GetAsync(command.Id);

        var flow = new DeactivateCoverageFlow(coverage);
        
        await flow.Materialize(_materializeFlow);
        
        _logger.LogInformation($"Finished deactivating of coverage with [Id] = {command.Id}");

        return coverage;
    }
}