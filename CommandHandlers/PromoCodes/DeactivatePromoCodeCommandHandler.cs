using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.PromoCodes;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Domain.Models.Payment;

namespace WildHealth.Application.CommandHandlers.PromoCodes;

public class DeactivatePromoCodeCommandHandler : IRequestHandler<DeactivatePromoCodeCommand>
{
    private readonly IPromoCodeCouponsService _promoCodeService;

    public DeactivatePromoCodeCommandHandler(IPromoCodeCouponsService promoCodeService)
    {
        _promoCodeService = promoCodeService;
    }

    public async Task Handle(DeactivatePromoCodeCommand request, CancellationToken cancellationToken)
    {
        var couponToDeactivate = await _promoCodeService.GetByIdAsync(request.Id);

        var aggregate = PromoCodeDomain.Create(couponToDeactivate, DateTime.UtcNow);
        
        var deactivated = aggregate.Deactivate();
        
        await _promoCodeService.EditAsync(deactivated);
    }
}