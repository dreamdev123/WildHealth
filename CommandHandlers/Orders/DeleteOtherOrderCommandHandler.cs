using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Orders.Flows;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Orders.Other;
using WildHealth.Domain.Entities.Orders;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders;

public class DeleteOtherOrderCommandHandler : IRequestHandler<DeleteOtherOrderCommand, OtherOrder>
{
    private readonly IFlowMaterialization _materializeFlow;
    private readonly IOtherOrdersService _otherOrdersService;
    private readonly ILogger _logger;

    public DeleteOtherOrderCommandHandler(
        IFlowMaterialization materializeFlow,
        IOtherOrdersService otherOrdersService, 
        ILogger<DeleteOtherOrderCommandHandler> logger)
    {
        _materializeFlow = materializeFlow;
        _otherOrdersService = otherOrdersService;
        _logger = logger;
    }

    public async Task<OtherOrder> Handle(DeleteOtherOrderCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Deleting Other order with id: {command.Id} has been started.");

        var order = await _otherOrdersService.GetAsync(command.Id);
        
        var flow = new DeleteOtherOrderFlow(order);
        
        order = await flow
            .Materialize(_materializeFlow.Materialize)
            .Select<OtherOrder>();
        
        _logger.LogInformation($"Deleting Other order with id: {command.Id} has been finished.");

        return order;
    }
}