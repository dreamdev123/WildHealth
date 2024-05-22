using System;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Appointments;

namespace WildHealth.Application.Services.AppointmentsOptions
{
    /// <summary>
    /// Provides CRUD methods for Appointment Options Entity
    /// </summary>
    public interface IAppointmentOptionsService
    {
        /// <summary>
        /// Returns appointment options by patient id
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<AppointmentOptions[]> GetByPatientAsync(int patientId);
        
        /// <summary>
        /// Creates and returns new appointment options
        /// </summary>
        /// <param name="appointmentOptions"></param>
        /// <returns></returns>
        Task<AppointmentOptions> CreateAsync(AppointmentOptions appointmentOptions);

        /// <summary>
        /// Updates existing appointment options
        /// </summary>
        /// <param name="appointmentOptions"></param>
        /// <returns></returns>
        Task<AppointmentOptions> UpdateAsync(AppointmentOptions appointmentOptions);

        
        /// <summary>
        /// Updates next HealthCoach appointment date or creates a new AppointmentOptions entry
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="nextAppointmentDate"></param>
        /// <returns></returns>
        Task UpdateHealthCoachAppointmentDateAsync(int patientId, DateTime nextAppointmentDate);

        /// <summary>
        /// Updates next Provider appointment date or creates a new AppointmentOptions entry
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="nextAppointmentDate"></param>
        /// <returns></returns>
        Task UpdateProviderAppointmentDateAsync(int patientId, DateTime nextAppointmentDate);

        /// <summary>
        /// Deleted appointment options
        /// </summary>
        /// <param name="appointmentOptionsId"></param>
        /// <returns></returns>
        Task DeleteAsync(int appointmentOptionsId);
    }
}