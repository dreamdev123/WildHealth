using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;

namespace WildHealth.Application.Services.Founders
{
    /// <summary>
    /// <see cref="IFoundersService"/>
    /// </summary>
    public class FoundersService : IFoundersService
    {
        private readonly IGeneralRepository<Founder> _foundersRepository;

        public FoundersService(IGeneralRepository<Founder> foundersRepository)
        {
            _foundersRepository = foundersRepository;
        }

        /// <summary>
        /// <see cref="IFoundersService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Founder> GetByIdAsync(int id)
        {
            var founder = await _foundersRepository.GetAsync(id);
            if (founder is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Founder does not exist.", exceptionParam);
            }

            return founder;
        }

        /// <summary>
        /// <see cref="IFoundersService.GetActiveAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Founder>> GetActiveAsync(int practiceId)
        {
            var founders = await _foundersRepository
                .All()
                .Active()
                .RelatedToPractice(practiceId)
                .Include(x => x.Employee)
                .ThenInclude(x => x.User)
                .AsNoTracking()
                .ToArrayAsync();

            return founders;
        }
    }
}