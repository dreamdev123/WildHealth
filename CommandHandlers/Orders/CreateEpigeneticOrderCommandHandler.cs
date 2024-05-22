using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Orders;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.Orders.Epigenetic;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Patients;
using WildHealth.TrueDiagnostic.Models;
using WildHealth.TrueDiagnostic.WebClients;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class CreateEpigeneticOrderCommandHandler : IRequestHandler<CreateEpigeneticOrderCommand, EpigeneticOrder>
    {
        private readonly IEpigeneticOrdersService _epigeneticOrdersService;
        private readonly ITrueDiagnosticWebClient _trueDiagnosticWebClient;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IPatientsService _patientsService;
        private readonly IPaymentService _paymentService;
        private readonly IAddOnsService _addOnsService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CreateEpigeneticOrderCommandHandler(
            IEpigeneticOrdersService epigeneticOrdersService,
            ITrueDiagnosticWebClient trueDiagnosticWebClient,
            IFeatureFlagsService featureFlagsService,
            IDateTimeProvider dateTimeProvider,
            IPatientsService patientsService, 
            IPaymentService paymentService,
            IAddOnsService addOnsService, 
            IMediator mediator,
            ILogger<CreateEpigeneticOrderCommandHandler> logger)
        {
            _epigeneticOrdersService = epigeneticOrdersService;
            _trueDiagnosticWebClient = trueDiagnosticWebClient;
            _featureFlagsService = featureFlagsService;
            _dateTimeProvider = dateTimeProvider;
            _patientsService = patientsService;
            _paymentService = paymentService;
            _addOnsService = addOnsService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<EpigeneticOrder> Handle(CreateEpigeneticOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating Epigenetic order for patient with id: {command.PatientId} has been started.");

            var date = DateTime.UtcNow;
            
            var patient = await _patientsService.GetByIdAsync(command.PatientId);
            
            var addOns = await FetchAddOnsAsync(command.AddOnIds, patient.User.PracticeId);

            AssertAddOnsType(addOns);

            var orderItems = MakeOrderItems(addOns);

            var order = new EpigeneticOrder(
                patient: patient,
                items: orderItems,
                provider: addOns.First().Provider,
                date: date
            );
            
            await PlaceOrderAsync(patient, order, addOns);

            await _epigeneticOrdersService.CreateAsync(order);

            await _mediator.Publish(new OrderCreatedEvent(order), cancellationToken);
            
            if (command.ProcessPayment)
            {
                var payment = await _paymentService.ProcessOrdersPaymentAsync(patient, new [] { order }, command.EmployerProduct);

                if (payment is not null)
                {
                    await _mediator.Send(new MarkEpigeneticOrderAsPaidCommand(order, payment.Id, date), cancellationToken);
                }
            }
            
            _logger.LogInformation($"Creating Epigenetic order for patient with id: {command.PatientId} has been finished.");

            return order;
        }
        
        #region private

        private async Task PlaceOrderAsync(Patient patient, EpigeneticOrder order, AddOn[] addOns)
        {
            if (!_featureFlagsService.GetFeatureFlag(FeatureFlags.TrueDiagnostic))
            {
                return;
            }

            var model = new PlaceOrderRequestModel
            {
                Order = new[]
                {
                    new PlaceOrderModel
                    {
                        Items = addOns.Select(x => new OrderItemModel
                        {
                            Sku = x.IntegrationId,
                            Quantity = 1
                        }).ToArray(),
                        Patient = new PatientModel
                        {
                            FirstName = patient.User.FirstName,
                            LastName = patient.User.LastName,
                            Email = patient.User.Email,
                            Phone = patient.User.PhoneNumber,
                            Address = patient.User.BillingAddress.StreetAddress1,
                            AddressLine1 = patient.User.BillingAddress.StreetAddress2,
                            City = patient.User.BillingAddress.City,
                            State = patient.User.BillingAddress.State,
                            ZipCode = patient.User.BillingAddress.ZipCode,
                            Country = patient.User.BillingAddress.Country
                        },
                        ShippingAddress = new ShippingAddressModel
                        {
                            AddressLine1 = patient.User.ShippingAddress.StreetAddress1,
                            AddressLine2 = patient.User.ShippingAddress.StreetAddress2,
                            City = patient.User.ShippingAddress.City,
                            State = patient.User.ShippingAddress.State,
                            PostalCode = patient.User.ShippingAddress.ZipCode,
                            Country = patient.User.ShippingAddress.Country
                        }
                    }
                }
            };

            var integrationModel = await _trueDiagnosticWebClient.PlaceOrder(model);

            order.MarkAsPlaced(integrationModel.OrderNumber, _dateTimeProvider.UtcNow());
        }
        
        /// <summary>
        /// Asserts if add-on types matches with order type
        /// </summary>
        /// <param name="addOns"></param>
        /// <exception cref="AppException"></exception>
        private void AssertAddOnsType(AddOn[] addOns)
        {
            if (addOns.Any(x => x.OrderType != OrderType.Epigenetic))
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

            return addOns
                .SelectMany(x => x.IsGroup 
                    ? x.Children.Select(k => k.Child) 
                    : new []{ x })
                .Distinct()
                .ToArray();
        }
        
        /// <summary>
        /// Creates and returns order items based on add-ons
        /// </summary>
        /// <param name="addOns"></param>
        /// <returns></returns>
        private OrderItem[] MakeOrderItems(AddOn[] addOns)
        {
            return addOns.Select(addOn =>
            {
                var item = new OrderItem(addOn);

                item.FillOut(
                    sku: addOn.IntegrationId,
                    description: addOn.Name,
                    price: addOn.GetPrice(),
                    quantity: 1
                );

                return item;
            }).ToArray();
        }
        
        #endregion
    }
}