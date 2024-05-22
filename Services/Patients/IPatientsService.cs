using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Infrastructure.Data.Queries.CustomSql.Models;

namespace WildHealth.Application.Services.Patients
{
    /// <summary>
    /// Manages patients
    /// </summary>
    public interface IPatientsService
    {
        /// <summary>
        /// Returns patient by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Patient> GetByIdAsync(int id);

        /// <summary>
        /// Returns all patient ids
        /// </summary>
        /// <returns></returns>
        Task<int?[]> GetAllPatientIds();
        
        /// <summary>
        /// Returns patient by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        Task<Patient> GetByIdAsync(int id, ISpecification<Patient> specification);

        /// <summary>
        /// <see cref="IPatientsService.GetForAvailability(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Patient> GetForAvailability(int id);

        /// <summary>
        /// <see cref="IPatientsService.GetByIdForCloneAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Patient> GetByIdForCloneAsync(int id);
        
        /// <summary>
        /// Returns patient by intake id
        /// </summary>
        /// <param name="intakeId"></param>
        /// <returns></returns>
        Task<Patient> GetByIntakeIdAsync(Guid intakeId);
        
        /// <summary>
        /// Returns patient by user id
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<Patient> GetByUserIdAsync(int userId);

        /// <summary>
        /// Returns patient by unversal id
        /// </summary>
        /// <param name="universalId"></param>
        /// <returns></returns>
        Task<Patient> GetByUserUniversalId(Guid universalId);

        /// <summary>
        /// Returns patient by user by first name, last name, and dob. Throws error if multiple patients found.
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="dob"></param>
        /// <returns></returns>
        Task<Patient> GetByFirstLastDobAsync(string firstName, string lastName, string dob);


        /// <summary>
        /// Returns all patients related to practice with no tracking 
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="locationIds"></param>
        /// <param name="assignedTo"></param>
        /// <param name="onBoardingStatus"></param>
        /// <param name="journeyStatuses"></param>
        /// <param name="isFellowship"></param>
        /// <param name="periodsIds"></param>
        /// <param name="coachesIds"></param>
        /// <param name="providersIds"></param>
        /// <param name="searchQuery"></param>
        /// <param name="sortingSource"></param>
        /// <param name="sortingDirection"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <param name="ignoreWithoutCompletedRegistration"></param>
        /// <param name="orderTypes"></param>
        /// <param name="stateAbbreviations"></param>
        /// <param name="isPlanActive"></param>
        /// <returns></returns>
        Task<(IEnumerable<Patient> patients, int totalCount)> SelectPatientsAsync(int practiceId,
            int[] locationIds,
            int? assignedTo = null,
            PatientOnBoardingStatus? onBoardingStatus = null,
            PatientJourneyStatus[]? journeyStatuses = null,
            bool? isFellowship = null,
            int[]? periodsIds = null,
            int[]? coachesIds = null,
            int[]? providersIds = null,
            string? searchQuery = null,
            string? sortingSource = null,
            string? sortingDirection = null,
            int? skip = null,
            int? take = null,
            bool ignoreWithoutCompletedRegistration = true,
            OrderType[]? orderTypes = null,
            string[]? stateAbbreviations = null,
            bool? isPlanActive = null);

        /// <summary>
        /// Creates new patient
        /// </summary>
        /// <param name="patient"></param>
        Task<Patient> CreatePatientAsync(Patient patient);

        /// <summary>
        /// Updates patient on boarding status
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        Task<Patient> UpdatePatientOnBoardingStatusAsync(Patient patient, PatientOnBoardingStatus status);

        /// <summary>
        /// Returns patient by integration id
        /// </summary>
        /// <returns></returns>
        Task<Patient> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor);

        /// <summary>
        /// Assigns patient to employees
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="employeeIds"></param>
        /// <returns></returns>
        Task<IEnumerable<int>> AssignToEmployeesAsync(Patient patient, int[] employeeIds);

        /// <summary>
        /// Links patient account to employee account
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        Task<int> LinkToEmployeeAsync(Patient patient, int employeeId);

        /// <summary>
        /// Updates patient options
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        Task<Patient> UpdatePatientOptionsAsync(Patient patient, PatientOptionsModel options);

        /// <summary>
        /// Links patient with integration system
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        Task<Patient> LinkPatientWithIntegrationSystemAsync(Patient patient, string integrationId, IntegrationVendor vendor);
        
        /// <summary>
        /// Locks patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        Task<Patient> LockPatientAsync(Patient patient);
        
        /// <summary>
        /// Unlocks patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        Task<Patient> UnlockPatientAsync(Patient patient);

        /// <summary>
        /// Update patients provider assignment
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        Task<Patient> UpdateProviderAssignmentAsync(Patient patient, Employee? provider);

        /// <summary>
        /// Update patient
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        Task<Patient> UpdateAsync(Patient patient);

