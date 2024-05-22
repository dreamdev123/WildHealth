using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands.PromoCodes;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Services.PromoCodes;

public interface IParsePromoCodesService
{
    Task<List<CreatePromoCodeCommand>> Parse(IFormFile file, List<PaymentPlan> paymentPlans, int practiceId);
}