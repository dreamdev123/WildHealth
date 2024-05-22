using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.PromoCodes;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Common.Models.PromoCodes;
using WildHealth.Domain.Models.Payment;
using WildHealth.Domain.Models.Payment.Create;

namespace WildHealth.Application.CommandHandlers.PromoCodes;

public class CreatePromoCodeCommandHandler : IRequestHandler<CreatePromoCodeCommand, PromoCodeVewModel>
{
    private readonly IPromoCodeCouponsService _promoCodeService;
    private readonly IPaymentPlansService _paymentPlansService;
    
    public CreatePromoCodeCommandHandler(
        IPromoCodeCouponsService promoCodeService, 
        IPaymentPlansService paymentPlansService)
    {
        _promoCodeService = promoCodeService;
        _paymentPlansService = paymentPlansService;
    }

    public async Task<PromoCodeVewModel> Handle(CreatePromoCodeCommand command, CancellationToken cancellationToken)
    {
        var existingPracticePromoCodes = (await _promoCodeService
            .GetAsync(command.PracticeId))
            .Select(x => PromoCodeDomain.Create(x, DateTime.UtcNow))
            .ToList();
        
        var allPaymentPlans = await _paymentPlansService.GetForPromoCode(command.PracticeId);
        
        var request = new CreatePromoCodeRequest(
            command.Code,
            command.Discount,
            command.DiscountType,
            command.Description,
            command.ExpirationDate,
            command.PaymentPlanIds,
            command.IsDiscountStartupFee,
            command.IsDiscountLabs,
            command.IsAppliedForInsurance,
            command.PracticeId,
            DateTime.UtcNow, 
            existingPracticePromoCodes,
            allPaymentPlans.ToList());

        var couponDomain = PromoCodeDomain.Create(request);

        var dataModel = couponDomain.BuildDataModel();

        var created = await _promoCodeService.CreateAsync(dataModel);
        
        return PromoCodeDomain
            .Create(created, DateTime.UtcNow)
            .BuildViewModel();
    }
}
