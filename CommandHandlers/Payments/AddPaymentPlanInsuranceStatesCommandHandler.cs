using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.PaymentPlans;

namespace WildHealth.Application.CommandHandlers.Payments
{
    public class AddPaymentPlanInsuranceStatesCommandHandler : IRequestHandler<AddPaymentPlanInsuranceStatesCommand>
    {
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly ILogger _logger;

        public AddPaymentPlanInsuranceStatesCommandHandler(
            IPaymentPlansService paymentPlansService,
            ILogger<AddPaymentPlanInsuranceStatesCommandHandler> logger)
        {
            _paymentPlansService = paymentPlansService;
            _logger = logger;
        }

        public async Task Handle(AddPaymentPlanInsuranceStatesCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Adding insurance states to PaymentPlan with id: {command.PaymentPlanId} has started.");

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
                    if (!paymentPlan.PaymentPlanInsuranceStates.Any(x => x.StateId == state.Id))
                    {
                        var paymentPlanInsuranceState = new PaymentPlanInsuranceState(paymentPlan.GetId(), state.Id);
                        
                        await _paymentPlansService.CreatePaymentPlanInsuranceStateAsync(paymentPlanInsuranceState);
                    }
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to add state (id: {state.Id}) to PaymentPlan (id: {paymentPlan.GetId()}): {ex.Message}");
                }
            }
            
            _logger.LogInformation($"Adding insurance states to PaymentPlan with id: {command.PaymentPlanId} has finished.");
        }
    }
}

