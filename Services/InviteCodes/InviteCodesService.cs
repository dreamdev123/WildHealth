using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.InviteCodes;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.InviteCodes
{
    /// <summary>
    /// <see cref="IInviteCodesService"/>
    /// </summary>
    public class InviteCodesService: IInviteCodesService
    {
        private readonly IGeneralRepository<InviteCode> _inviteCodeRepository;

        public InviteCodesService(IGeneralRepository<InviteCode> inviteCodeRepository)
        {
            _inviteCodeRepository = inviteCodeRepository;
        }

        /// <summary>
        /// <see cref="IInviteCodesService.GetByCodeAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        public async Task<InviteCode> GetByCodeAsync(int practiceId, string code)
        {
            var inviteCode = await _inviteCodeRepository
                .All()
                .ByCode(code)
                .RelatedToPractice(practiceId)
                .Active()
                .IncludePatients()
                .FirstOrDefaultAsync();

            if (inviteCode is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Active invite code {code} not found.");
            }

            return inviteCode;
        }
    }
}
