using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.TimeKit.Clients.Models.Bookings;

namespace WildHealth.Application.Services.Schedulers.Bookings
{
    public interface ISchedulerBookingsService
    {
        /// <summary>
        /// Creates appointment in scheduler system.
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="appointment"></param>
        /// <param name="employees"></param>
        /// <param name="patient"></param>
        /// <returns>Scheduler booking model</returns>
        Task<BookingModel> CreateBookingAsync(int practiceId,
            Appointment appointment,
            IEnumerable<Employee> employees,
            Patient? patient);
  
        /// <summary>
        /// Cancels booking in scheduler system.
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="appointment"></param>
        /// <returns></returns>
        Task CancelBookingAsync(int practiceId, Appointment appointment);
    }
}