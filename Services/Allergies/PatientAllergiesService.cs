using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using WildHealth.Common.Models.Alergies;
using WildHealth.Domain.Entities.Alergies;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.Allergies
{
    public class PatientAllergiesService : IPatientAllergiesService
    {
        private readonly IGeneralRepository<PatientAlergy> _patientAllergiesRepository;

        public PatientAllergiesService(IGeneralRepository<PatientAlergy> patientAllergiesRepository)
        {
            _patientAllergiesRepository = patientAllergiesRepository;
        }

        /// <summary>
        /// <see cref="IPatientAllergiesService.GetByPatientIdAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PatientAlergy>> GetByPatientIdAsync(int patientId)
        {
            var patientAllergies = await _patientAllergiesRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludePatient()
                .ToArrayAsync();

            return patientAllergies;
        }
        
        public async Task<PatientAlergy[]> GetByIdsAsync(int[] ids)
        {
            var patientAllergies = await _patientAllergiesRepository
                .All()
                .ByIds(ids)
                .ToArrayAsync();

            return patientAllergies;
        }

        /// <summary>
        /// <see cref="IPatientAllergiesService.CreatePatientAllergyAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<PatientAlergy> CreatePatientAllergyAsync(CreatePatientAlergyModel model, int patientId)
        {
            var patientAllergy = new PatientAlergy(patientId, model.Name)
            {
                Reaction = model.Reaction
            };

            await _patientAllergiesRepository.AddAsync(patientAllergy);

            await _patientAllergiesRepository.SaveAsync();

            return patientAllergy;
        }

        /// <summary>
        /// <see cref="IPatientAllergiesService.EditPatientAllergyAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<PatientAlergy> EditPatientAllergyAsync(PatientAlergyModel model)
        {
            var patientAllergy = await _patientAllergiesRepository.GetAsync(model.Id);

            if (patientAllergy is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(model.Id), model.Id);
                throw new AppException(HttpStatusCode.NotFound, "Patient allergy does not exist", exceptionParam);
            }

            patientAllergy.Name = model.Name;
            patientAllergy.Reaction = model.Reaction;

            _patientAllergiesRepository.Edit(patientAllergy);

            await _patientAllergiesRepository.SaveAsync();

            return patientAllergy;
        }

        /// <summary>
        /// <see cref="IPatientAllergiesService.DeleteAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<PatientAlergy> DeleteAsync(int id)
        {
            var patientSupplement = await _patientAllergiesRepository.GetAsync(id);

            if (patientSupplement is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Patient allergy does not exist", exceptionParam);
            }

            _patientAllergiesRepository.Delete(patientSupplement);

            await _patientAllergiesRepository.SaveAsync();

            return patientSupplement;
        }
    }
}
