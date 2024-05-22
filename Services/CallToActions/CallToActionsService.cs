using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Query;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Actions;
using WildHealth.Domain.Enums.Actions;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.CallToActions;

public class CallToActionsService : ICallToActionsService
{
    private readonly IGeneralRepository<CallToAction> _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CallToActionsService(
        IGeneralRepository<CallToAction> repository, 
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<CallToAction> GetAsync(int id)
    {
        return _repository
            .All()
            .ById(id)
            .Include(x => x.Data)
            .Include(x => x.Results)
            .FindAsync();
    }

    public Task<CallToAction[]> ActiveAsync(int patientId)
    {
        return _repository
            .All()
            .RelatedToPatient(patientId)
            .ByStatus(ActionStatus.Active)
            .NotExpired(_dateTimeProvider.UtcNow())
            .ToArrayAsync();
    }

    public Task<CallToAction[]> AllAsync(int patientId)
    {
        return _repository
            .All()
            .RelatedToPatient(patientId)
            .ToArrayAsync();
    }
}