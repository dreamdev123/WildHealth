using System.Threading.Tasks;
using WildHealth.Domain.Entities.Practices;

namespace WildHealth.Application.Services.Practices;

public interface IPracticeEntityService
{
    /// <summary>
    /// Get practice entity by practice id and state id
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="stateId"></param>
    /// <returns></returns>
    Task<PracticeEntity> GetByPracticeAndStateAsync(int practiceId, int stateId);
}