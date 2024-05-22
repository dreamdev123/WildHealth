using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Orders;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class OrderCreatedAtCheckoutCommandHandler : IRequestHandler<OrderCreatedAtCheckoutCommand, bool>
    {
        private readonly IPatientsService _patientsService;
        private readonly ILogger _logger;

        public OrderCreatedAtCheckoutCommandHandler(
            IPatientsService patientsService,
            ILogger<OrderCreatedAtCheckoutCommand> logger)
        {
            _patientsService = patientsService;
            _logger = logger;
        }
        
        public async Task<bool> Handle(OrderCreatedAtCheckoutCommand command, CancellationToken cancellationToken)
        {
            var order = command.Order;
            
            var patientId = order.PatientId;
            
            _logger.LogInformation($"Checking if order for [PatientId]: {patientId} was created at checkout has started.");
            
            var patient = await _patientsService.GetByIdAsync(patientId, PatientSpecifications.OrdersSpecification);

            var firstOrder = patient.Orders
                .Where(o => o.Type == order.Type)
                .OrderBy(o => o.OrderedAt)
                .FirstOrDefault();

            if (firstOrder == null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Unexpected error attempting to determine whether an order was ordered at checkout, [OrderId] = {order.GetId()}");
            }
            
            var timespanBetweenPatientAndOrderCreated = order.OrderedAt - patient.CreatedAt;
            
            // If there's only a single order or if the first order is this order
            // Sometimes there's a lag between when checkout happens and when we get order placed events.  Provide a buffer here to
            // consider that the order was make as part of a patient checking out
            if (firstOrder.GetId() == order.GetId() && timespanBetweenPatientAndOrderCreated.Days < 2)
            {
                return true;
            }

            return false;
        }
        
    }
}