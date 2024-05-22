using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.PaymentPlans;

namespace WildHealth.Application.CommandHandlers.Payments
{
    public class RemovePaymentPlanInsuranceStatesCommandHandler : IRequestHandler<RemovePaymentPlanInsuranceStatesCommand>
    {
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly ILogger _logger;

        public RemovePaymentPlanInsuranceStatesCommandHandler(
            IPaymentPlansService paymentPlansService,
            ILogger<RemovePaymentPlanInsuranceStatesCommandHandler> logger)
        {
            _paymentPlansService = paymentPlansService;
            _logger = logger;
        }

        public async Task Handle(RemovePaymentPlanInsuranceStatesCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Removing insurance states to PaymentPlan with id: {command.PaymentPlanId} has started.");

            var paymentPlan = await _paymentPlansService.GetByIdAsync(command.PaymentPlanId);

            if (paymentPlan is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"PaymentPlan with id: {command.PaymentPlanId} not found.");
            }

            var states = command.States;
            
            foreach (var state in states)
            {
                try
                {

                    var paymentPlanInsuranceState = paymentPlan.PaymentPlanInsuranceStates.First(x => x.StateId == state.Id);
                    
                    await _paymentPlansService.DeletePaymentPlanInsuranceStateAsync(paymentPlanInsuranceState.GetId());
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to remove state (id: {state.Id}) to PaymentPlan (id: {paymentPlan.GetId()}): {ex.Message}");
                }
            }
            
            _logger.LogInformation($"Removing insurance states to PaymentPlan with id: {command.PaymentPlanId} has finished.");
        }
    }
}

