using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Utils.LabNameProvider;
using WildHealth.Common.Models.Inputs;
using WildHealth.ClarityCore.Models.Labs;
using WildHealth.ClarityCore.WebClients.Labs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Exceptions;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class CorrectLabResultsCommandHandler : IRequestHandler<CorrectLabResultsCommand>
    {
        private readonly ITransactionManager _transactionManager;
        private readonly ILabNameProvider _labNameProvider;
        private readonly IInputsService _inputsService;
        private readonly ILabsWebClient _labsWebClient;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CorrectLabResultsCommandHandler(
            ITransactionManager transactionManager,
            ILabNameProvider labNameProvider,
            IInputsService inputsService, 
            ILabsWebClient labsWebClient,
            IMediator mediator, 
            ILogger<CorrectLabResultsCommandHandler> logger)
        {
            _transactionManager = transactionManager;
            _labNameProvider = labNameProvider;
            _inputsService = inputsService;
            _labsWebClient = labsWebClient;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(CorrectLabResultsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Correcting lab results for patient with id: {command.PatientId} has been started.");
            
            var patientId = command.PatientId;
            
            var reportId = command.ReportId;

            var orderNumber = command.OrderNumber;

            var fileInputs = await _inputsService.GetFileInputsAsync(patientId);

            var wrongFileInput = GetWrongLabFileInput(fileInputs, orderNumber);
            
            var labResults = await FetchLabResultsAsync(patientId, reportId);

            var (bytes, fileName) = await FetchLabResultsDocumentAsync(patientId, reportId);

            var aggregator = await _inputsService.GetAggregatorAsync(patientId);
            
            var uploadInputsFileCommand = new UploadInputsFileCommand(
                type: FileInputType.LabResults,
                dataProvider: FileInputDataProvider.LabCorpElation,
                bytes: bytes,
                fileName: fileName,
                patientId: patientId
            );
            
            await using var transaction = _transactionManager.BeginTransaction();

            try
            {
                if (!(wrongFileInput is null))
                {
                    await _inputsService.DeleteFileInputAsync(wrongFileInput);
                }
                
                await _inputsService.UpdateLabInputsAsync(await ToModel(labResults, aggregator), patientId);

                var file = await _mediator.Send(uploadInputsFileCommand, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                
                await _mediator.Publish(new FileInputsUploadedEvent(patientId, FileInputType.LabResults, file.File.Uri, orderNumber), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError($"Receiving lab results for patient with id: {command.PatientId} has been failed - {e}");
                
                await transaction.RollbackAsync(cancellationToken);
                
                throw;
            }
            
            _logger.LogInformation($"Correcting lab results for patient with id: {command.PatientId} has been finished.");
        }
        
        #region private

        /// <summary>
        /// Returns wrong lab inputs file
        /// </summary>
        /// <param name="fileInputs"></param>
        /// <param name="orderNumber"></param>
        /// <returns></returns>
        private FileInput? GetWrongLabFileInput(IEnumerable<FileInput> fileInputs, string orderNumber)
        {
            var fileName = $"Lab_Results_{orderNumber}.";
            
            return fileInputs.FirstOrDefault(x => 
                x.Type == FileInputType.LabResults 
                && x.DataProvider == FileInputDataProvider.LabCorpElation
                && x.File.Name.Contains(fileName));
        }
        
        /// <summary>
        /// Fetches and returns all lab results related to corresponding lab report
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        private async Task<LabResultModel[]> FetchLabResultsAsync(int patientId, int reportId)
        {
            var results = await _labsWebClient.GetPatientLabResultsAsync(patientId.ToString(),reportId);

            return results.Results.ToArray();
        }
        
        /// <summary>
        /// Fetches and returns all lab results related to corresponding lab report
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="reportId"></param>
        /// <returns></returns>
        private async Task<(byte[], string)> FetchLabResultsDocumentAsync(int patientId, int reportId)
        {
            var command = new DownloadLabOrderResultsCommand(
                patientId: patientId,
                reportId: reportId
            );
            
            return await _mediator.Send(command);
        }
        
        /// <summary>
        /// Prepares update lab inputs model based on LabResults
        /// </summary>
        /// <param name="results"></param>
        /// <param name="aggregator"></param>
        /// <returns></returns>
        private async Task<UpdateLabInputsModel> ToModel(LabResultModel[] results, InputsAggregator aggregator)
        {
            var correctModels = new List<CorrectLabInputValueModel>();
            
            var correctedResults = results
                .Where(x => x.Status.ToUpper() == "CORRECTED")
                .ToArray();

            foreach (var result in correctedResults)
            {
                try
                {
                    var labDate = result.Request.ObservationDateTime ?? DateTime.UtcNow.Date;
                    var labName = await GetLabNameForResult(result);
                    var existingValues = aggregator.GetLabInputValuesSet(labDate);
                    var wrongValue = existingValues.FirstOrDefault(x => x.Name == labName);

                    if (wrongValue is null)
                    {
                        continue;
                    }
                    
                    correctModels.Add(new CorrectLabInputValueModel
                    {
                        Id = wrongValue.GetId(),
                        StringValue = result.Value,
                        Note = result.Notes
                    });
                }
                catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                   // Ignore
                }
            }
            
            return new UpdateLabInputsModel
            {
                ToCorrect = correctModels.ToArray(),
                LabInputMutations = Array.Empty<LabInput>(),
                ToUpdate = Array.Empty<UpdateLabInputValueModel>(),
                ToCreate = Array.Empty<CreateLabInputValueModel>(),
                ToAppend = Array.Empty<AppendLabInputValueModel>(),
                Ranges = Array.Empty<UpdateLabInputRangeModel>(),
                ToDelete = Array.Empty<int>()
            };
        }

        private async Task<string> GetLabNameForResult(LabResultModel result)
        {
            var labName = await _labNameProvider.WildHealthNameForResultCode(result.ResultCode);

            // If we don't have a formatting for this lab name, grab the one from the results
            if (string.IsNullOrEmpty(labName))
            {
                labName = result.ResultCodeLabel;
            }

            return labName;
        }
        
        #endregion
    }
}