using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Domain.Entities.Orders;
using Microsoft.Extensions.Logging;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class UpdateDnaOrderCommandHandler : IRequestHandler<UpdateDnaOrderCommand, DnaOrder>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly ILogger _logger;

        public UpdateDnaOrderCommandHandler(
            IDnaOrdersService dnaOrdersService,
            ILogger<UpdateDnaOrderCommandHandler> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _logger = logger;
        }

        public async Task<DnaOrder> Handle(UpdateDnaOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating DNA order with id: {command.Id} has been started.");
            
            var order = await _dnaOrdersService.GetByIdAsync(command.Id);

            order.UpdateDnaOrderInformation(
                number: command.Number,
                barcode: command.Barcode,
                patientShippingNumber: command.PatientShippingNumber,
                laboratoryShippingNumber: command.LaboratoryShippingNumber
            );

            await _dnaOrdersService.UpdateAsync(order);
            
            _logger.LogInformation($"Updating DNA order with id: {command.Id} has been finished.");
            
            return order;
        }
    }
}