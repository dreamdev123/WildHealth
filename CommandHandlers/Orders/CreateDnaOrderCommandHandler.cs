using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Orders;
using OrderItem = WildHealth.Domain.Entities.Orders.OrderItem;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Common.Constants;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class CreateDnaOrderCommandHandler : IRequestHandler<CreateDnaOrderCommand, DnaOrder>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IPatientsService _patientsService;
        private readonly IPaymentService _paymentService;
        private readonly IAddOnsService _addOnsService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly IFeatureFlagsService _featureFlagsService;

        public CreateDnaOrderCommandHandler(
            IDnaOrdersService dnaOrdersService,
            IPatientsService patientsService,
            IPaymentService paymentService,
            IAddOnsService addOnsService,
            IMediator mediator,
            ILogger<CreateDnaOrderCommandHandler> logger,
            IFeatureFlagsService featureFlagsService)
        {
            _dnaOrdersService = dnaOrdersService;
            _patientsService = patientsService;
            _paymentService = paymentService;
            _addOnsService = addOnsService;
            _mediator = mediator;
            _logger = logger;
            _featureFlagsService = featureFlagsService;
        }

        public async Task<DnaOrder> Handle(CreateDnaOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating DNA order for patient with id: {command.PatientId} has been started.");

            var date = DateTime.UtcNow;
            var patient = await _patientsService.GetByIdAsync(command.PatientId);
            DnaOrder order;
            
            if (!command.IsManual)
            {
                var addOns = await FetchAddOnsAsync(command.AddOnIds, patient.User.PracticeId);

                AssertAddOnsType(addOns);

                var orderItems = CreateOrderItems(addOns);

                order = new DnaOrder(
                    patient: patient,
                    items: orderItems,
                    provider: addOns.First().Provider,
                    date: date
                );
            }
            else
            {
                order = new ManualDnaOrder(
                    patient: patient, 
                    date: date, 
                    barCode: command.BarCode, 
                    patientShippingCode: command.OutboundShippingCode, 
                    laboratoryShippingCode: command.ReturnShippingCode
                );
                
            }

            await _dnaOrdersService.CreateAsync(order);

            await _mediator.Publish(new OrderCreatedEvent(order), cancellationToken);

            if (command.ProcessPayment)
            {
                var payment = await _paymentService.ProcessOrdersPaymentAsync(patient, new [] { order });

                if (payment is not null)
                {
                    await _mediator.Send(new MarkDnaOrderAsPaidCommand(order, payment.Id, date), cancellationToken);

                }
            }

            _logger.LogInformation($"Creating DNA order for patient with id: {command.PatientId} has been finished.");

            return order;
        }

        #region private

        /// <summary>
        /// Asserts if add-on types matches with order type
        /// </summary>
        /// <param name="addOns"></param>
        /// <exception cref="AppException"></exception>
        private void AssertAddOnsType(AddOn[] addOns)
        {
            if (addOns.Any(x => x.OrderType != OrderType.Dna))
            {
                throw new AppException(HttpStatusCode.BadRequest, "Add on type and order type does not match.");
            }
        }

        /// <summary>
        /// Fetches and returns add-ons by ids
        /// </summary>
        /// <param name="addOnIds"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        private async Task<AddOn[]> FetchAddOnsAsync(int[] addOnIds, int practiceId)
        {
            var addOns = await _addOnsService.GetByIdsAsync(addOnIds, practiceId);

            return addOns.ToArray();
        }

        /// <summary>
        /// Creates and returns order items based on add-ons
        /// </summary>
        /// <param name="addOns"></param>
        /// <returns></returns>
        private OrderItem[] CreateOrderItems(AddOn[] addOns)
        {
            return addOns.Select(addOn =>
            {
                var item = new OrderItem(addOn);

                item.FillOut(
                    sku: GetSkuForAddOn(addOn),
                    description: addOn.Name,
                    price: addOn.GetPrice(),
                    quantity: 1
                );

                return item;
            }).ToArray();
        }

        private string GetSkuForAddOn(AddOn addOn)
        {
            var isDnaOrderKit = _featureFlagsService.GetFeatureFlag(FeatureFlags.DnaOrderKit);

            // TODO
            // This is how we are going to handle the initial launch of the Kit integration, this allows us to control the on/off of kit integration with the feature flag
            // once things stabilize, we can update the IntegrationId field for this AddOn in the database 
            return isDnaOrderKit
                ? KitIntegrationConstants.Identifiers.Sku
                : addOn.IntegrationId;
        }

        #endregion
    }
}