        /// <summary>
        /// Returns patients with questionnaire results
        /// </summary>
        /// <returns></returns>
        Task<ICollection<Patient>> GetPatientsWithQuestionnaireResultsAsync();

        /// <summary>
        /// Returns patients of employees with fellow role (optionally can query by employeeId)
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="fellowId"></param>
        /// <returns></returns>
        Task<ICollection<Patient>> GetPracticumPatientsAsync(int practiceId, int? fellowId = null);

        /// <summary>
        /// Returns patients of employees with fellow role by fellow creation date range
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        Task<ICollection<Patient>> GetPracticumPatientsByFellowCreationAsync(int practiceId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Returns patients of employees with fellow role by roster identifier
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="rosterId"></param>
        /// <returns></returns>
        Task<ICollection<Patient>> GetPracticumPatientsByRosterAsync(int practiceId, int rosterId);

        /// <summary>
        /// Returns patient entity includes their appointments
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<Patient> GetPatientWithAppointments(int patientId);
        
        /// <summary>
        /// Returns insurance patients with upcoming appointments
        /// </summary>
        /// <returns></returns>
        Task<Patient[]> GetInsurancePatientsWithUpcomingAppointment(int practiceId, DateTime from, DateTime to);
        
        /// <summary>
        /// Fetches and returns patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        Task<Patient> FetchPatientAsync(int patientId);

        /// <summary>
        /// Sets fellowship note to patient options
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        Task<Patient> SetFellowshipNote(int patientId, string note);

        /// <summary>
        /// Get unassigned patients of a practice that have active membership
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        Task<ICollection<Patient>> GetUnassignedPracticePatientsAsync(int practiceId);

        /// <summary>
        /// Get Patients without conversation;
        /// </summary>
        /// <returns>patients</returns>
        Task<Patient[]> GetPatientsWOMessagesOrConversation();
        
        /// <summary>
        /// Get Patients with unread messages since an amount of time in minutes
        /// </summary>
        /// <returns>patients</returns>
        Task<Patient[]> GetAllWithUnreadMessagesSince(int minutes);
        
        /// <summary>
        /// Get Patients registered since an amount of days
        /// </summary>
        /// <returns>patients</returns>
        Task<Patient[]> GetAllWithDaysSinceSubscription(int days);


        /// <summary>
        /// Get Patients with active
        /// </summary>
        /// <returns>patients</returns>
        Task<Patient[]> GetAllWithActiveSubscription();
        
        /// <summary>
        /// Gets my patients response with the given filter
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<PatientStatusModel[]> GetMyPatientsWithFilters(int employeeId, MyPatientsFilterModel filter);

        /// <summary>
        /// Gets patients response with the given filter without assigment
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<PatientStatusModel[]> GetMyPatientsWithFiltersWithoutAssigment(MyPatientsFilterModel filter);


        /// <summary>
        /// Gets my patients response with no filters.
        /// </summary>
        /// <returns></returns>
        Task<PatientStatusModel[]> GetAllMyPatientsNoFilter();

        /// <summary>
        /// <see cref="IPatientsService.PatientsAssignedToEmployeeSinceAsync"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="since"></param>
        /// <returns></returns>
        Task<ICollection<Patient>> PatientsAssignedToEmployeeSinceAsync(int employeeId, DateTime since);

        /// <summary>
        /// Get list of patients filtered for sms notifications
        /// </summary>
        /// <param name="paymentPlans"></param>
        /// <param name="onlyPatientIds"></param>
        /// <param name="dateFrom"></param>
        /// <param name="dateTo"></param>
        /// <param name="hasCompletedAppointment"></param>
        /// <param name="hasActiveSubscription"></param>
        /// <returns></returns>
        Task<ICollection<Patient>> GetPatientsForNotificationAsync(string[] paymentPlans, int[] onlyPatientIds, DateTime? dateFrom = null,
            DateTime? dateTo = null, bool? hasCompletedAppointment = null, bool? hasActiveSubscription = null);

        /// <summary>
        /// Patients that are considered at risk because they do not have an ICC or ICC scheduled date is after a certain threshold
        /// </summary>
        /// <returns></returns>
        Task<AtRiskIccPatientRaw[]> AtRiskIccDue();


        /// <summary>
        /// Patients that are considered at risk because they do not have an IMC or IMC scheduled date is after a certain threshold
        /// </summary>
        /// <returns></returns>
        Task<AtRiskImcPatientRaw[]> AtRiskImcDue();

        Task<PatientCohortModelRaw> GetPatientCohort(int patientId);

        Task<bool> IsPremium(int patientId);
        
        Task<bool> HasLabs(int patientId);

        /// <summary>
        /// Returns the universal ID associated with the provided patientId
        /// </summary>
        /// <returns></returns>
        Task<string> GetUniversalIdForPatientId(int patientId);
    }
}
