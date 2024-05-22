using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Services.Inputs;
using WildHealth.ClarityCore.WebClients.Labs;
using WildHealth.ClarityCore.Models.Labs;
using WildHealth.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Infrastructure.Data.Queries;
using Microsoft.EntityFrameworkCore;
using MediatR;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class CleanUpLabInputValueDatesCommandHandler : IRequestHandler<CleanUpLabInputValueDatesCommand>
    {
        private readonly IInputsService _inputsService;
        private readonly ILabsWebClient _labsWebClient;
        private readonly IPatientsService _patientsService;
        private readonly ILogger _logger;
        private readonly IGeneralRepository<InputsAggregator> _inputsRepository;

        public CleanUpLabInputValueDatesCommandHandler(
            IInputsService inputsService, 
            ILabsWebClient labsWebClient,
            IPatientsService patientsService,
            ILogger<CleanUpLabInputValueDatesCommandHandler> logger,
            IGeneralRepository<InputsAggregator> inputsRepository)
        {
            _inputsService = inputsService;
            _labsWebClient = labsWebClient;
            _patientsService = patientsService;
            _logger = logger;
            _inputsRepository = inputsRepository;
        }

        public async Task Handle(CleanUpLabInputValueDatesCommand command, CancellationToken cancellationToken)
        {
            var patientId = command.PatientId;
            
            var reportId = command.ReportId;

            var patient = await _patientsService.GetByIdAsync(patientId);

            // Need to verify that the patient has a practice in order to determine the default lab vendor to use (configured in licensing)
            AssertPatientWithPracticeExists(patient, patientId, reportId);

            var labResults = await FetchLabResultsAsync(patientId, reportId);

            var aggregator = await _inputsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeLabInputsWithNames()
                .IncludePatient()
                .FirstAsync(cancellationToken: cancellationToken);

            try
            {
                
                // Pulling lab date this way because there were instances of results coming in where the request was null
                // We can assume that all results within a common reportId have the same observation date
                var firstResultWithRequest = labResults.Where(o => o.Request != null).FirstOrDefault();

                var labDate = firstResultWithRequest?.Request.ObservationDateTime?.Date ?? DateTime.UtcNow.Date;

                var potentialWrongDate = firstResultWithRequest?.CreatedAt.Date;

                var potentialWrongValues = new List<LabInputValue>(aggregator.GetLabInputValuesSet(potentialWrongDate));

                var existingValues = aggregator.GetLabInputValuesSet(labDate);

                var properDatSetId = existingValues.FirstOrDefault()?.DataSetId;

                var deletes = new List<int>();
                
                var updates = new List<(int, string, DateTime)>();
            
                if (potentialWrongValues.Any())
                {
                    _logger.LogInformation($"It appears that {potentialWrongValues.Count} labs have the [Date] = {potentialWrongDate} but should be [Date] = {labDate}");

                    foreach (var val in potentialWrongValues)
                    {
                        // If the same value is in both existing and wrongValues, then it means wrongValues is a bad duplicate and we should delete it
                        if (existingValues.Any(o => o.Name == val.Name && o.Value == val.Value))
                        {
                            deletes.Add(val.GetId());

                            continue;
                        }
                    
                        // Otherwise, it was created on the wrong date and we need to move it over
                        if (!string.IsNullOrEmpty(properDatSetId))
                        {
                            updates.Add((val.GetId(), properDatSetId, labDate));

                            continue;
                        }
                    }
                }

                foreach (var deleteId in deletes)
                {
                    aggregator.DeleteLabInputValue(deleteId);
                }

                foreach (var update in updates)
                {
                    aggregator.CleanLabInputValueDataSetIdAndDate(update.Item1, update.Item2, update.Item3);
                }
            
                _inputsRepository.Edit(aggregator);

                await _inputsRepository.SaveAsync();

            }
            catch (Exception e)
            {
                _logger.LogError($"Cleaning up lab results for patient with id: {command.PatientId} has failed - {e}");
                
                throw;
            }
        }
        
        #region private


        /// <summary>
        /// Fetches and returns all lab results related to corresponding lab report
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        private async Task<LabResultModel[]> FetchLabResultsAsync(int patientId, int reportId)
        {
            var results = await _labsWebClient.GetPatientLabResultsAsync(patientId.ToString(), reportId);

            return results.Results.ToArray();
        }

        private void AssertPatientWithPracticeExists(Patient patient, int patientId, int reportId)
        {
            if(patient.User?.Practice == null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.BadRequest, "Cannot receive lab results for patient that does not have an associated practice", exceptionParam);
            }
        }

        #endregion
    }
}