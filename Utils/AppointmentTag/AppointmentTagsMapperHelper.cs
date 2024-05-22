using System.Linq;
using System.Collections.Generic;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Appointments;
using AutoMapper;
using WildHealth.Domain.Models.Patient;

namespace WildHealth.Application.Utils.AppointmentTag;

public class AppointmentTagsMapperHelper: IAppointmentTagsMapperHelper
{
    private readonly IMapper _mapper;

    public AppointmentTagsMapperHelper(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    /// <summary>
    /// <see cref="IAppointmentTagsMapperHelper.MapAppointmentWithTags"/>
    /// </summary>
    /// <param name="appointments"></param>
    /// <returns></returns>
    public ICollection<EmployeeAppointmentModel> MapAppointmentWithTags(IEnumerable<Appointment> appointments)
    {
        var mappedResult = new List<EmployeeAppointmentModel>(); 
            
        foreach (var appointment in appointments.Where(a => a.Patient != null))
        {
            var mappedAppointment = _mapper.Map<EmployeeAppointmentModel>(appointment);

            mappedAppointment.Tags = MapAppointmentTags(appointment);
                
            mappedResult.Add(mappedAppointment);
        }
        
        return mappedResult;
    }

    /// <summary>
    /// <see cref="IAppointmentTagsMapperHelper.MapAppointmentTags"/>
    /// </summary>
    /// <param name="appointment"></param>
    /// <returns></returns>
    public AppointmentTagsModel[] MapAppointmentTags(Appointment appointment)
    {
        var tags = new List<AppointmentTagsModel>();

        var patientDomain = PatientDomain.Create(appointment.Patient);

        var hasNewLabsSinceLastVisit = patientDomain.HasNewLabsSinceLastVisit();
            
        if (hasNewLabsSinceLastVisit.HasValue && hasNewLabsSinceLastVisit.Value)
        {
            tags.Add(new AppointmentTagsModel
            (
                displayValue: "Recent Labs",
                order: 1
            ));
        }

        if (appointment.Patient.CurrentSubscription?.PaymentPrice?.PaymentPeriod?.PaymentPlan?.Name is not null 
            && appointment.Patient.CurrentSubscription.PaymentPrice.PaymentPeriod.PaymentPlan.Name.Equals("PRECISION_CARE_PACKAGE"))
        {
            tags.Add(new AppointmentTagsModel
            (
                displayValue: "Care Package Patient",
                order: 0
            ));
        }
            
        foreach (var tag in appointment.Patient.TagRelations)
        {
            tags.Add(new AppointmentTagsModel
            (
                displayValue: tag.Tag.Name,
                order: tags.Count
            ));
        }

        return tags.ToArray();
    }
}