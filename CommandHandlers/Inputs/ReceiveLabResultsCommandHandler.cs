using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using EllipticCurve.Utils;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Utils.LabNameProvider;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Utils.LabNameRangeProvider;
using WildHealth.ClarityCore.WebClients.Labs;
using WildHealth.ClarityCore.Models.Labs;
using WildHealth.Common.Models.Inputs;
using WildHealth.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Inputs;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Settings;
using WildHealth.Domain.Enums.User;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class ReceiveLabResultsCommandHandler : IRequestHandler<ReceiveLabResultsCommand>
    {
        private readonly ITransactionManager _transactionManager;
        private readonly ILabNameProvider _labNameProvider;
        private readonly IInputsService _inputsService;
        private readonly ILabsWebClient _labsWebClient;
        private readonly ILabNamesService _labNamesService;
        private readonly ILabNameAliasesService _labNameAliasesService;
        private readonly ILabVendorsService _labVendorsService;
        private readonly ILabNameRangesService _labNameRangesService;
        private readonly ILabNameRangeProvider _labNameRangeProvider;
        private readonly IPatientsService _patientsService;
        private readonly ISettingsManager _settingsManager;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public ReceiveLabResultsCommandHandler(
            ITransactionManager transactionManager,
            ILabNameProvider labNameProvider,
            IInputsService inputsService, 
            ILabsWebClient labsWebClient,
            ILabNamesService labNamesService,
            ILabNameAliasesService labNameAliasesService,
            ILabVendorsService labVendorsService,
            ILabNameRangesService labNameRangesService,
            ILabNameRangeProvider labNameRangeProvider,
            IPatientsService patientsService,
            ISettingsManager settingsManager,
            IMediator mediator,
            ILogger<ReceiveLabResultsCommandHandler> logger)
        {
            _transactionManager = transactionManager;
            _labNameProvider = labNameProvider;
            _inputsService = inputsService;
            _labsWebClient = labsWebClient;
            _labNamesService = labNamesService;
            _labNameAliasesService = labNameAliasesService;
            _labVendorsService = labVendorsService;
            _labNameRangesService = labNameRangesService;
            _labNameRangeProvider = labNameRangeProvider;
            _patientsService = patientsService;
            _settingsManager = settingsManager;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(ReceiveLabResultsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Receiving lab results for patient with id: {command.PatientId} has been started.");
            
            var patientId = command.PatientId;
            
            var reportId = command.ReportId;

            var patient = await _patientsService.GetByIdAsync(patientId);

            // Need to verify that the patient has a practice in order to determine the default lab vendor to use (configured in licensing)
            AssertPatientWithPracticeExists(patient, patientId, reportId);

            var labVendor = await _labVendorsService.GetForPatient(patient);

            var labResults = await FetchLabResultsAsync(patientId, reportId);

            if (!labResults.Any())
            {
                _logger.LogInformation($"Receiving lab results for [PatientId]: {command.PatientId}, [ReportId] = {reportId} had no results returned, taking not action and returning.");
                
                return;
            }

            var labReport = await FetchLabReportForReportId(patientId, reportId);

            var (bytes, fileName) = await FetchLabResultsDocumentAsync(patientId, reportId);

            // There's a chance that preior imports brought int wrong data and didn't store based on observation date, make sure we clean that first
            await _mediator.Send(new CleanUpLabInputValueDatesCommand(patientId: patientId, reportId: reportId));

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
                var updateLabInputsModel = await ToModel(patient, labVendor, labResults, aggregator);

                await _inputsService.UpdateLabInputsAsync(updateLabInputsModel, patientId);

                var file = await _mediator.Send(uploadInputsFileCommand, cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                
                await _mediator.Publish(new FileInputsUploadedEvent(patientId, FileInputType.LabResults, file.File.Uri, labReport?.OrderNumber), cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError($"Receiving lab results for patient with id: {command.PatientId} has been failed - {e}");
                
                await transaction.RollbackAsync(cancellationToken);
                
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

        private async Task<LabReportModel> FetchLabReportForReportId(int patientId, int labReportId) {
            return await _labsWebClient.GetPatientLabReportAsync(Convert.ToString(patientId), labReportId);
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
        private async Task<UpdateLabInputsModel> ToModel(Patient patient, LabVendor labVendor, LabResultModel[] results, InputsAggregator aggregator)
        {
            var createModels = new List<CreateLabInputValueModel>();
            var updateModels = new List<UpdateLabInputValueModel>();
            var appendModels = new List<AppendLabInputValueModel>();
            var labInputMutations = new List<LabInput>();


            // Make sure they have a birthday so we can enter in range information further downstream
            AssertBirthdayExists(results.FirstOrDefault(), patient.User.Birthday);

            // Pulling lab date this way because there were instances of results coming in where the request was null
            // We can assume that all results within a common reportId have the same observation date
            var firstResultWithRequest = results.Where(o => o.Request != null).FirstOrDefault();

            var labDate = firstResultWithRequest?.Request.ObservationDateTime?.Date ?? DateTime.UtcNow.Date;
            
            var existingValues = aggregator.GetLabInputValuesSet(labDate);
            
            foreach (var result in results)
            {
                try
                {
                    if(!patient.User.Birthday.HasValue) continue;
                    
                    (LabNameAlias labNameAlias, LabNameRange labNameRange) = await GetLabNameAliasForResult(labVendor, patient.User.Gender, patient.User.Birthday.Value, result);
                    
                    var labNameString = labNameAlias.LabName.WildHealthName;

                    var labInputRange = GetRangeFromResult(result);

                    // Handle creating/updating of LabInputs
                    labInputMutations.Add(ManageLabInputs(aggregator, labNameAlias, labNameRange, labInputRange));

                    if (existingValues is null || !existingValues.Any())
                    {
                        createModels.Add(new CreateLabInputValueModel
                            {
                                Date = labDate,
                                StringValue = result.Value,
                                Name = labNameString,
                                LabNameAlias = labNameAlias,
                                LabNameRange = labNameRange,
                                LabInputRange = labInputRange
                            });
                        
                        continue;
                    }

                    if (existingValues.Any(x => x.Name == labNameString))
                    {
                        var existingValue = existingValues.First(x => x.Name == labNameString);
                        
                        updateModels.Add(new UpdateLabInputValueModel
                        {
                            Date = labDate,
                            Id = existingValue.GetId(),
                            StringValue = result.Value,
                            LabInputRange = labInputRange
                        });
                        
                        continue;
                    }

                    if (appendModels.All(o => o.Name != labNameString))
                    {
                        appendModels.Add(new AppendLabInputValueModel
                        {
                            DataSetId = existingValues[0].DataSetId,
                            StringValue = result.Value,
                            Name = labNameString,
                            LabInputRange = labInputRange
                        });
                    }
                }
                catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
                {
                   // Ignore
                }
            }
            
            return new UpdateLabInputsModel
            {
                ToCreate = createModels.ToArray(),
                ToAppend = appendModels.ToArray(),
                ToUpdate = updateModels.ToArray(),
                ToCorrect = Array.Empty<CorrectLabInputValueModel>(),
                Ranges = Array.Empty<UpdateLabInputRangeModel>(),
                ToDelete = Array.Empty<int>(),
                LabInputMutations = labInputMutations.ToArray()
            };
        }

        private LabInputRange? GetRangeFromResult(LabResultModel result)
        {
            if (string.IsNullOrEmpty(result.Range))
            {
                return new LabInputRange(LabRangeType.None, null, null, null);
            }
            
            if (result.Range.Contains(">="))
            {
                var from = result.Range.Split(">=")[1];
                var parseFlag = decimal.TryParse(from, out decimal fromValue);
                return new LabInputRange(LabRangeType.MoreThanOrEqual, result.Units, parseFlag ? fromValue : null, null);
            }

            if (result.Range.Contains("<="))
            {
                var to = result.Range.Split("<=")[1];
                var parseFlag = decimal.TryParse(to, out decimal toValue);
                return new LabInputRange(LabRangeType.LessThanOrEqual, result.Units, null, parseFlag ? toValue : null);
            }
            
            // Try first for more than
            if (result.Range.Contains('>'))
            {
                var from = result.Range.Split('>')[1];
                var parseFlag = decimal.TryParse(from, out decimal fromValue);
                return new LabInputRange(LabRangeType.MoreThen, result.Units, parseFlag ? fromValue : null, null);
            }

            if (result.Range.Contains('<'))
            {
                var to = result.Range.Split('<')[1];
                var parseFlag = decimal.TryParse(to, out decimal toValue);
                return new LabInputRange(LabRangeType.LessThen, result.Units, null, parseFlag ? toValue : null);
            }

            if (result.Range.Contains('-'))
            {
                var fromTo = result.Range.Split('-');
                var fromParse = decimal.TryParse(fromTo[0], out decimal from);
                var toParse = decimal.TryParse(fromTo[1], out decimal to);
                return new LabInputRange(LabRangeType.FromTo, result.Units, fromParse ? from : null, toParse ? to : null);
            }

            return null;
        }
        
        private LabInput ManageLabInputs(InputsAggregator aggregator, LabNameAlias labNameAlias, LabNameRange labNameRange, LabInputRange? labInputRange)
        {
            var labNameString = labNameAlias.LabName.WildHealthName;
            
            // First want to get by a direct relationship from the alias table
            var labInput = aggregator.LabInputs.FirstOrDefault(o => o.LabNameAliasId == labNameAlias.GetId());

            // If we didn't find anything, look by name, some legacy data does NOT reference an alias and in this case we will want to get by name
            if (labInput is null)
            {
                labInput = aggregator.LabInputs.FirstOrDefault(o => o.Name.Equals(labNameString));
            }
            
            // Create LabInput if it does not exist
            // Otherwise, try to start updating these to reference an existing LabNameAlias and update range information if it does not exist
            if(labInput == null)
            {
                var range = labInputRange != null ? 
                    new LabInputRange(
                        type: labInputRange.Type,
                        dimension: labInputRange.Dimension,
                        from: labInputRange.From,
                        to: labInputRange.To)
                    : 
                        labNameRange != null ?
                    new LabInputRange(
                        type: labNameRange.RangeType,
                        dimension: labNameRange.RangeDimension,
                        from: labNameRange.RangeFrom,
                        to: labNameRange.RangeTo
                    ) : new LabInputRange(
                        type: LabRangeType.None,
                        dimension: null,
                        from: null,
                        to: null
                    );

                    labInput = new LabInput(
                        name: labNameString,
                        labNameAliasId: labNameAlias.Id,
                        group: Enum.Parse<LabGroup>(labNameAlias.LabName.GroupName),
                        range: range
                    );
                    
                    // Need to make sure we add this here, if we don't, then if a result file has 2 new results that require new LabInputs to be created, we will
                    // create that LabInput multiple times which causes problems when generating health reports
                    aggregator.LabInputs.Add(labInput);

                    return labInput;
            }
        
            labInput.SetLabNameAliasId(labNameAlias.Id);
            labInput.SetGroupName(labNameAlias.LabName.GroupName);

            if(!labInput.HasRange)
            {
                labInput.UpdateRange(labNameRange);
            }

            return labInput;
        }

        private async Task<(LabNameAlias, LabNameRange)> GetLabNameAliasForResult(LabVendor labVendor, Gender gender, DateTime birthday, LabResultModel result)
        {
            LabNameRange? labNameRange = null;

            var labNameAlias = await _labNameProvider.LabNameAliasForResultCode(result.ResultCode);

            // If we don't have a formatting for this lab name, grab the one from the results
            // This should result in creating the following:
            // 1/ LabName
            // 2/ LabNameAlias (for the particular vendor)
            // 3/ LabNameRange (to store the range information for this LabName within the context of the vendor and patient gender/age)
            if (labNameAlias == null)
            {
                // First try to get the LabName or see if it exists
                var labNameObject = await _labNamesService.Get(result.ResultCodeLabel);

                // Create the LabName
                if(labNameObject == null)
                {
                    labNameObject = await _labNamesService.Create(new LabName(){
                        WildHealthName = result.ResultCodeLabel,
                        GroupName = LabGroup.Other.ToString()
                    });
                }

                // Create the LabNameAlias
                var labNameAliasObject = await _labNameAliasesService.Create(new LabNameAlias() {
                    LabNameId = labNameObject.GetId(),
                    LabVendorId = labVendor.GetId(),
                    ResultCode = result.ResultCode,
                });

                // We added a new LabName so need to reset the cache
                _labNameProvider.ResetResultCodesMap();

                labNameAlias = labNameAliasObject;
            }

            // Get or create the labNameRange
            labNameRange = await _labNameRangesService.GetOrCreate(labNameAlias.LabName, labVendor, gender, birthday, result.Units, result.Range);
            
            return (labNameAlias!, labNameRange);
        }


        private void AssertBirthdayExists(LabResultModel? labResultModel, DateTime? birthday)
        {
            if(birthday == null || labResultModel == null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(labResultModel.Id), labResultModel?.Id);                
                throw new AppException(HttpStatusCode.BadRequest, "Cannot receive lab results - patient does not have a configured birthday", exceptionParam);
            }
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