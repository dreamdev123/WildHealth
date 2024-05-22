using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.SubscriptionPauses;

public class SubscriptionPausesService : ISubscriptionPausesService
{
    private readonly IGeneralRepository<SubscriptionPause> _repository;

    public SubscriptionPausesService(IGeneralRepository<SubscriptionPause> repository)
    {
        _repository = repository;
    }

    public Task<SubscriptionPause[]> GetAsync(DateTime date)
    {
        return _repository
            .All()
            .ByDate(date)
            .ToArrayAsync();
    }
}