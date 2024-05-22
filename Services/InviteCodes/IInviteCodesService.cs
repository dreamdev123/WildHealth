using System.Threading.Tasks;
using WildHealth.Domain.Entities.InviteCodes;

namespace WildHealth.Application.Services.InviteCodes
{
    /// <summary>
    /// Provides methods for working with invite codes
    /// </summary>
    public interface IInviteCodesService
    {
        /// <summary>
        /// Returns invite code entity by code
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        Task<InviteCode> GetByCodeAsync(int practiceId, string code);
    }
}
