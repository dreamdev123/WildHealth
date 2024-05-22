using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Net;
using WildHealth.Shared.Data.Repository;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Shared.Exceptions;


namespace WildHealth.Application.Services.Integrations
{
    /// <summary>
    /// Provides methods for returning various patient identities in other integrations
    /// </summary>
    public class PatientIdentitiesService : IPatientIdentitiesService
    {
        private readonly IGeneralRepository<WildHealth.Domain.Entities.Integrations.Integration> _integrationsRepository;
        private readonly IGeneralRepository<Patient> _patientsRepository;

        public PatientIdentitiesService(
            IGeneralRepository<WildHealth.Domain.Entities.Integrations.Integration> integrationsRepository,
                IGeneralRepository<Patient> patientsRepository
            )
        {
            _integrationsRepository = integrationsRepository;
            _patientsRepository = patientsRepository;
        }
        /// <summary>
        /// Returns the patient identity models for a given unique patient identity combination 
        /// </summary>
        /// <param name="integrationVendor"></param>
        /// <param name="integrationVendorId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PatientIdentityModel>> GetAsync(IntegrationVendor integrationVendor, string integrationVendorId)
        {
            var thisIntegration = await _integrationsRepository
                .All()
                .IncludePatientFromPatient()
                .Where(o => o.Vendor == integrationVendor && o.Value == integrationVendorId)
                .FirstOrDefaultAsync();

            Patient? patient = null;

            if (thisIntegration == null && integrationVendor == IntegrationVendor.Clarity)
            {
                patient = await _patientsRepository
                    .All()
                    .IncludeIntegrations()
                    .Where(o => o.Id == Convert.ToInt32(integrationVendorId))
                    .FirstOrDefaultAsync();
            }
            else
            {
                if (thisIntegration != null)
                {
                    patient = await _patientsRepository
                        .All()
                        .IncludeIntegrations()
                        .Where(o => o.Id == thisIntegration.PatientIntegration.Patient.GetId())
                        .FirstOrDefaultAsync();
                }
            }

            if (patient == null)
            {
                throw new AppException(HttpStatusCode.NotFound,
                    $"Patient with [IntegrationVendor] = {integrationVendor} and [IntegrationVendorId] = {integrationVendorId} was not found");
            }

            return GetAllIntegrationModelsForPatient(patient);
        }

        private IEnumerable<PatientIdentityModel> GetAllIntegrationModelsForPatient(Patient patient)
        {
            var integrationResults = patient.Integrations.Select(o => new PatientIdentityModel(
                integrationVendor: o.Integration.Vendor,
                integrationVendorId: o.Integration.Value
            ));

            return integrationResults.Concat(new List<PatientIdentityModel>()
            {
                new PatientIdentityModel(
                    integrationVendor: IntegrationVendor.Clarity,
                    integrationVendorId: patient.GetId().ToString()
                )
            });
        }
    }
}