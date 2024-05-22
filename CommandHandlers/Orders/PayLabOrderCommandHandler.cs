using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Application.Services.PaymentService;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.AddOns;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Models.Orders;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Models.Payments;
using WildHealth.Shared.Utils.AuthTicket;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class PayLabOrderCommandHandler : IRequestHandler<PayLabOrderCommand, LabOrder>
    {
        private readonly IPatientProductsService _patientProductsService;
        private readonly ILabOrdersService _labOrdersService;
        private readonly IPatientsService _patientsService;
        private readonly IPaymentService _paymentService;
        private readonly IAuthTicket _authTicket;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public PayLabOrderCommandHandler(
            IPatientProductsService patientProductsService,
            ILabOrdersService labOrdersService, 
            IPatientsService patientsService,
            IPaymentService paymentService,
            IAuthTicket authTicket,
            IMediator mediator,
            ILogger<PayLabOrderCommandHandler> logger)
        {
            _patientProductsService = patientProductsService;
            _labOrdersService = labOrdersService;
            _patientsService = patientsService;
            _paymentService = paymentService;
            _authTicket = authTicket;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<LabOrder> Handle(PayLabOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Paying of Lab order with id: {command.Id} has been started.");

            var order = await _labOrdersService.GetByIdAsync(command.Id);

            AssertCanBePaid(order);

            if (await ShouldSkipPaymentAsync(order))
            {
                return order;
            }
            
            var date = DateTime.UtcNow;

            var specification = PatientSpecifications.PatientWithEmployerProductSpecification;

            var patient = await _patientsService.GetByIdAsync(order.PatientId, specification);

            var employerProduct = patient.CurrentSubscription?.EmployerProduct;

            var freeOrderProduct = await _patientProductsService.GetByTypeAsync(
                patientId: patient.GetId(),
                type: ProductType.FreeLabOrder,
                builtInSourceId: patient.CurrentSubscription?.UniversalId ?? Guid.Empty
            );

            var isPatientChargedForLabs = true;

            try
            {
                PaymentIntegrationModel payment;
                
                if (freeOrderProduct is not null)
                {
                    payment = await _paymentService.ProcessFreeOrdersPaymentAsync(
                        patient: patient,
                        orders: new Order[] { order }
                    );
                    
                    var usedBy = _authTicket.IsBackgroundProcess()
                        ? "Background Process"
                        : _authTicket.GetId().ToString();

                    await _patientProductsService.UseAsync(
                        id: freeOrderProduct.GetId(), 
                        usedBy: usedBy,
                        usedAt: order.OrderedAt
                    );

                    isPatientChargedForLabs = false;
                }
                else
                {
                    payment = await _paymentService.ProcessOrdersPaymentAsync(
                        patient: order.Patient, 
                        orders: new Order[] { order },
                        employerProduct: employerProduct
                    );
                }
                
                // PaymentService::ProcessOrdersPaymentAsync can return null.  If that's the case then we want to act accordingly
                if (payment is not null)
                {
                    await _mediator.Send(new MarkLabOrderAsPaidCommand(
                        order: order,
                        paymentId: payment.Id,
                        paymentDate: date), cancellationToken);
            
                    if (command.ShouldSendOrderInvoiceEmail && isPatientChargedForLabs) {
                        await _mediator.Send(new SendLabOrderInvoiceEmailCommand(order), cancellationToken); 
                    }   
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Paying of Lab order with id: {command.Id} has been failed. {ex.Message}");
                
                throw;
            }
            
            _logger.LogInformation($"Paying of Lab order with id: {command.Id} has been finished.");

            return order;
        }

        #region private
        /// <summary>
        /// Checks if order can be paid, otherwise throw an exception
        /// </summary>
        /// <param name="order"></param>
        /// <exception cref="AppException"></exception>
        private void AssertCanBePaid(LabOrder order)
        {
            var orderDomain = OrderDomain.Create(order);
            if (orderDomain.IsPaid())
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(order.Id), order.GetId());
                throw new AppException(HttpStatusCode.BadRequest, "Order already paid.", exceptionParam);
            }
        }

        /// <summary>
        /// Checks if payment should be skipped
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private async Task<bool> ShouldSkipPaymentAsync(LabOrder order)
        {
            var addOns = GetAddOns(order);
            
            var canPayForAddOns = await _paymentService.CanPayForAddOnsAsync(addOns, order.Patient.User.PracticeId);

            return !canPayForAddOns;
        }
        
        /// <summary>
        /// returns order add-ons
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        private AddOn[] GetAddOns(LabOrder order)
        {
            return order.Items.Select(x => x.AddOn).ToArray();
        }

        #endregion
    }
}