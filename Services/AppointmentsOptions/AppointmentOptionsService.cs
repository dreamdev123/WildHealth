using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Services.AppointmentsOptions
{
    /// <summary>
    /// <see cref="IAppointmentOptionsService"/>
    /// </summary>
    public class AppointmentOptionsService : IAppointmentOptionsService
    {
        private readonly IGeneralRepository<AppointmentOptions> _repository;
        
        public AppointmentOptionsService(IGeneralRepository<AppointmentOptions> repository)
        {
            _repository = repository;
        }
        
        /// <summary>
        /// <see cref="IAppointmentOptionsService.GetByPatientAsync(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<AppointmentOptions[]> GetByPatientAsync(int patientId)
        {
            return await _repository
                .All()
                .RelatedToPatient(patientId)
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IAppointmentOptionsService.CreateAsync(AppointmentOptions)"/>
        /// </summary>
        /// <param name="appointmentOptions"></param>
        /// <returns></returns>
        public async Task<AppointmentOptions> CreateAsync(AppointmentOptions appointmentOptions)
        {
            await _repository.AddAsync(appointmentOptions);

            await _repository.SaveAsync();

            return appointmentOptions;
        }

        /// <summary>
        /// <see cref="IAppointmentOptionsService.UpdateAsync(AppointmentOptions)"/>
        /// </summary>
        /// <param name="appointmentOptions"></param>
        /// <returns></returns>
        public async Task<AppointmentOptions> UpdateAsync(AppointmentOptions appointmentOptions)
        {
            _repository.Edit(appointmentOptions);

            await _repository.SaveAsync();

            return appointmentOptions;
        }

        /// <summary>
        /// <see cref="IAppointmentOptionsService.UpdateHealthCoachAppointmentDateAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="purpose"></param>
        /// <param name="nextAppointmentDate"></param>
        public async Task UpdateHealthCoachAppointmentDateAsync(int patientId, DateTime nextAppointmentDate)
        {
            await CreateOrUpdateAsync(
                patientId: patientId, 
                withType: AppointmentWithType.HealthCoach, 
                purpose: AppointmentPurpose.FollowUp, 
                nextAppointmentDate: nextAppointmentDate
            );
        }
        
        /// <summary>
        /// <see cref="IAppointmentOptionsService.UpdateProviderAppointmentDateAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="nextAppointmentDate"></param>
        public async Task UpdateProviderAppointmentDateAsync(int patientId, DateTime nextAppointmentDate)
        {
            await CreateOrUpdateAsync(
                patientId: patientId, 
                withType: AppointmentWithType.HealthCoachAndProvider, 
                purpose: AppointmentPurpose.FollowUp, 
                nextAppointmentDate: nextAppointmentDate
            );
            
            await CreateOrUpdateAsync(
                patientId: patientId, 
                withType: AppointmentWithType.Provider, 
                purpose: AppointmentPurpose.FollowUp, 
                nextAppointmentDate: nextAppointmentDate
            );
        }
        
        /// <summary>
        /// <see cref="IAppointmentOptionsService.DeleteAsync(int)"/>
        /// </summary>
        /// <param name="appointmentOptionsId"></param>
        /// <exception cref="AppException"></exception>
        public async Task DeleteAsync(int appointmentOptionsId)
        {
            var appointmentOptions = await _repository
                .All()
                .ById(appointmentOptionsId)
                .FirstOrDefaultAsync();

            if (appointmentOptions is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Appointment Options does not exist");
            }

            _repository.Delete(appointmentOptions);

            await _repository.SaveAsync();
        }
        
        private async Task CreateOrUpdateAsync(int patientId, AppointmentWithType withType, AppointmentPurpose purpose,
            DateTime nextAppointmentDate)
        {
            var existingAppointmentOption = await _repository
                .All()
                .RelatedToPatient(patientId)
                .Where(x => x.WithType == withType && x.Purpose == purpose)
                .FirstOrDefaultAsync();

            if (existingAppointmentOption == null)
            {
                var appointmentOption = new AppointmentOptions(nextAppointmentDate, withType, purpose, patientId);
                await _repository.AddAsync(appointmentOption);
            }
            else
            {
                existingAppointmentOption.NextAppointmentDate = nextAppointmentDate;
                _repository.Edit(existingAppointmentOption);
            }

            await _repository.SaveAsync();
        }
    }
}