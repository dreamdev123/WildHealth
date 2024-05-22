using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Employees;

namespace WildHealth.Application.Utils.Patients;

public interface IPatientFilterHelper
{
    PatientStatusModel[] HandlePatientFilter(PatientStatusModel[] patientStatusModel, MyPatientsFilterModel filter, Employee emp);
    
    PatientStatusModel[] HandlePatientFilterWithoutAssigment(PatientStatusModel[] patientStatusModel, MyPatientsFilterModel filter);
    
    bool HandlePatientFilter(PatientStatusModel patientStatusModel, MyPatientsFilterModel filter, Employee emp);
}