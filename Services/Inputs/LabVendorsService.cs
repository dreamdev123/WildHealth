using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Settings;
using WildHealth.Common.Constants;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// <see cref="ILabVendorsService"/>
    /// </summary>
    public class LabVendorsService : ILabVendorsService
    {
        private static readonly string[] LabVendorSettings =
        {
            SettingsNames.Labs.DefaultVendorName
        };

        
        private readonly IGeneralRepository<LabVendor> _labVendorsRepository;
        private readonly ISettingsManager _settingsManager;

        public LabVendorsService(
            IGeneralRepository<LabVendor> labVendorsRepository,
            ISettingsManager settingsManager
            )
        {
            _labVendorsRepository = labVendorsRepository;
            _settingsManager = settingsManager;
        }
        
        /// <summary>
        /// <see cref="ILabVendorsService.Create"/>
        /// </summary>
        /// <param name="labVendor"></param>
        /// <returns></returns>
        public async Task<LabVendor> Create(LabVendor labVendor)
        {
            await _labVendorsRepository.AddAsync(labVendor);

            await _labVendorsRepository.SaveAsync();

            return labVendor;
        }

        /// <summary>
        /// <see cref="ILabVendorsService.GetByName"/>
        /// </summary>
        /// <returns></returns>
        public async Task<LabVendor> GetByName(string name)
        {
            return (await _labVendorsRepository
                .All()
                .ByName(name)
                .FirstOrDefaultAsync())!;
        }

        /// <summary>
        /// <see cref="ILabVendorsService.GetForPatient"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<LabVendor> GetForPatient(Patient patient)
        {
            var settings = await _settingsManager.GetSettings(LabVendorSettings, patient.User.PracticeId);

            var labVendorName = settings[SettingsNames.Labs.DefaultVendorName];

            return await GetByName(labVendorName);
        }
    }
}
        