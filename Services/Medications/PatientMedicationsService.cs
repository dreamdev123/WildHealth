using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Models.Medications;
using WildHealth.Domain.Entities.Medication;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.Medications
{
    public class PatientMedicationsService : IPatientMedicationsService
    {
        private readonly IGeneralRepository<PatientMedication> _patientMedicationsRepository;

        public PatientMedicationsService(IGeneralRepository<PatientMedication> patientMedicationsRepository)
        {
            _patientMedicationsRepository = patientMedicationsRepository;
        }

        /// <summary>
        /// <see cref="IPatientMedicationsService.GetAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PatientMedication>> GetAsync(int patientId)
        {
            var patientSupplements = await _patientMedicationsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludePatient()
                .ToArrayAsync();

            return patientSupplements;
        }
        
        public async Task<PatientMedication[]> GetByIdsAsync(int[] ids)
        {
            var patientSupplements = await _patientMedicationsRepository
                .All()
                .ByIds(ids)
                .ToArrayAsync();

            return patientSupplements;
        }

        /// <summary>
        /// <see cref="IPatientMedicationsService.CreateAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<PatientMedication> CreateAsync(CreatePatientMedicationModel model, int patientId)
        {
            var patientMedication = new PatientMedication(patientId, model.Name)
            {
                Dosage =  model.Dosage,
                Instructions = model.Instructions,
                StartDate = model.StartDate
            };

            await _patientMedicationsRepository.AddAsync(patientMedication);
            await _patientMedicationsRepository.SaveAsync();

            return patientMedication;
        }

        /// <summary>
        /// <see cref="IPatientMedicationsService.EditAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<PatientMedication> EditAsync(PatientMedicationModel model)
        {
            var patientMedication = await _patientMedicationsRepository.GetAsync(model.Id);
            
            if (patientMedication is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(model.Id), model.Id);
                throw new AppException(HttpStatusCode.NotFound, "Patient medication does not exist", exceptionParam);
            }
               
            patientMedication.Name = model.Name;
            patientMedication.Dosage = model.Dosage;
            patientMedication.Instructions = model.Instructions;
            patientMedication.StartDate = model.StartDate;

            _patientMedicationsRepository.Edit(patientMedication);

            await _patientMedicationsRepository.SaveAsync();

            return patientMedication;
        }

        /// <summary>
        /// <see cref="IPatientMedicationsService.DeleteAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<PatientMedication> DeleteAsync(int id)
        {
            var patientMedication = await _patientMedicationsRepository.GetAsync(id);
            
            if (patientMedication is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Patient medication oes not exist", exceptionParam);
            }

            _patientMedicationsRepository.Delete(patientMedication);

            await _patientMedicationsRepository.SaveAsync();

            return patientMedication;
        }
    }
}
