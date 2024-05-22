using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.PromoCodes;
using WildHealth.Application.Services.PromoCodes;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Models.Payment;

namespace WildHealth.Application.CommandHandlers.PromoCodes;

public class DeletePromoCodeCommandHandler : IRequestHandler<DeletePromoCodeCommand>
{
    private readonly IPromoCodeCouponsService _promoCodeService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeletePromoCodeCommandHandler(
        IPromoCodeCouponsService promoCodeService, 
        IDateTimeProvider dateTimeProvider)
    {
        _promoCodeService = promoCodeService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(DeletePromoCodeCommand request, CancellationToken cancellationToken)
    {
        var couponToDelete = await _promoCodeService.GetByIdAsync(request.Id);

        var aggregate = PromoCodeDomain.Create(couponToDelete, _dateTimeProvider.UtcNow());

        var deleted = aggregate.Delete();
        
        await _promoCodeService.DeleteAsync(deleted);
    }
}