using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.HealthSummaries
{
    public class HealthSummaryService : IHealthSummaryService
    {
        private readonly IGeneralRepository<HealthSummaryMap> _healthSummaryMapRepository;
        private readonly IGeneralRepository<HealthSummaryValue> _healthSummaryRepository;

        public HealthSummaryService(
            IGeneralRepository<HealthSummaryMap> healthSummaryMapRepository,
            IGeneralRepository<HealthSummaryValue> healthSummaryRepository)
        {   
            _healthSummaryMapRepository = healthSummaryMapRepository;
            _healthSummaryRepository = healthSummaryRepository;
        }

        /// <summary>
        /// <see cref="IHealthSummaryService.GetMapAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<HealthSummaryMap[]> GetMapAsync()
        {
            return await _healthSummaryMapRepository
                .All()
                .Include(x => x.Items)
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IHealthSummaryService.GetMapByKeyAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<HealthSummaryMap[]> GetMapByKeyAsync(string key)
        {
            return await _healthSummaryMapRepository
                .All()
                .Include(x => x.Items)
                .Where(x=>x.Key.Equals(key))
                .ToArrayAsync();
        }
        
        /// <summary>
        /// <see cref="IHealthSummaryService.GetByPatientAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<HealthSummaryValue[]> GetByPatientAsync(int patientId)
        {
            var healthSumaryRecords =  await _healthSummaryRepository
                .All()
                .RelatedToPatient(patientId)
                .ToArrayAsync();

            return healthSumaryRecords;
        }

        /// <summary>
        /// <see cref="IHealthSummaryService.GetByKeyAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public async Task<HealthSummaryValue> GetByKeyAsync(int patientId, string key)
        {
            return await _healthSummaryRepository
                .All()
                .RelatedToPatient(patientId)
                .Where(x=> x.Key == key)
                .FirstAsync();
        }

        /// <summary>
        /// <see cref="IHealthSummaryService.CreateAsync"/>
        /// </summary>
        /// <param name="healthSummaryValue"></param>
        /// <returns></returns>
        public async Task<HealthSummaryValue> CreateAsync(HealthSummaryValue healthSummaryValue)
        {
            await _healthSummaryRepository.AddAsync(healthSummaryValue);

            await _healthSummaryRepository.SaveAsync();

            return healthSummaryValue;
        }

        /// <summary>
        /// <see cref="IHealthSummaryService.CreateBatchAsync"/>
        /// </summary>
        /// <param name="healthSummaries"></param>
        /// <returns></returns>
        public async Task<HealthSummaryValue[]> CreateBatchAsync(HealthSummaryValue[] healthSummaries)
        {
            foreach (var healthSummary in healthSummaries)
            {
                await _healthSummaryRepository.AddAsync(healthSummary);
            }

            await _healthSummaryRepository.SaveAsync();

            return healthSummaries;
        }

        /// <summary>
        /// <see cref="IHealthSummaryService.UpdateAsync"/>
        /// </summary>
        /// <param name="healthSummaryValue"></param>
        /// <returns></returns>
        public async Task<HealthSummaryValue> UpdateAsync(HealthSummaryValue healthSummaryValue)
        {
            _healthSummaryRepository.Edit(healthSummaryValue);

            await _healthSummaryRepository.SaveAsync();

            return healthSummaryValue;
        }

        /// <summary>
        /// <see cref="IHealthSummaryService.DeleteAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="key"></param>
        /// <exception cref="AppException"></exception>
        public async Task DeleteAsync(int patientId, string key)
        {
            var entity = await _healthSummaryRepository
                .All()
                .FirstOrDefaultAsync(x => x.PatientId == patientId && x.Key == key);

            if (entity is null)
            {
                // do not throw exception if trying to delete not existed item
                return;
            }
            
            _healthSummaryRepository.Delete(entity);

            await _healthSummaryRepository.SaveAsync();
        }
    }
}