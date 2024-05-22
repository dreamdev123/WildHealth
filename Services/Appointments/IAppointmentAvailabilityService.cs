using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Enums.Appointments;

namespace WildHealth.Application.Services.Appointments
{
    public interface IAppointmentAvailabilityService
    {
        /// <summary>
        /// Returns appointment types that are available for a patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<IEnumerable<AppointmentWithType>> GetForPatientIdAsync(int patientId);

        /// <summary>
        /// Returns reasons why appointments are not available
        /// </summary>
        /// <param name="patientId"></param>

        Task<IEnumerable<AppointmentUnavailableModel>> GetUnavailableForPatientIdAsync(int patientId);
    }
}
