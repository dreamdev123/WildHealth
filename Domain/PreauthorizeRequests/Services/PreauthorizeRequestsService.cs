using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Users;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;

namespace WildHealth.Application.Domain.PreauthorizeRequests.Services;

public class PreauthorizeRequestsService : IPreauthorizeRequestsService
{
    private readonly IGeneralRepository<PreauthorizeRequest> _repository;

    public PreauthorizeRequestsService(IGeneralRepository<PreauthorizeRequest> repository)
    {
        _repository = repository;
    }

    public Task<PreauthorizeRequest> GetByEmailAsync(string email)
    {
        return _repository
            .All()
            .IncludeUser()
            .ByEmail(email)
            .FindAsync();
    }

    public Task<PreauthorizeRequest> GetByTokenAsync(string token)
    {
        return _repository
            .All()
            .IncludeUser()
            .ByToken(token)
            .FindAsync();
    }

    public Task<PreauthorizeRequest> GetByIdAsync(int id)
    {
        return _repository
            .All()
            .IncludeUser()
            .ById(id)
            .FindAsync();
    }

    public Task<PreauthorizeRequest[]> GetByIdsAsync(int[] ids)
    {
        return _repository
            .All()
            .IncludeUser()
            .ByIds(ids)
            .ToArrayAsync();
    }

    public Task<PreauthorizeRequest[]> GetAsync(
        int practiceId, 
        int? paymentPlanId = null, 
        int? paymentPeriodId = null, 
        int? paymentPriceId = null,
        int? employerProductId = null)
    {
        return _repository
            .All()
            .IncludeUser()
            .ByPracticeId(practiceId)
            .ByPaymentPlanId(paymentPlanId)
            .ByPaymentPeriodId(paymentPeriodId)
            .ByPaymentPriceId(paymentPriceId)
            .ByEmployerProductId(employerProductId)
            .ToArrayAsync();
    }
}