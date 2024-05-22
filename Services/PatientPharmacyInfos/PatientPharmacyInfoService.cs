using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.PatientPharmacyInfos;

public class PatientPharmacyInfoService: IPatientPharmacyInfoService
{
    private readonly IGeneralRepository<PatientPharmacyInfo> _patientPharmacyInfoRepository;

    public PatientPharmacyInfoService(IGeneralRepository<PatientPharmacyInfo> patientPharmacyInfoRepository)
    {
        _patientPharmacyInfoRepository = patientPharmacyInfoRepository;
    }

    public async Task<PatientPharmacyInfo> CreateOrUpdateAsync(PatientPharmacyInfo patientPharmacyInfo)
    {
        var pharmacyInfo = await GetByPatientIdAsync(patientPharmacyInfo.PatientId);

        if (pharmacyInfo is not null)
        {
            pharmacyInfo.Name = patientPharmacyInfo.Name;
            pharmacyInfo.Phone = patientPharmacyInfo.Phone;
            pharmacyInfo.StreetAddress = patientPharmacyInfo.StreetAddress;
            pharmacyInfo.City = patientPharmacyInfo.City;
            pharmacyInfo.ZipCode = patientPharmacyInfo.ZipCode;
            pharmacyInfo.State = patientPharmacyInfo.State;
            pharmacyInfo.Country = patientPharmacyInfo.Country;
            
            _patientPharmacyInfoRepository.Edit(pharmacyInfo);

            await _patientPharmacyInfoRepository.SaveAsync();
        
            return pharmacyInfo;
        }
        
        await _patientPharmacyInfoRepository.AddAsync(patientPharmacyInfo);

        await _patientPharmacyInfoRepository.SaveAsync();

        return patientPharmacyInfo;
    }

    public async Task<PatientPharmacyInfo?> GetByPatientIdAsync(int patientId)
    {
        var result = await _patientPharmacyInfoRepository
                .All()
                .RelatedToPatient(patientId)
                .FirstOrDefaultAsync();

        return result;
    }
}