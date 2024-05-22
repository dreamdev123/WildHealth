using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.PatientCreator;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Managers.TransactionManager;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class AddAggregationInputsOnPatientCommandHandler : IRequestHandler<AddPatientAggregationInputCommand>
    {
        private readonly IPatientsService _patientsService;
        private readonly IPatientCreator _patientCreator;
        private readonly ILogger<AddAggregationInputsOnPatientCommandHandler> _logger;

        public AddAggregationInputsOnPatientCommandHandler(IPatientsService patientsService, IPatientCreator patientCreator, ILogger<AddAggregationInputsOnPatientCommandHandler> logger)
        {
            _patientsService = patientsService;
            _patientCreator = patientCreator;
            _logger = logger;

        }

        public async Task Handle(AddPatientAggregationInputCommand command, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(command.PatientId, PatientSpecifications.PatientWithAggregationInputs);
            
            _logger.LogInformation($"Starting adding Aggregation inputs to new Patient [id] : {command.PatientId}");
            
            try
            {
                if (patient?.InputsAggregator?.LabInputs?.Any() ?? false)
                {
                    await CatchUpPatientAggregationInputs(patient.User, patient, patient.Location);

                    return;
                }
                
                if (patient is not null)
                    await AddPatientAggregationInputs(patient.User, patient, patient.Location);
            }
            catch (Exception err)
            {
                _logger.LogWarning($"Error adding inputs Aggregator in checkout with error : {err.StackTrace}");
                
                throw new ApplicationException("Error adding inputs Aggregator in checkout with error: ", err);
            }
            
            _logger.LogInformation($"Adding Aggregation inputs to new Patient [id] : {command.PatientId} end Successfully");
        }
        
        private async Task AddPatientAggregationInputs(User user, Patient patient, Location location)
        {
            patient = await _patientCreator.AddPatientInputsAggregator(user, patient, location);
            
            await _patientsService.UpdateAsync(patient);
            
        }
        
        private async Task CatchUpPatientAggregationInputs(User user, Patient patient, Location location)
        {
            patient = await _patientCreator.CatchUpPatientInputsAggregator(user, patient, location);
            
            await _patientsService.UpdateAsync(patient);
            
        }
    }
}
