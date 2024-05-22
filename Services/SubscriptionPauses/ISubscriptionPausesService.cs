using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Payments;

namespace WildHealth.Application.Services.SubscriptionPauses;

public interface ISubscriptionPausesService
{
    Task<SubscriptionPause[]> GetAsync(DateTime date);
}