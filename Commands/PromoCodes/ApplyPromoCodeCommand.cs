using System.Collections.Generic;
using MediatR;
using WildHealth.Common.Models.Payments;
using WildHealth.Domain.Enums.Payments;

namespace WildHealth.Application.Commands.PromoCodes;

public record ApplyPromoCodeCommand(
    string Code,
    int PaymentPeriodId,
    PaymentPriceType PaymentPriceType,
    int UserId) : IRequest<List<PaymentPriceModel>>;
