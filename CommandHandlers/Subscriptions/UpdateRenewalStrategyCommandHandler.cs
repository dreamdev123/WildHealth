using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.EmployerProducts;
using WildHealth.Application.Services.PaymentPrices;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public class UpdateRenewalStrategyCommandHandler : IRequestHandler<UpdateRenewalStrategyCommand, RenewalStrategy>
{
    private readonly IPaymentPriceService _paymentPriceService;
    private readonly IPromoCodeCouponsService _promoCodeService;
    private readonly IEmployerProductService _employerProductService;
    private readonly MaterializeFlow _materialization;
    private readonly IMediator _mediator;

    public UpdateRenewalStrategyCommandHandler(
        IPaymentPriceService paymentPriceService, 
        IPromoCodeCouponsService promoCodeService, 
        IEmployerProductService employerProductService,
        MaterializeFlow materialization,
        IMediator mediator)
    {
        _paymentPriceService = paymentPriceService;
        _promoCodeService = promoCodeService;
        _employerProductService = employerProductService;
        _materialization = materialization;
        _mediator = mediator;
    }

    public async Task<RenewalStrategy> Handle(UpdateRenewalStrategyCommand command, CancellationToken cancellationToken)
    {
        var getStrategyCommand = new GetOrCreateRenewalStrategyCommand(command.SubscriptionId);

        var strategy = await _mediator.Send(getStrategyCommand, cancellationToken);

        var paymentPrice = await _paymentPriceService.GetAsync(command.PaymentPriceId);

        var promoCode = command.PromoCodeId.HasValue
            ? await _promoCodeService.GetByIdAsync(command.PromoCodeId.Value)
            : null;
            
        var employerProduct = command.EmployerProductId.HasValue
            ? await _employerProductService.GetByIdAsync(command.EmployerProductId.Value)
            : null;

        var flow = new UpdateRenewalStrategyFlow(
            renewalStrategy: strategy,
            paymentPrice: paymentPrice,
            promoCode: promoCode,
            employerProduct: employerProduct
        );
        
        await flow.Materialize(_materialization);

        return strategy;
    }
}