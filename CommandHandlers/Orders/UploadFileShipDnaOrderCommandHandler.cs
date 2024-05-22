using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Shared.Exceptions;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Enums.Orders;
using MediatR;
using WildHealth.Application.Utils.Spreadsheets;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class UploadFileShipDnaOrderCommandHandler : IRequestHandler<UploadFileShipDnaOrderCommand>
    {
        private const string BarcodeTitle = "Barcode Number";
        private const string ShipDateTitle = "Ship Date";
        private const string OrderNumberTitle = "Order-Job";
        private const string PatientTrackingTitle = "Tracking No";
        private const string LabTrackingTitle = "Con. Tracking No.";

        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger<UploadFileShipDnaOrderCommandHandler> _logger;

        public UploadFileShipDnaOrderCommandHandler(
            IDnaOrdersService dnaOrdersService,
            IMediator mediator,
            ILogger<UploadFileShipDnaOrderCommandHandler> logger
            )
        {
            _dnaOrdersService = dnaOrdersService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(UploadFileShipDnaOrderCommand command, CancellationToken cancellationToken)
        {
            var spreadsheetIterator = new SpreadsheetIterator(command.File);
            
            var importantTitles = new Dictionary<string, string>
            {
                { BarcodeTitle, string.Empty },
                { ShipDateTitle, string.Empty },
                { OrderNumberTitle, string.Empty },
                { PatientTrackingTitle, string.Empty },
                { LabTrackingTitle, string.Empty }
            };
            
            await spreadsheetIterator.Iterate(importantTitles, async (rowResults) =>
            {
                try
                {
                    var orderNumber = rowResults[OrderNumberTitle];

                    _logger.LogInformation($"Attempting to import order: {orderNumber}");

                    if(!String.IsNullOrEmpty(orderNumber))
                    {
                        var order = await _dnaOrdersService.GetByNumberAsync(rowResults[OrderNumberTitle]);
                        var priorStatus = order.Status;

                        order.ReceivedShippingInformation(
                            barcode: rowResults[BarcodeTitle],
                            patientShippingNumber: rowResults[PatientTrackingTitle],
                            laboratoryShippingNumber: rowResults[LabTrackingTitle],
                            date: DateTime.Parse(rowResults[ShipDateTitle])
                        );

                        await _dnaOrdersService.UpdateAsync(order);

                        // Only publish this event if we actually changed status from something else to shipping
                        if(new List<OrderStatus>() { OrderStatus.Ordered, OrderStatus.Placed }.Contains(priorStatus))
                        {
                            await _mediator.Publish(new OrderStatusChangedEvent(order), cancellationToken);
                        }
                    }
                }
                catch (AppException ex) when (ex.StatusCode != HttpStatusCode.NotFound)
                {
                    throw;
                }
            });
        }
    }
}