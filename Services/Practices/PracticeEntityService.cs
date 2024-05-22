using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Practices;

public class PracticeEntityService : IPracticeEntityService
{
    private readonly IGeneralRepository<PracticeEntity> _practiceEntityRepository;

    public PracticeEntityService(IGeneralRepository<PracticeEntity> practiceEntityRepository)
    {
        _practiceEntityRepository = practiceEntityRepository;
    }

    public async Task<PracticeEntity> GetByPracticeAndStateAsync(int practiceId, int stateId)
    {
        var practiceEntity = await _practiceEntityRepository
            .All()
            .Where(o => o.PracticeId == practiceId && o.StateId == stateId)
            .FirstOrDefaultAsync();

        if (practiceEntity is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"Practice Entity with practiceId = {practiceId} and stateId = {stateId} does not exist.");
        }

        return practiceEntity;
    }
}