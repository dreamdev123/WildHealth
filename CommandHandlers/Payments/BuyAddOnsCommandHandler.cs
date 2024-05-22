using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.AddOns;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.PaymentService;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Enums.User;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Products;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Utils.AuthTicket;
using MediatR;
using WildHealth.Domain.Entities.EmployerProducts;

namespace WildHealth.Application.CommandHandlers.Payments
{
    public class BuyAddOnsCommandHandler : IRequestHandler<BuyAddOnsCommand, IEnumerable<Order>>
    {
        private static readonly ProductType[] FreeOrderProductsTypes =
        {
            ProductType.FreeDnaOrder,
            ProductType.FreeLabOrder,
            ProductType.FreeEpigeneticOrder,
        };

        private static readonly IDictionary<ProductType, OrderType> ProductToOrderMap =
            new Dictionary<ProductType, OrderType>
            {
                { ProductType.FreeDnaOrder, OrderType.Dna },
                { ProductType.FreeLabOrder, OrderType.Lab },
                { ProductType.FreeEpigeneticOrder, OrderType.Epigenetic }
            };

        private readonly IPatientsService _patientsService;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly IPatientProductsService _patientProductsService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IPaymentService _paymentService;
        private readonly IAddOnsService _addOnsService;
        private readonly IAuthTicket _authTicket;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public BuyAddOnsCommandHandler(
            IPatientsService patientsService,
            IPaymentPlansService paymentPlansService,
            IPatientProductsService patientProductsService,
            IFeatureFlagsService featureFlagsService,
            IPaymentService paymentService,
            IAddOnsService addOnsService,
            IAuthTicket authTicket,
            IMediator mediator,
            ILogger<BuyAddOnsCommandHandler> logger)
        {
            _patientsService = patientsService;
            _paymentPlansService = paymentPlansService;
            _patientProductsService = patientProductsService;
            _featureFlagsService = featureFlagsService;
            _paymentService = paymentService;
            _addOnsService = addOnsService;
            _authTicket = authTicket;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<IEnumerable<Order>> Handle(BuyAddOnsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Buying add for patient {request.Patient?.User?.Email} for practice {request.PracticeId} started");

            var patient = request.Patient!;
            
            var gender = request.Patient?.User?.Gender ?? Gender.None;

            var subscriptionPatient = await _patientsService.GetByIdAsync(patient.GetId(),
                PatientSpecifications.PatientJourneyStatusSpecification);

            var products = await _patientProductsService.GetBySubscriptionAsync(patient.GetId(), subscriptionPatient.CurrentSubscription);
            
            try
            {
                var employerProduct = request.EmployerProduct;
                
                var addOns = await GetAddOnsAsync(
                    request.SelectedAddOnIds,
                    request.PaymentPriceId, 
                    request.BuyRequiredAddOns, 
                    request.PracticeId,
                    gender,
                    patient
                );

                if (!addOns.Any())
                {
                    _logger.LogInformation("No addOns to buy");
                    
                    return Array.Empty<Order>();
                }

                var allOrders = await MakeOrdersAsync(patient, addOns, request.EmployerProduct);

                var freeOrdersAndProducts = GetFreeOrders(allOrders, products);

                var freeOrders = freeOrdersAndProducts.Select(x => x.order).ToArray();
                
                var freeProducts = freeOrdersAndProducts.Select(x => x.product).ToArray();
                
                var regularOrders = GetRegularOrders(allOrders, freeOrders);

                if (freeOrders.Any())
                {
                    var payment = await _paymentService.ProcessFreeOrdersPaymentAsync(patient, freeOrders);
                    
                    if (payment is not null)
                    {
                        await MarkOrderAsPaidAsync(freeOrders, payment.Id, DateTime.UtcNow);
                    }

                    var usedBy = _authTicket.IsBackgroundProcess()
                        ? "Background Process"
                        : _authTicket.GetId().ToString();

                    var date = DateTime.UtcNow;
        
                    await _patientProductsService.UseBulkAsync(
                        patientProducts: freeProducts, 
                        usedBy:  usedBy, 
                        usedAt: date
                    );
                }

                if (regularOrders.Any())
                {
                    var payment = await _paymentService.ProcessOrdersPaymentAsync(patient, regularOrders, employerProduct);

                    if (payment is not null)
                    {
                        await MarkOrderAsPaidAsync(regularOrders, payment.Id, DateTime.UtcNow);
                    }
                
                    _logger.LogInformation($"{regularOrders.Count()} addOns successfully paid by patient with email: {request.Patient?.User?.Email} for practice {request.PracticeId}");
                }
                
                return allOrders;
            }
            catch (Exception e)
            {
                if (request.SkipPaymentError)
                {
                    _logger.LogError($"AddOns payment process was failed for patient with email: {request.Patient?.User?.Email} was failed without throwing exception. with [Error]: {e}");
                    
                    return Array.Empty<Order>();
                }

                _logger.LogError($"AddOns payment process was failed for patient with email: {request.Patient?.User?.Email} was failed with throwing exception. with [Error]: {e}");
                
                throw;
            }
        }
        
        #region private

        private (Order order, PatientProduct product)[] GetFreeOrders(Order[] allOrders, PatientProduct[] products)
        {
            var freeOrders = new List<(Order order, PatientProduct product)>();

            foreach (var product in products)
            {
                if (!FreeOrderProductsTypes.Contains(product.ProductType))
                {
                    continue;
                }

                var freeOrderType = ProductToOrderMap[product.ProductType];

                var order = allOrders.FirstOrDefault(x => x.Type == freeOrderType && freeOrders.All(t => t.order.Id != x.Id));
                if (order is not null)
                {
                    freeOrders.Add((order, product));
                }
            }

            return freeOrders.ToArray();
        }

        private Order[] GetRegularOrders(Order[] allOrders, Order[] freeOrders)
        {
            return allOrders.Where(x => freeOrders.All(t => t.Id != x.Id)).ToArray();
        }
        
        private async Task MarkOrderAsPaidAsync(Order[] orders, string paymentId, DateTime paymentDate)
        {
            foreach (var order in orders)
            {
                Order _ = true switch
                {
                    true when order is DnaOrder dnaOrder => await _mediator.Send(new MarkDnaOrderAsPaidCommand(
                        order: dnaOrder, 
                        paymentId: paymentId, 
                        paymentDate: paymentDate)),
                    
                    true when order is EpigeneticOrder epigeneticOrder => await _mediator.Send(new MarkEpigeneticOrderAsPaidCommand(
                        order: epigeneticOrder, 
                        paymentId: paymentId, 
                        paymentDate: paymentDate)),
                    
                    true when order is LabOrder labOrder => await _mediator.Send(new MarkLabOrderAsPaidCommand(
                        order: labOrder, 
                        paymentId: paymentId, 
                        paymentDate: paymentDate)),

                    _ => throw new ArgumentException("Unsupported order type")
                };
            }
        }

        private async Task<Order[]> MakeOrdersAsync(Patient patient, IEnumerable<AddOn> addOns, EmployerProduct employerProduct)
        {
            var useIntegrationFlow = _featureFlagsService.GetFeatureFlag(FeatureFlags.LabOrdersChangeHealthCare);

            var orders = new List<Order>();
            
            var addOnGroups = addOns.GroupBy(x => x.OrderType);
            
            foreach (var addOnGroup in addOnGroups)
            {
                var addOnIds = addOnGroup.Select(x => x.GetId()).ToArray();

                if (addOnGroup.Key == OrderType.Dna)
                {
                    var dnaOrder = await _mediator.Send(new CreateDnaOrderCommand(
                        patientId: patient.GetId(),
                        addOnIds: addOnIds,
                        processPayment: false,
                        isManual: false,
                        barCode: "",
                        returnShippingCode: "",
                        outboundShippingCode:""
                    ));
                    
                    orders.Add(dnaOrder);
                    
                    continue;
                }

                if (addOnGroup.Key == OrderType.Epigenetic)
                {
                    var epigeneticOrder = await _mediator.Send(new CreateEpigeneticOrderCommand(
                        patientId: patient.GetId(),
                        addOnIds: addOnIds,
                        processPayment: false,
                        employerProduct: employerProduct
                    ));
                    
                    orders.Add(epigeneticOrder);
                    
                    continue;
                }

                if (addOnGroup.Key == OrderType.Lab)
                {
                    if (useIntegrationFlow)
                    {
                        await _mediator.Send(new PlaceLabOrderCommand(
                            patient: patient,
                            addOnIds: addOnIds
                        ));
                    
                        continue;
                    }

                    var labOrder = await _mediator.Send(new CreateLabOrderCommand(
                        patientId: patient.GetId(),
                        addOnIds: addOnIds,
                        orderNumber: string.Empty
                    ));
                    
                    orders.Add(labOrder);
                        
                    continue;
                }

                throw new ArgumentException("Unsupported order type");
            }

            return orders.ToArray();
        }

        private async Task<AddOn[]> GetAddOnsAsync(
            IEnumerable<int> selectedAddOnIds, 
            int paymentPriceId, 
            bool includeRequired, 
            int practiceId,
            Gender gender,
            Patient patient)
        {
            var employerKey = patient?.CurrentSubscription?.EmployerProduct?.Key;
            
            var paymentPrice = await _paymentPlansService.GetPaymentPriceByIdAsync(paymentPriceId);
            var requiredAddOns = await _addOnsService.GetRequiredAsync(paymentPrice.PaymentPeriod.PaymentPlanId, gender, employerKey);
            var selectedAddOns = await _addOnsService.GetByIdsAsync(selectedAddOnIds, practiceId, employerKey);

            if (paymentPrice.IsAddOnsDisabled() && includeRequired)
            {
                return requiredAddOns.ToArray();
            }

            var allAddOns = includeRequired 
                ? requiredAddOns.Concat(selectedAddOns).ToArray() 
                : selectedAddOns.ToArray();

            var useDefaultFlow = !_featureFlagsService.GetFeatureFlag(FeatureFlags.LabOrdersChangeHealthCare);
            if (useDefaultFlow)
            {
                return allAddOns;
            }

            return allAddOns
                .SelectMany(x => x.IsGroup 
                    ? x.Children.Select(k => k.Child) 
                    : new []{ x })
                .Distinct()
                .ToArray();
        }
        
        #endregion
    }
}
