using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.PaymentPlans;
using MediatR;
using WildHealth.Application.Commands.Subscriptions;

namespace WildHealth.Application.CommandHandlers.Payments
{
    public class MigrateSubscriptionCommandHandler : IRequestHandler<MigrateSubscriptionCommand, bool>
    {
        private readonly IMediator _mediator;
        private readonly IPaymentPlansService _paymentPlansService;

        public MigrateSubscriptionCommandHandler(
            IPaymentPlansService paymentPlansService,
            IMediator mediator)
        {
            _paymentPlansService = paymentPlansService;
            _mediator = mediator;
        }

        public async Task<bool> Handle(MigrateSubscriptionCommand command, CancellationToken cancellationToken)
        {
            var price = await _paymentPlansService.GetPriceV2(
                planName: command.PlanName,
                paymentStrategy: command.PaymentStrategy,
                isInsurance: command.IsInsurance);

            await _mediator.Send(new ChangeSubscriptionPaymentPriceCommand(
                currentSubscriptionId: command.FromSubscriptionId,
                newPaymentPriceId: price.GetId(),
                startDate: command.StartDate,
                endDate: command.EndDate,
                couponCode: command.CouponCode,
                employerProductId: command.EmployerProductId));

            return true;
        }
    }
}