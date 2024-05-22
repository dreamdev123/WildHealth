using System.Threading.Tasks;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Services.PatientPharmacyInfos;

public interface IPatientPharmacyInfoService
{
    public Task<PatientPharmacyInfo> CreateOrUpdateAsync(PatientPharmacyInfo patientPharmacyInfo);

    public Task<PatientPharmacyInfo?> GetByPatientIdAsync(int patientId);
}