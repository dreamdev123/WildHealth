using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Application.Services.Patients;
using System.Collections.Generic;
using WildHealth.Domain.Entities.Attachments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Patient;
using WildHealth.Integration.Factories.IntegrationServiceFactory;

namespace WildHealth.Application.Cloners.Patients
{
    /// <summary>
    /// <see cref="IPatientCloner"/>
    /// </summary>
    public class PatientCloner : IPatientCloner
    {
        private readonly IPatientsService _patientsService;
        private readonly ILogger _logger;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;

        public PatientCloner(
            IPatientsService patientsService,
            ILogger<PatientCloner> logger,
            IIntegrationServiceFactory integrationServiceFactory)
        {
            _patientsService = patientsService;
            _logger = logger;
            _integrationServiceFactory = integrationServiceFactory;
        }

        /// <summary>
        /// <see cref="IPatientCloner.ClonePatientForNewPracticeAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="coreEmail"></param>
        /// <param name="toPractice"></param>
        /// <returns></returns>
        public async Task<Patient> ClonePatientForNewPracticeAsync(Patient patient, string coreEmail,
            Practice toPractice)
        {
            _logger.LogInformation($"Started cloning patient with [Id] = {patient.GetId()} to practice with [Id] = {toPractice.GetId()}");
            
            var clone = patient.DeepClone();
            var clonedPatientDomain = PatientDomain.Create(clone);
            
            // Ensure we have core email assigned
            clone.User.Email = coreEmail;
            clone.User.Identity.Email = coreEmail;

            // Update the LocationId for the new patient
            clone.ChangeLocation(toPractice.Locations.Any()
                ? toPractice.Locations.First()
                : new Location(toPractice));

            clone.User.PracticeId = toPractice.GetId();
            clone.User.Identity.PracticeId = toPractice.GetId();

            // Save first, now we have foreign keys and can save rest
            await _patientsService.CreatePatientAsync(clone);

            // Clone user attachemnts use old attachemnts and new user
            var cloneAttachments = new List<UserAttachment>();

            foreach (var attachment in patient.User.Attachments)
            {
                cloneAttachments.Add(new UserAttachment(clone.User, attachment.Attachment));
            }

            clone.User.Attachments = cloneAttachments;

            var inputsAggregatorId = clone.InputsAggregator.GetId();
            foreach (var labInput in clone.InputsAggregator.LabInputs)
            {
                var cloneLabInput = patient.InputsAggregator.LabInputs.FirstOrDefault(o => o.Name.Equals(labInput.Name));

                if (cloneLabInput == null) continue;
                
                foreach (var value in cloneLabInput.Values)
                {
                    var res = value.DeepClone();
                    res.AggregatorId = inputsAggregatorId;

                    // Add the value
                    labInput.AddValue(res);
                }
            }

            foreach (var vital in clone.InputsAggregator.Vitals)
            {
                var cloneVital = patient.InputsAggregator.Vitals.FirstOrDefault(o => o.Name.Equals(vital.Name));

                if (cloneVital == null) continue;
                
                foreach (var value in cloneVital.Values)
                {
                    vital.AddValue(value.Date, value.Value, value.SourceType);
                }
            }

            await _patientsService.UpdateAsync(clone);

            if (!clonedPatientDomain.IsLinkedWithIntegrationSystem(IntegrationVendor.Stripe))
            {
                // Create integrations if they don't already exist
                var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);
                
                await integrationService.CreatePatientAsync(clone, clone.Employees.Select(o => o.Employee));
            }
            
            _logger.LogInformation($"Finished cloning patient with [Id] = {patient.GetId()} to practice with [Id] = {toPractice.GetId()}");

            return clone;
        }
    }
}