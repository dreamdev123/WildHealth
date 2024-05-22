using System;
using MediatR;
using WildHealth.Common.Models.PromoCodes;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.Commands.PromoCodes;

public record CreatePromoCodeCommand(string Code,
    decimal Discount,
    DiscountType DiscountType,
    string Description,
    DateTime? ExpirationDate,
    int[] PaymentPlanIds,
    bool IsDiscountStartupFee,
    bool IsDiscountLabs,
    bool IsAppliedForInsurance,
    int PracticeId) : IRequest<PromoCodeVewModel>;
