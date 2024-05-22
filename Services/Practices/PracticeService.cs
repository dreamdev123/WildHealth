using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Licensing.Api.Models.Practices;
using WildHealth.Licensing.Api.Services;
using WildHealth.Shared.Data.Context;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Shared.Data.Extensions;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.DistributedCache.Services;
using WildHealth.Infrastructure.Data.Queries;

namespace WildHealth.Application.Services.Practices
{
    /// <summary>
    /// <see cref="IPracticeService"/>
    /// </summary>
    public class PracticeService : IPracticeService
    {
        private readonly IGeneralRepository<Practice> _practicesRepository;
        private readonly IWildHealthSpecificCacheService<PracticeService, PracticeModel> _wildHealthSpecificCacheService;
        private readonly IWildHealthLicensingApiService _licensingApiService;
        private readonly IApplicationDbContext _context;

        public PracticeService(
            IGeneralRepository<Practice> practices,
            IWildHealthSpecificCacheService<PracticeService, PracticeModel> wildHealthSpecificCacheService,
            IWildHealthLicensingApiService licensingApiService,
            IApplicationDbContext context)
        {
            _practicesRepository = practices;
            _wildHealthSpecificCacheService = wildHealthSpecificCacheService;
            _licensingApiService = licensingApiService;
            _context = context;
        }

        /// <summary>
        /// <see cref="IPracticeService.GetOriginalPractice"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<PracticeModel> GetOriginalPractice(int id)
        {
            return await _wildHealthSpecificCacheService.GetAsync($"{id.GetHashCode()}", async () => await _licensingApiService.GetPractice(id));
        }

        /// <summary>
        /// <see cref="IPracticeService.GetAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Practice> GetAsync(int id)
        {
            var practice = await _practicesRepository.GetAsync(id);

            if (practice is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Active Practice does not exist", exceptionParam);
            }

            return practice;
        }

        /// <summary>
        /// <see cref="IPracticeService.GetAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Practice> GetAsync(int id, ISpecification<Practice> specification)
        {
            var practice = await _practicesRepository
                .All()
                .GetById(id)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();

            if (practice is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Active Practice does not exist", exceptionParam);
            }

            return practice;
        }

        /// <summary>
        /// <see cref="IPracticeService.GetPatientCount(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<int> GetPatientCount(int id)
        {
            var totalPatientCount = 0;
            
            await using(var command = _context.Instance.Database.GetDbConnection().CreateCommand()) {
                command.CommandText = @$"
SELECT count(*) as ""TotalPatientCount""
FROM Practices p 
	INNER JOIN Users u on u.PracticeId = p.Id 
	INNER JOIN Patient p2 on p2.UserId = u.Id 
	
WHERE 
	p.Id = {id}";
                _context.Instance.Database.OpenConnection();
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    if(reader.HasRows) {
                        while(reader.Read()) {
                            totalPatientCount = reader.GetInt32(0);
                        }
                    }
                }
            }

            return totalPatientCount;
        }


        /// <summary>
        /// <see cref="IPracticeService.GetSpecAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Practice> GetSpecAsync(int id, ISpecification<Practice> specification)
        {
            var practice = await _practicesRepository
                .Get(o => o.Id == id)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();

            if (practice is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Practice does not exist", exceptionParam);
            }

            return practice;
        }

        /// <summary>
        /// <see cref="IPracticeService.GetActiveAsync()"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Practice>> GetActiveAsync()
        {
            var practices = await _practicesRepository.All()
                .Active()
                .ToArrayAsync();

            return practices;
        }

        /// <summary>
        /// <see cref="IPracticeService.GetAllAsync()"/>
        /// </summary>
        /// <returns></returns>
        public async Task<IEnumerable<Practice>> GetAllAsync()
        {
            var practices = await _practicesRepository.All()
                .ToArrayAsync();

            return practices;
        }

        /// <summary>
        /// <see cref="IPracticeService.CreateAsync"/>
        /// </summary>
        /// <param name="practice"></param>
        /// <returns></returns>
        public async Task<Practice> CreateAsync(Practice practice)
        {
            if (_context.Instance.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                await _context.Instance.Database.ExecuteSqlInterpolatedAsync($"SET IDENTITY_INSERT [Practices] ON;");
            }
            await _practicesRepository.AddAsync(practice);

            await _practicesRepository.SaveAsync();

            if (_context.Instance.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                await _context.Instance.Database.ExecuteSqlInterpolatedAsync($"SET IDENTITY_INSERT [Practices] OFF;");
            }
            return practice;
        }

        /// <summary>
        /// <see cref="IPracticeService.UpdateAsync"/>
        /// </summary>
        /// <param name="practice"></param>
        /// <returns></returns>
        public async Task<Practice> UpdateAsync(Practice practice)
        {
            _practicesRepository.Edit(practice);

            await _practicesRepository.SaveAsync();
            
            InvalidateCache(practice.GetId());

            return practice;
        }

        /// <summary>
        /// <see cref="IPracticeService.InvalidateCache"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public void InvalidateCache(int id)
        {
            _wildHealthSpecificCacheService.RemoveKey($"{id.GetHashCode()}");
        }
    }
}