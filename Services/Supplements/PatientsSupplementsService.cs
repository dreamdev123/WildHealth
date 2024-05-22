using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Supplement;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Settings;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Notes.Common;
using WildHealth.Shared.Data.Queries;
using WildHealth.Application.Extensions.Query;
using WildHealth.Common.Models.Supplements;

namespace WildHealth.Application.Services.Supplements
{
    public class PatientsSupplementsService: IPatientsSupplementsService
    {
        private readonly IGeneralRepository<PatientSupplement> _patientSupplementsRepository;
        private readonly IGeneralRepository<CommonSupplement> _commonSupplementsRepository;
        private readonly ISettingsManager _settingsManager;

        public PatientsSupplementsService(
            IGeneralRepository<PatientSupplement> patientSupplementsRepository,
            IGeneralRepository<CommonSupplement> commonSupplementsRepository,
            ISettingsManager settingsManager
        )
        {
            _patientSupplementsRepository = patientSupplementsRepository;
            _commonSupplementsRepository = commonSupplementsRepository;
            _settingsManager = settingsManager;
        }

        /// <summary>
        /// <see cref="IPatientsSupplementsService.GetAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PatientSupplement>> GetAsync(int patientId)
        {
            var patientSupplements =  await _patientSupplementsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludePatient()
                .ToArrayAsync();

            return patientSupplements;
        }

        /// <summary>
        /// <see cref="IPatientsSupplementsService.GetDefaultLinkAsync(int)"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<string?> GetDefaultLinkAsync(int practiceId)
        {
            var hasDefaultSupplementLink = await _settingsManager.GetSetting<bool>(SettingsNames.General.HasDefaultSupplementLinks, practiceId);

            return hasDefaultSupplementLink
                ? null
                : await _settingsManager.GetSetting<string>(SettingsNames.General.SupplementsLink, practiceId);
        }

        /// <summary>
        /// <see cref="IPatientsSupplementsService.GetByIdAsync(int)"/>
        /// </summary>
        /// <param name="supplementId"></param>
        /// <returns></returns>
        public async Task<PatientSupplement> GetByIdAsync(int supplementId)
        {
            return await _patientSupplementsRepository.GetAsync(supplementId);
        }
        
        public async Task<PatientSupplement[]> GetByIdsAsync(int[] ids)
        {
            var result = await _patientSupplementsRepository
                .All()
                .ByIds(ids)
                .ToArrayAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IPatientsSupplementsService.GetCommonSupplementByIdAsync(int)"/>
        /// </summary>
        /// <param name="supplementId"></param>
        /// <returns></returns>
        public async Task<CommonSupplement> GetCommonSupplementByIdAsync(int supplementId)
        {
            return await _commonSupplementsRepository
                .All()
                .ById(supplementId)
                .FindAsync();
        }

        /// <summary>
        /// <see cref="IPatientsSupplementsService.GetCommonSupplementsAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<CommonSupplement[]> GetCommonSupplementsAsync()
        {
            var commonSupplements =  await _commonSupplementsRepository
                .All()
                .ToArrayAsync();

            return commonSupplements;
        }

        /// <summary>
        /// <see cref="IPatientsSupplementsService.CreateCommonSupplementAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<CommonSupplement> CreateCommonSupplementAsync(CommonSupplementModel model)
        {
            var newCommonSupplement = new CommonSupplement
            {
                Name = model.Name,
                Dosage = model.Dosage,
                Instructions = model.Instructions,
                PurchaseLink = model.PurchaseLink,
                IsInCurrent = model.IsInCurrent,
                IsStopped = model.IsStopped
            };

            await _commonSupplementsRepository.AddAsync(newCommonSupplement);

            await _commonSupplementsRepository.SaveAsync();

            return newCommonSupplement;
        }

        /// <summary>
        /// <see cref="IPatientsSupplementsService.UpdateCommonSupplementAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<CommonSupplement> UpdateCommonSupplementAsync(CommonSupplementModel model)
        {
            var commonSupplement = await GetCommonSupplementByIdAsync(model.Id);

            commonSupplement.Name = model.Name;
            commonSupplement.Dosage = model.Dosage;
            commonSupplement.Instructions = model.Instructions;
            commonSupplement.PurchaseLink = model.PurchaseLink;
            commonSupplement.IsInCurrent = model.IsInCurrent;
            commonSupplement.IsStopped = model.IsStopped;

            _commonSupplementsRepository.Edit(commonSupplement);

            await _commonSupplementsRepository.SaveAsync();

            return commonSupplement;
        }

        /// <summary>
        /// <see cref="IPatientsSupplementsService.DeleteCommonSupplementAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<CommonSupplement> DeleteCommonSupplementAsync(int id)
        {
            var commonSupplement = await GetCommonSupplementByIdAsync(id);

            _commonSupplementsRepository.Delete(commonSupplement);

            await _commonSupplementsRepository.SaveAsync();

            return commonSupplement;
        }
    }
}
