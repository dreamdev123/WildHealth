using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Constants;


namespace WildHealth.Application.CommandHandlers.Orders
{
    public class DeleteDnaOrderShippingCommandHandler : IRequestHandler<DeleteDnaOrderShippingCommand, DnaOrder>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public DeleteDnaOrderShippingCommandHandler(
            IDnaOrdersService dnaOrdersService,
            IMediator mediator,
            ILogger<DeleteDnaOrderShippingCommandHandler> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<DnaOrder> Handle(DeleteDnaOrderShippingCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating DNA order status with id: {command.Id} has been started.");
            
            var dnaOrder = await _mediator.Send(new UpdateDnaOrderCommand(
                command.Id,
                Guid.NewGuid().ToString().ToLower(),
                DnaOrderConstants.DeletedBarcode,
                DnaOrderConstants.DeletedPatientShippingNumber,
                DnaOrderConstants.DeletedLaboratoryShippingNumber));

            dnaOrder.UpdateStatus(OrderStatus.Placed);

            await _dnaOrdersService.UpdateAsync(dnaOrder);
            
            _logger.LogInformation($"Successfully assigned [Status] = {OrderStatus.Placed} to Dna Order [Id] = {dnaOrder.GetId()}");
            
            return dnaOrder;
        }
    }
}