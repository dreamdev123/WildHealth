using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Employees;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Models.Patient;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Models.Patients;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class CreatePaymentIntegrationAccountCommandHandler : IRequestHandler<CreatePaymentIntegrationAccountCommand, IEnumerable<PatientCreatedModel>>
    {
        private readonly IPatientsService _patientsService;
        private readonly ILogger _logger;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        
        public CreatePaymentIntegrationAccountCommandHandler(
            IPatientsService patientsService,
            ILogger<CreatePaymentIntegrationAccountCommandHandler> logger,
            IIntegrationServiceFactory integrationServiceFactory)
        {
            _patientsService = patientsService;
            _logger = logger;
            _integrationServiceFactory = integrationServiceFactory;
        }

        public async Task<IEnumerable<PatientCreatedModel>> Handle(CreatePaymentIntegrationAccountCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating payment integration account for [PatientId] = {command.PatientId} has started");

            var patientCreatedModels = Enumerable.Empty<PatientCreatedModel>();
            
            var patient =
                await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientUserSpecification);

            var patientDomain = PatientDomain.Create(patient);
            var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

            if (!patientDomain.IsLinkedWithIntegrationSystem(integrationService.IntegrationVendor))
            {
                patientCreatedModels = await LinkPatientToIntegrationSystemAsync(patient, Enumerable.Empty<Employee>(), integrationService);
            }

            _logger.LogInformation($"Creating payment integration account for [PatientId] = {command.PatientId} has finished");

            return patientCreatedModels;
        }
        
        
        private async Task<IEnumerable<PatientCreatedModel>> LinkPatientToIntegrationSystemAsync(Patient patient, IEnumerable<Employee> assigmentEmployees, IWildHealthIntegrationService integrationService)
        {
            var patientCreatedModels = await integrationService.CreatePatientAsync(patient, assigmentEmployees);
            
            foreach(var patientCreatedModel in patientCreatedModels) {
                await _patientsService.LinkPatientWithIntegrationSystemAsync(
                    patient, 
                    patientCreatedModel.IntegrationId, 
                    patientCreatedModel.IntegrationVendor);
            }
            
            return patientCreatedModels;
        }
    }
}