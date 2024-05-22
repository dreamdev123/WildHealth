using System.Collections.Generic;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Appointments;

namespace WildHealth.Application.Utils.AppointmentTag;

public interface IAppointmentTagsMapperHelper
{
    /// <summary>
    /// Returns appointment tags
    /// </summary>
    /// <param name="appointments"></param>
    /// <returns></returns>
    ICollection<EmployeeAppointmentModel> MapAppointmentWithTags(IEnumerable<Appointment> appointments);
    
    /// <summary>
    /// Maps appointment tags
    /// </summary>
    /// <param name="appointment"></param>
    /// <returns></returns>
    AppointmentTagsModel[] MapAppointmentTags(Appointment appointment);
}