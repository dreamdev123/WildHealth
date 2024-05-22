using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Domain.Entities.Orders;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Orders.Referral;
using WildHealth.Application.CommandHandlers.Orders.Flows;
using WildHealth.Application.Functional.Flow;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders;

public class DeleteReferralOrderCommandHandler : IRequestHandler<DeleteReferralOrderCommand, ReferralOrder>
{
    private readonly IFlowMaterialization _materializeFlow;
    private readonly IReferralOrdersService _referralOrdersService;
    private readonly ILogger _logger;


    public DeleteReferralOrderCommandHandler(
        IFlowMaterialization materializeFlow, 
        IReferralOrdersService referralOrdersService, 
        ILogger<DeleteReferralOrderCommandHandler> logger)
    {
        _materializeFlow = materializeFlow;
        _referralOrdersService = referralOrdersService;
        _logger = logger;
    }

    public async Task<ReferralOrder> Handle(DeleteReferralOrderCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Deleting Other order with id: {command.Id} has been started.");

        var order = await _referralOrdersService.GetAsync(command.Id);
        
        var flow = new DeleteReferralOrderFlow(order);
        
        order = await flow
            .Materialize(_materializeFlow.Materialize)
            .Select<ReferralOrder>();
        
        _logger.LogInformation($"Deleting Other order with id: {command.Id} has been finished.");

        return order;
    }
}