using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Epigenetic;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Enums.AddOns;
using WildHealth.Domain.Enums.Orders;
using WildHealth.TrueDiagnostic.WebClients;
using WildHealth.Application.CommandHandlers.Orders.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders;

public class ProcessEpigeneticOrdersCommandHandler : IRequestHandler<ProcessEpigeneticOrdersCommand>
{
    private static readonly OrderStatus[] InterestingStatuses = {
        OrderStatus.Placed,
        OrderStatus.Shipping,
        OrderStatus.Arrived
    };
    
    private static readonly AddOnProvider[] AddOnProviders = {
        AddOnProvider.TrueDiagnostic
    };
    
    private readonly IEpigeneticOrdersService _epigeneticOrdersService;
    private readonly ITrueDiagnosticWebClient _trueDiagnosticWebClient;
    private readonly IFlowMaterialization _materializeFlow;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;

    public ProcessEpigeneticOrdersCommandHandler(
        IEpigeneticOrdersService epigeneticOrdersService, 
        ITrueDiagnosticWebClient trueDiagnosticWebClient, 
        IFlowMaterialization materializeFlow,
        IDateTimeProvider dateTimeProvider,
        ILogger<ProcessEpigeneticOrdersCommandHandler> logger)
    {
        _epigeneticOrdersService = epigeneticOrdersService;
        _trueDiagnosticWebClient = trueDiagnosticWebClient;
        _materializeFlow = materializeFlow;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task Handle(ProcessEpigeneticOrdersCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Started processing epigenetic orders");
        
        var orders = await _epigeneticOrdersService.SelectOrdersAsync(InterestingStatuses, AddOnProviders);
        
        _logger.LogInformation("Selected {0} epigenetic orders with interesting statuses", orders.Length);

        foreach (var order in orders)
        {
            try
            {
                _logger.LogInformation("Started processing epigenetic order with id {id}", order.GetId());
            
                var status = await _trueDiagnosticWebClient.GetOrderStatus(order.Number);

                var flow = new ProcessEpigeneticOrdersFlow(order, status, _dateTimeProvider.UtcNow());

                await flow.Materialize(_materializeFlow.Materialize);

                _logger.LogInformation("Epigenetic order with id {id} processed successfully.", order.GetId());
            }
            catch (Exception e)
            {
                _logger.LogError("Started processing epigenetic order with id {id}. {error}", order.GetId(), e);
            }
        }
    }
}