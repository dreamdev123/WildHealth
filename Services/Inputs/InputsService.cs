using System;
using System.Linq;
using System.Net;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Extensions.Inputs;
using WildHealth.Application.Events.Inputs;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Models.Inputs;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Enums.Inputs;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Inputs.Services.InputsParser;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Vitals;
using WildHealth.Application.Events.Patients;
using Microsoft.Extensions.Logging;
using WildHealth.ClarityCore.WebClients.Insights;
using WildHealth.ClarityCore.Models.Insights;
using MediatR;
using WildHealth.Domain.Enums;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.Services.Inputs
{
    /// <summary>
    /// <see cref="IInputsService"/>
    /// </summary>
    public class InputsService : IInputsService
    {
        // Only want LabInputs for groups we know about, anything in Other group should be ignored
        private readonly LabGroup[] _acceptableLabInputGroups =
        {
            LabGroup.Hormones,
            LabGroup.Inflammation,
            LabGroup.InsulinResistanceAndMetabolism,
            LabGroup.Lipids,
            LabGroup.Methylation,
            LabGroup.VitaminsAndMicronutrients,
            LabGroup.CBC,
            LabGroup.Metabolic,
            LabGroup.Other
        };

        private readonly IGeneralRepository<FileInput> _fileInputs;
        private readonly IGeneralRepository<InputsAggregator> _inputsRepository;
        private readonly IGeneralRepository<MicrobiomeInput> _microbiomeInputRepository;
        private readonly IGeneralRepository<GeneralInputs> _generalInputsRepository;
        private readonly IGeneralRepository<LabInput> _labInputRepository;
        private readonly IGeneralRepository<DnaInput> _dnaInputRepository;
        private readonly IGeneralRepository<Vital> _vitalRepository;
        private readonly IInputsParser _inputsParser;
        private readonly IInsightsWebClient _insightsWebClient;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly IMediator _mediator;
        private readonly ILogger<InputsService> _logger;

        public InputsService(
            IGeneralRepository<FileInput> fileInputs,
            IGeneralRepository<InputsAggregator> inputsRepository,
            IGeneralRepository<MicrobiomeInput> microbiomeInputRepository,
            IGeneralRepository<GeneralInputs> generalInputsRepository,
            IGeneralRepository<LabInput> labInputRepository,
            IGeneralRepository<DnaInput> dnaInputRepository,
            IGeneralRepository<Vital> vitalRepository,
            IInputsParser inputsParser,
            IInsightsWebClient insightsWebClient,
            IPermissionsGuard permissionsGuard,
            IMediator mediator,
            ILogger<InputsService> logger)
        {
            _fileInputs = fileInputs;
            _inputsRepository = inputsRepository;
            _microbiomeInputRepository = microbiomeInputRepository;
            _generalInputsRepository = generalInputsRepository;
            _labInputRepository = labInputRepository;
            _dnaInputRepository = dnaInputRepository;
            _vitalRepository = vitalRepository;
            _inputsParser = inputsParser;
            _insightsWebClient = insightsWebClient;
            _permissionsGuard = permissionsGuard;
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IInputsService.GetAggregatorAsync(int, FileInputType)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<InputsAggregator> GetAggregatorAsync(int patientId, FileInputType type)
        {
            var query = _inputsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeFiles();

            query = type switch
            {
                FileInputType.DnaReport => query.IncludeDnaInputs(),
                FileInputType.LabResults => query.IncludeLabInputs(),
                FileInputType.MicrobiomeData => query.IncludeMicrobiomeInputs(),
                _ => throw new ArgumentException("Unsupported file input type")
            };

            return (await query.FirstOrDefaultAsync())!;
        }

        /// <summary>
        /// <see cref="IInputsService.GetAggregatorAsync(int, bool)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="includeInsights"></param>
        /// <returns></returns>
        public async Task<InputsAggregator> GetAggregatorAsync(int patientId, bool includeInsights = false)
        {
            var aggregator = await _inputsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeUser()
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (aggregator is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Inputs aggregator for patient does not exist.", exceptionParam);
            }

            var dna = await _dnaInputRepository
                .All()
                .Where(x => x.AggregatorId == aggregator.Id)
                .AsNoTracking()
                .ToListAsync();

            var labsInputs = await _labInputRepository
                .All()
                .Where(x => x.AggregatorId == aggregator.Id)
                .Include(x => x.Values)
                .Include(x => x.Highlight)
                .AsNoTracking()
                .ToListAsync();

            var labInputValues = labsInputs.SelectMany(x => x.Values).ToArray();

            var microbiome = await _microbiomeInputRepository
                .All()
                .Where(x => x.AggregatorId == aggregator.Id)
                .AsNoTracking()
                .ToListAsync();

            var general = await _generalInputsRepository
                .All()
                .Where(x => x.AggregatorId == aggregator.Id)
                .Include(x => x.Aggregator)
                .ThenInclude(x => x.Patient)
                .ThenInclude(x => x.User)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var vitals = await _vitalRepository
                .All()
                .Where(x => x.AggregatorId == aggregator.Id)
                .Include(x => x.Values)
                .AsNoTracking()
                .ToListAsync();

            var vitalValues = vitals.SelectMany(x => x.Values).ToArray();
            
            aggregator.Dna = dna;
            aggregator.LabInputs = labsInputs;
            aggregator.LabInputValues = labInputValues;
            aggregator.Microbiome = microbiome;
            aggregator.General = general;
            aggregator.Vitals = vitals;
            aggregator.VitalValues = vitalValues;

            if (includeInsights)
            {
                var insights = Array.Empty<InsightModel>();

                try
                {
                    insights = await _insightsWebClient.GetLatestInsightsAsync(patientId: patientId.ToString());
                }
                catch
                {
                    // Ignore
                }
                
                aggregator.Insights = insights
                    .Select(x => x.ToInsight())
                    .Where(x => !string.IsNullOrEmpty(x.Name))
                    .ToArray();
            }

            return aggregator;
        }

        /// <summary>
        /// <see cref="IInputsService.FillOutInputsAsync"/>
        /// </summary>
        /// <param name="aggregator"></param>
        /// <param name="input"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public Task FillOutInputsAsync(InputsAggregator aggregator, FileInput input, byte[] content)
        {
            return input.Type switch
            {
                FileInputType.LabResults => FillOutLabInputsAsync(aggregator, input, content),
                FileInputType.MicrobiomeData => FillOutMicrobiomeInputsAsync(aggregator, input, content),
                FileInputType.DnaReport => FillOutDnaInputsAsync(aggregator, input, content),
                _ => throw new ArgumentException("Unsupported input type.")
            };
        }


        #region file inputs

        /// <summary>
        /// <see cref="IInputsService.GetFileInputAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<FileInput> GetFileInputAsync(int id, int patientId)
        {
            var fileInput = await _fileInputs
                .All()
                .ById(id)
                .Include(x => x.File)
                .Include(x => x.Aggregator)
                .FirstOrDefaultAsync();

            if (fileInput is null || fileInput.Aggregator.PatientId != patientId)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "File does not exist.", exceptionParam);
            }

            return fileInput;
        }

        /// <summary>
        /// <see cref="IInputsService.GetFileInputsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<FileInput>> GetFileInputsAsync(int patientId)
        {
            var aggregator = await GetAggregatorWithFiles(patientId);

            _permissionsGuard.AssertPermissions(aggregator);

            return aggregator.Files;
        }

        /// <summary>
        /// <see cref="IInputsService.CreateFileInputAsync"/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<FileInput> CreateFileInputAsync(FileInput input)
        {
            await _fileInputs.AddAsync(input);

            await _fileInputs.SaveAsync();

            return input;
        }

        /// <summary>
        /// <see cref="IInputsService.UpdateFileInputAsync"/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<FileInput> UpdateFileInputAsync(FileInput input)
        {
            _fileInputs.Edit(input);

            await _fileInputs.SaveAsync();

            return input;
        }

        /// <summary>
        /// <see cref="IInputsService.DeleteFileInputAsync"/>
        /// </summary>
        /// <param name="fileInput"></param>
        /// <returns></returns>
        public async Task<FileInput> DeleteFileInputAsync(FileInput fileInput)
        {
            _fileInputs.Delete(fileInput);

            await _fileInputs.SaveAsync();

            return fileInput;
        }

        #endregion

        #region lab inputs

        /// <summary>
        /// <see cref="IInputsService.GetLabInputsWithoutAliasReference"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ICollection<LabInput>> GetLabInputsWithoutAliasReference()
        {
            return await _labInputRepository
                .All()
                .WithoutAliasReference()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IInputsService.GetLabInputsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<ICollection<LabInput>> GetLabInputsAsync(int patientId)
        {
            var aggregator = await GetAggregatorWithLabs(patientId);

            _permissionsGuard.AssertPermissions(aggregator);

            return aggregator.LabInputs.Where(o => _acceptableLabInputGroups.Contains(o.Group)).ToArray();
        }

        /// <summary>
        /// <see cref="IInputsService.GetLabInputsIntegrationAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<ICollection<LabInput>> GetLabInputsIntegrationAsync(int patientId)
        {
            var aggregator = await GetAggregatorWithLabs(patientId);

            return aggregator.LabInputs;
        }

        public async Task<ICollection<LabInput>> DeleteLabValues(int patientId, string datasetId)
        {
            var now = DateTime.UtcNow;
            
            var aggregator = await GetAggregatorWithLabs(patientId);

            var values = aggregator.GetLatestLabInputValuesSet(datasetId);

            foreach (var value in values)
            {
                aggregator.DeleteLabInputValue(value.GetId());
            }
            
            _inputsRepository.Edit(aggregator);

            await _inputsRepository.SaveAsync();

            await _mediator.Publish(new LabInputsUpdatedEvent(
                patientId: patientId,
                mostRecentLabDate: aggregator.GetLatestLabInputValueDate(),
                updatedAt: now,
                inputs: aggregator.GetLabInputValuesAfterUpdate(now),
                createdInputs: Array.Empty<LabInput>()
            ));

            return aggregator.LabInputs;
        }

        /// <summary>
        /// <see cref="IInputsService.UpdateLabInputAsync"/>
        /// Update the provided lab input
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<LabInput> UpdateLabInputAsync(LabInput model)
        {
            _labInputRepository.Edit(model);

            await _labInputRepository.SaveAsync();

            return model;
        }

        /// <summary>
        /// <see cref="IInputsService.UpdateLabInputsAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<ICollection<LabInput>> UpdateLabInputsAsync(UpdateLabInputsModel model, int patientId)
        {
            var now = DateTime.UtcNow;

            var aggregator = await GetAggregatorWithLabs(patientId);

            var createdInputs = new List<LabInput>();

            // Create other lab inputs
            foreach (var newInputModel in model.NewLabInputs)
            {
                var newLabInput = new LabInput(
                    name: newInputModel.Name,
                    labNameAliasId: null,
                    group: LabGroup.Other,
                    range: new LabInputRange(
                        type: newInputModel.Range.Type,
                        dimension: newInputModel.Range.Dimension,
                        from: newInputModel.Range.From,
                        to: newInputModel.Range.To
                    )
                );

                if (newInputModel.isHighlighted)
                {
                    newLabInput.SetHighlighted(newInputModel.isHighlighted);
                }
                
                aggregator.LabInputs.Add(newLabInput);
                createdInputs.Add(newLabInput);
            }
            
            // Update other lab inputs
            foreach (var updatedLabInputModel in model.UpdatedLabInputs)
            {
                var existingLabInput = aggregator.LabInputs.FirstOrDefault(o => o.Id.Equals(updatedLabInputModel.Id));
                if (existingLabInput is null)
                {
                    throw new DomainException("Lab input does not exist");
                }
                
                if (existingLabInput.Group != LabGroup.Other)
                {
                    throw new DomainException("You can change the name only for 'Other' lab inputs");
                }

                existingLabInput.SetName(updatedLabInputModel.Name);
            }
            
            // Update LabInputs first
            foreach (var labInput in model.LabInputMutations)
            {
                if (labInput.Id.HasValue)
                {
                    var existingLabInput = aggregator.LabInputs.FirstOrDefault(o => o.Id.Equals(labInput.Id));

                    if (existingLabInput != null)
                    {
                        existingLabInput.SetLabNameAliasId(labInput.LabNameAliasId);
                        existingLabInput.UpdateRange(labInput.Range);
                        existingLabInput.SetGroupName(Enum.GetName(labInput.Group));
                    }
                }
                else
                {
                    aggregator.LabInputs.Add(labInput);
                    createdInputs.Add(labInput);
                }
            }

            foreach (var range in model.Ranges)
            {
                if (range.Id is null)
                {
                    throw new DomainException("Lab input  id must be specified");
                }
                
                aggregator.UpdateLabRange(
                    id: range.Id.Value,
                    from: range.From.HasValue ? Convert.ToDecimal(range.From.Value) : new decimal?(),
                    to: range.To.HasValue ? Convert.ToDecimal(range.To.Value) : new decimal?()
                );
            }

            var newValueGroups = model.ToCreate.GroupBy(x => x.Date).ToArray();

            foreach (var group in newValueGroups)
            {
                // Cannot assume that the names of the items in here is unique, need to filter out any items that are duplicates and only keep one
                var filteredGroup = group.DistinctBy(o => o.Name).ToArray();

                if (filteredGroup.Count() != group.Count())
                {
                    var duplicateNames = String.Join(", ", 
                        group.GroupBy(o => o.Name).Where(o => o.Count() > 1).Select(o => o.Key));
                    
                    _logger.LogWarning($"Determined that there were duplicated lab results for [PatientId] = {patientId}, these labs are: {duplicateNames}");
                }

                var date = group.Key;

                AssertPossibilityToAddNewLabResult(aggregator, date);

                var dataSetId = aggregator.GenerateDataSetId();
                
                // Go through each lab input entry and pass in value + range
                foreach (var kvp in group)
                {
                    var value = !string.IsNullOrEmpty(kvp.StringValue)
                        ? kvp.StringValue
                        : kvp.Value.ToString(CultureInfo.InvariantCulture);
                    
                    aggregator.AddLabInputValues(
                        name: kvp.Name,
                        value: value,
                        dataSetId: dataSetId,
                        range: kvp.LabInputRange,
                        provider: FileInputDataProvider.Manual,
                        date: date
                    );
                }
            }

            foreach (var toUpdate in model.ToUpdate)
            {
                var value = !string.IsNullOrEmpty(toUpdate.StringValue)
                    ? toUpdate.StringValue
                    : toUpdate.Value.ToString(CultureInfo.InvariantCulture);

                aggregator.UpdateLabValueManually(
                    id: toUpdate.Id,
                    value: value,
                    range: toUpdate.LabInputRange,
                    dateTime: toUpdate.Date ?? now
                );
            }

            foreach (var toCorrect in model.ToCorrect)
            {
                var value = !string.IsNullOrEmpty(toCorrect.StringValue)
                    ? toCorrect.StringValue
                    : toCorrect.Value.ToString(CultureInfo.InvariantCulture);

                aggregator.CorrectLabValueManually(
                    id: toCorrect.Id,
                    value: value,
                    range: toCorrect.LabInputRange,
                    note: toCorrect.Note
                );
            }

            foreach (var toAppend in model.ToAppend)
            {
                var value = !string.IsNullOrEmpty(toAppend.StringValue)
                    ? toAppend.StringValue
                    : toAppend.Value.ToString(CultureInfo.InvariantCulture);

                aggregator.AppendLabValueManually(
                    name: toAppend.Name,
                    value: value,
                    range: toAppend.LabInputRange,
                    dataSetId: toAppend.DataSetId
                );
            }

            foreach (var valueId in model.ToDelete)
            {
                aggregator.DeleteLabInputValue(valueId);
            }

            _inputsRepository.Edit(aggregator);

            await _inputsRepository.SaveAsync();

            await _mediator.Publish(new LabInputsUpdatedEvent(
                patientId: patientId,
                mostRecentLabDate: aggregator.GetLatestLabInputValueDate(),
                updatedAt: now,
                inputs: aggregator.GetLabInputValuesAfterUpdate(now),
                createdInputs: createdInputs
            ));

            return aggregator.LabInputs;
        }

        #endregion

        #region general inputs

        /// <summary>
        /// <see cref="IInputsService.GetGeneralInputsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<GeneralInputs> GetGeneralInputsAsync(int patientId)
        {
            var aggregator = await GetAggregatorWithGeneralInputs(patientId);

            return aggregator.General;
        }

        /// <summary>
        /// <see cref="IInputsService.UpdateGeneralInputsAsync(GeneralInputsModel, int)"/>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<GeneralInputs> UpdateGeneralInputsAsync(GeneralInputsModel model, int patientId)
        {
            var aggregator = await GetAggregatorWithGeneralInputs(patientId);

            var generalInputs = aggregator.General;

            generalInputs.Update(model);

            var result = await UpdateGeneralInputsAsync(generalInputs, patientId);

            return result;
        }

        /// <summary>
        /// <see cref="IInputsService.UpdateGeneralInputsAsync(GeneralInputs, int)"/>
        /// </summary>
        /// <param name="generalInputs"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<GeneralInputs> UpdateGeneralInputsAsync(GeneralInputs generalInputs, int patientId)
        {
            _inputsRepository.EditRelated(generalInputs);

            await _inputsRepository.SaveAsync();

            await _mediator.Publish(new PatientUpdatedEvent(
                PatientId: patientId,
                NewlyAssignedEmployeeIds: Enumerable.Empty<int>()
            ));

            return generalInputs;
        }

        public async Task UpdateHideApoe(YesNo hideApoe, int patientId)
        {
            var aggregator = await GetAggregatorWithGeneralInputs(patientId);
            var generalInputs = aggregator.General;
            generalInputs.HideApoe = hideApoe;
            await UpdateGeneralInputsAsync(generalInputs, patientId);
        }

        public async Task<YesNo> GetHideApoe(int patientId)
        {
            var aggregator = await GetAggregatorWithGeneralInputs(patientId);
            return aggregator.General.HideApoe;
        }

        #endregion

        #region microbiome inputs

        /// <summary>
        /// <see cref="IInputsService.GetMicrobiomeInputsAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<ICollection<MicrobiomeInput>> GetMicrobiomeInputsAsync(int patientId)
        {
            var aggregator = await GetAggregatorWithMicrobiome(patientId);

            _permissionsGuard.AssertPermissions(aggregator);

            return aggregator.GetMicrobiome();
        }

        /// <summary>
        /// <see cref="IInputsService.UpdateMicrobiomeInputsAsync(ICollection{MicrobiomeInputModel}, int)"/>
        /// </summary>
        /// <param name="models"></param>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<ICollection<MicrobiomeInput>> UpdateMicrobiomeInputsAsync(
            ICollection<MicrobiomeInputModel> models, int patientId)
        {
            var aggregator = await GetAggregatorWithMicrobiome(patientId);

            foreach (var model in models)
            {
                if (model.IsChanged && model.IsInitialized)
                {
                    aggregator.SetMicrobiomeManually(model.Name, model.Value);
                }
                else if (model.IsChanged && !model.IsInitialized)
                {
                    aggregator.ResetMicrobiome(model.Name);
                }
            }

            _inputsRepository.Edit(aggregator);

            await _inputsRepository.SaveAsync();
            
            await _mediator.Publish(new MicrobiomeInputsUpdatedEvent(patientId, aggregator.GetMicrobiome()));

            return aggregator.GetMicrobiome();
        }

        /// <summary>
        /// <see cref="IInputsService.UpdateMicrobiomeInputsAsync(ICollection{MicrobiomeInput})"/>
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public async Task<ICollection<MicrobiomeInput>> UpdateMicrobiomeInputsAsync(ICollection<MicrobiomeInput> inputs)
        {
            foreach (var input in inputs)
            {
                _microbiomeInputRepository.Edit(input);
            }

            await _microbiomeInputRepository.SaveAsync();

            return inputs;
        }

        #endregion

        #region private

        private async Task FillOutLabInputsAsync(InputsAggregator aggregator, FileInput input, byte[] content)
        {
            var now = DateTime.UtcNow;
            
            var createModels = new List<CreateLabInputValueModel>();
            var updateModels = new List<UpdateLabInputValueModel>();
            var appendModels = new List<AppendLabInputValueModel>();
            var labInputMutations = new List<LabInput>();
            
            try
            {
                var (datum, date) = _inputsParser.ParseLabs(input.DataProvider, content);

                input.ObservationDate = date;

                await UpdateFileInputAsync(input);

                if (!datum.Any())
                {
                    throw new AppException(HttpStatusCode.BadRequest, "Invalid format, please manually enter the lab values");
                }

                var existingValues = aggregator.GetLabInputValuesSet(date);

                var dataSetId = aggregator.GenerateDataSetId();
                
                foreach (var data in datum)
                {
                    var name = data.Key;
                    var value = data.Value;
                    
                    // Get lab name alias and lab name for this
                    var labInput = aggregator.LabInputs.FirstOrDefault(o => o.Name.Equals(name));

                    if (labInput is null)
                    {
                        labInput = new LabInput(
                            name: name,
                            labNameAliasId: null,
                            group: LabGroup.Other,
                            range: new LabInputRange(
                                type: LabRangeType.None,
                                dimension: null,
                                from: null,
                                to: null
                            )
                        );
                    }
                    
                    if (existingValues is null || !existingValues.Any())
                    {
                        createModels.Add(new CreateLabInputValueModel
                        {
                            Date = date,
                            StringValue = value.Value,
                            Name = name,
                            LabInputRange = value.LabInputRange
                        });
                        
                        continue;
                    }
                    
                    if (existingValues.Any(x => x.Name == name))
                    {
                        var existingValue = existingValues.First(x => x.Name == name);
                        
                        updateModels.Add(new UpdateLabInputValueModel
                        {
                            Date = date,
                            Id = existingValue.GetId(),
                            StringValue = value.Value,
                            LabInputRange = value.LabInputRange
                        });
                        
                        continue;
                    }

                    appendModels.Add(new AppendLabInputValueModel
                    {
                        DataSetId = existingValues[0].DataSetId,
                        StringValue = value.Value,
                        Name = name,
                        LabInputRange = value.LabInputRange
                    });
                }
                
                var updateModel = new UpdateLabInputsModel
                {
                    ToCreate = createModels.ToArray(),
                    ToAppend = appendModels.ToArray(),
                    ToUpdate = updateModels.ToArray(),
                    ToCorrect = Array.Empty<CorrectLabInputValueModel>(),
                    Ranges = Array.Empty<UpdateLabInputRangeModel>(),
                    ToDelete = Array.Empty<int>(),
                    LabInputMutations = labInputMutations.ToArray()
                };
                
                await UpdateLabInputsAsync(updateModel, aggregator.PatientId);
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Fill out lab inputs has failed with [Error]: {e}");
                throw new AppException(HttpStatusCode.BadRequest, "Unable to parse this file.");
            }
        }

        private async Task FillOutMicrobiomeInputsAsync(InputsAggregator aggregator, FileInput input, byte[] content)
        {
            try
            {
                var (data, date) = _inputsParser.ParseMicrobiome(input, content);
                foreach (var (name, value) in data)
                {
                    aggregator.SetMicrobiome(name, value, input.DataProvider, date);
                }
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Fill out microbiome inputs has failed with [Error]: {e}");
                throw new AppException(HttpStatusCode.BadRequest, "Unable to parse this file.");
            }

            _inputsRepository.Edit(aggregator);

            await _inputsRepository.SaveAsync();
            
            await _mediator.Publish(new MicrobiomeInputsUpdatedEvent(aggregator.PatientId, aggregator.GetMicrobiome()));
        }

        private async Task FillOutDnaInputsAsync(InputsAggregator aggregator, FileInput input, byte[] content)
        {
            try
            {
                aggregator.Dna = _inputsParser.ParseDna(input, content).ToList();
            }
            catch (AppException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Fill out DNA inputs has failed with [Error]: {e}");
                throw new AppException(HttpStatusCode.BadRequest, "Unable to parse this file.");
            }

            _inputsRepository.Edit(aggregator);

            await _inputsRepository.SaveAsync();
        }

        private async Task<InputsAggregator> GetAggregatorWithFiles(int patientId)
        {
            var aggregator = await _inputsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeFiles()
                .IncludeLabInputs()
                .IncludePatient()
                .FirstOrDefaultAsync();

            return aggregator!;
        }

        private async Task<InputsAggregator> GetAggregatorWithLabs(int patientId)
        {
            var aggregator = await _inputsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeLabInputsWithNames()
                .IncludePatient()
                .FirstAsync();


            return aggregator;
        }

        private async Task<InputsAggregator> GetAggregatorWithMicrobiome(int patientId)
        {
            var aggregator = await _inputsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeMicrobiomeInputs()
                .IncludePatient()
                .FirstAsync();

            return aggregator;
        }

        private async Task<InputsAggregator> GetAggregatorWithGeneralInputs(int patientId)
        {
            var aggregator = await _inputsRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeGeneralInputs()
                .IncludePatient()
                .FirstOrDefaultAsync();

            return aggregator!;
        }

        private void AssertPossibilityToAddNewLabResult(InputsAggregator aggregator, DateTime dateTime)
        {
            var isDateExist = aggregator.LabInputValues.Any(c => c.Date == dateTime);

            if (isDateExist)
            {
                throw new AppException(HttpStatusCode.BadRequest, $"Cannot create lab results with already existing date {dateTime.Date}");
            }
        }

        #endregion
    }
}