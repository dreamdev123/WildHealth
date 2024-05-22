using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Models.Patient;

namespace WildHealth.Application.Services.Appointments
{
    public class AppointmentAvailabilityService : IAppointmentAvailabilityService
    {
        private readonly IPatientsService _patientsService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public AppointmentAvailabilityService(
            IPatientsService patientsService, 
            IDateTimeProvider dateTimeProvider)
        {
            _patientsService = patientsService;
            _dateTimeProvider = dateTimeProvider;
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetForPatientIdAsync"/>
        /// </summary>
        /// <param name="patientId]"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AppointmentWithType>> GetForPatientIdAsync(int patientId)
        {
            var patient = await _patientsService.GetForAvailability(patientId);

            var available = new List<AppointmentWithType>();

            var healthCoachUnavailableReasonsForPatient = GetHealthCoachUnavailableReasonsForPatient(patient);
            var providerUnavailableReasonsForPatient = GetProviderUnavailableReasonsForPatient(patient);

            // Check for the health coach appointment
            if (!healthCoachUnavailableReasonsForPatient.Any())
            {
                available.Add(AppointmentWithType.HealthCoach);
            }

            // Check for the provider appointmnt
            if(!providerUnavailableReasonsForPatient.Any())
            {
                available.Add(AppointmentWithType.HealthCoachAndProvider);
            }

            return available;
        }


        /// <summary>
        /// Returns reasons why appointments are not available
        /// </summary>
        /// <param name="patientId"></param>
        public async Task<IEnumerable<AppointmentUnavailableModel>> GetUnavailableForPatientIdAsync(int patientId)
        {
            var patient = await _patientsService.GetForAvailability(patientId);

            var healthCoachUnavailableReasonsForPatient = GetHealthCoachUnavailableReasonsForPatient(patient);
            var providerUnavailableReasonsForPatient = GetProviderUnavailableReasonsForPatient(patient);

            return healthCoachUnavailableReasonsForPatient.Concat(providerUnavailableReasonsForPatient);
        }


        private IEnumerable<AppointmentUnavailableModel> GetHealthCoachUnavailableReasonsForPatient(Patient patient)
        {
            var results = new List<AppointmentUnavailableModel>();
            var patientDomain = PatientDomain.Create(patient);
            if(!patientDomain.IsAssignedHealthCoach)
            {
                results.Add(new AppointmentUnavailableModel(
                    patientId: patient.GetId(),
                    reason: $"Health coach is not yet assigned",
                    withType: AppointmentWithType.HealthCoach));
            }

            if (patientDomain.IsNextHealthCoachAppointmentInFuture(_dateTimeProvider.UtcNow()))
            {
                results.Add(new AppointmentUnavailableModel(
                    patientId: patient.GetId(),
                    reason: 
                    $"Appointment options dictate that the next health coach appointment will be available on {patientDomain.NextHealthCoachAppointmentAvailableDate}",
                    withType: AppointmentWithType.HealthCoach));
            }
            
            if(!patientDomain.HasHadProviderAppointmentIfHadICC)
            {
                results.Add(new AppointmentUnavailableModel(
                    patientId: patient.GetId(),
                    reason: $"Patient must have a Provider visit before another Health coach visit",
                    withType: AppointmentWithType.HealthCoach));
            }

            return results;
        }

        private IEnumerable<AppointmentUnavailableModel> GetProviderUnavailableReasonsForPatient(Patient patient)
        {
            var results = new List<AppointmentUnavailableModel>();
            var patientDomain = PatientDomain.Create(patient);
            var appointmentWithType = patient.User.PracticeId == 1
                ? AppointmentWithType.HealthCoachAndProvider
                : AppointmentWithType.Provider;

            if (!patientDomain.IsAssignedHealthCoach)
            {
                results.Add(new AppointmentUnavailableModel(
                    patientId: patient.GetId(),
                    reason: $"Health coach is not yet assigned",
                    withType: appointmentWithType));
            }

            if (!patientDomain.IsAssignedProvider)
            {
                results.Add(new AppointmentUnavailableModel(
                    patientId: patient.GetId(),
                    reason: $"Provider is not yet assigned",
                    withType: appointmentWithType));
            }

            if (!patientDomain.IsDnaCompleted)
            {
                results.Add(new AppointmentUnavailableModel(
                    patientId: patient.GetId(),
                    reason: $"DNA results are not yet completed",
                    withType: appointmentWithType));
            }

            if (!patientDomain.AreLabsCompletedIfOrdered)
            {
                results.Add(new AppointmentUnavailableModel(
                    patientId: patient.GetId(),
                    reason: $"Lab results are not yet completed",
                    withType: appointmentWithType));
            }

            if (patientDomain.IsNextProviderAppointmentInFuture(_dateTimeProvider.UtcNow()))
            {
                results.Add(new AppointmentUnavailableModel(
                    patientId: patient.GetId(),
                    reason: 
                        $"Appointment options dictate that the next provider appointment will be available on {patientDomain.NextProviderAppointmentAvailableDate}",
                    withType: appointmentWithType));
            }
            
            return results;
        }

    }
}
