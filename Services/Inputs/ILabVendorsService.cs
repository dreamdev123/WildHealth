using System.Threading.Tasks;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// Provides methods for working with lab vendors
    /// </summary>
    public interface ILabVendorsService
    {
        /// <summary>
        /// <see cref="ILabNamesService.Create"/>
        /// </summary>
        /// <param name="labVendor"></param>
        /// <returns></returns>
        Task<LabVendor> Create(LabVendor labVendor);

        /// <summary>
        /// <see cref="ILabNamesService.GetByName"/>
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Task<LabVendor> GetByName(string name);

        /// <summary>
        /// <see cref="ILabVendorsService.GetForPatient"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        Task<LabVendor> GetForPatient(Patient patient);
    }
}