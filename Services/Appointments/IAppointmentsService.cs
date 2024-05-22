using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.Services.Appointments;

public interface IAppointmentsService
{
    /// <summary>
    /// Returns appointment by id
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<Appointment> GetByIdAsync(int id);
        
    /// <summary>
    /// Returns appointment by id
    /// </summary>
    /// <param name="ids"></param>
    /// <returns></returns>
    Task<Appointment[]> GetByIdsAsync(int[] ids);

    /// <summary>
    /// Returns appointment by scheduler system id
    /// </summary>
    /// <param name="schedulerSystemId"></param>
    /// <returns></returns>
    Task<Appointment?> GetBySchedulerSystemIdAsync(string schedulerSystemId);
        
    /// <summary>
    /// Returns all patient appointments by status, start date, end date
    /// </summary>
    /// <param name="patientId"></param>
    /// <param name="status"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <returns></returns>
    Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(
        int patientId, 
        AppointmentStatus status = AppointmentStatus.All, 
        DateTime? startDate = null, 
        DateTime? endDate = null);

    /// <summary>
    /// Returns all employee appointments by status, start date, end date
    /// </summary>
    /// <param name="employeeId"></param>
    /// <param name="status"></param>
    /// <param name="startDate"></param>
    /// <param name="endDate"></param>
    /// <param name="onlyActive"></param>
    /// <returns></returns>
    Task<IEnumerable<Appointment>> GetEmployeeAppointmentsAsync(int employeeId,
        AppointmentStatus status = AppointmentStatus.All, 
        DateTime? startDate = null, 
        DateTime? endDate = null,
        bool onlyActive = false);
    
    /// <summary>
    /// Creates appointment
    /// </summary>
    /// <param name="appointment"></param>
    /// <returns></returns>
    Task<Appointment> CreateAppointmentAsync(Appointment appointment);
        
    /// <summary>
    /// Edits patient appointment
    /// </summary>
    /// <param name="appointment"></param>
    /// <returns></returns>
    Task<Appointment> EditAppointmentAsync(Appointment appointment);

    /// <summary>
    /// Asserts time range is available for new appointment
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="employeeId"></param>
    /// <returns></returns>
    Task<bool> AssertTimeAvailableAsync(DateTime from, DateTime to, int employeeId);

    /// <summary>
    /// Returns available appointment types
    /// </summary>
    /// <param name="patientId"></param>
    /// <returns></returns>
    Task<IEnumerable<AppointmentTypeModel>> GetAvailableTypesAsync(int patientId);

    /// <summary>
    /// Returns all practice appointment types
    /// </summary>
    /// <param name="practiceId"></param>
    /// <returns></returns>
    Task<IEnumerable<AppointmentType>> GetAllTypesAsync(int practiceId);

    /// <summary>
    /// Returns corresponding appointment type by configuration id
    /// </summary>
    /// <param name="practiceId"></param>
    /// <param name="configurationId"></param>
    /// <returns></returns>
    Task<(AppointmentType, AppointmentTypeConfiguration)> GetTypeByConfigurationIdAsync(
        int practiceId,
        int configurationId);
    
    /// <summary>
    /// Returns appointment insurance type
    /// </summary>
    /// <param name="appointmentPurpose"></param>
    /// <returns></returns>
    Task<AppointmentInsuranceType?> GetAppointmentInsuranceTypeByAppointmentPurpose(
        AppointmentPurpose appointmentPurpose);

    /// <summary>
    /// Returns appointment by integration id
    /// </summary>
    /// <param name="integrationId"></param>
    /// <param name="vendor"></param>
    /// <param name="purpose"></param>
    /// <returns></returns>
    Task<Appointment?> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor, string purpose);

    /// <summary>
    /// Returns EmployeeAppointments by appointment ids
    /// </summary>
    /// <param name="appointmentIds"></param>
    /// <returns></returns>
    Task<EmployeeAppointmentModel[]> GetEmployeeAppointmentsByIdsAsync(int[] appointmentIds);

    /// <summary>
    /// Returns SequenceNumbers by appointmentIds and patientIds
    /// </summary>
    /// <param name="models"></param>
    /// <returns></returns>
    Task<IEnumerable<AppointmentsSequenceInfoModel>> GetSequenceNumbers(AppointmentSequenceNumbersModel[] models);

    /// <summary>
    /// Acknowledge the SignOff for the given appointment
    /// </summary>
    /// <param name="id"></param>
    /// <param name="appointmentSignOffType"></param>
    /// <returns></returns>
    Task<Appointment> SignOffAppointment(int id, AppointmentSignOffType appointmentSignOffType);

    /// <summary>
    /// Returns appointment by meeting system id
    /// </summary>
    /// <param name="meetingSystemId"></param>
    /// <returns></returns>
    Task<Appointment> GetByMeetingSystemIdAsync(long meetingSystemId);
}