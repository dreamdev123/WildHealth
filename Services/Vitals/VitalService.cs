using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WildHealth.Common.Models._Base;
using WildHealth.Common.Models.Vitals;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Inputs;
using WildHealth.Domain.Entities.Vitals;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Application.Events.Patients;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Enums.Vitals;
using MediatR;
using WildHealth.Application.Extensions.Inputs;
using WildHealth.ClarityCore.Models.Insights;
using WildHealth.ClarityCore.WebClients.Insights;
using WildHealth.Common.Extensions;

namespace WildHealth.Application.Services.Vitals
{
    /// <inheritdoc/>
    public class VitalService : IVitalService
    {
        private readonly IGeneralRepository<InputsAggregator> _inputAggregatorRepository;
        private readonly IGeneralRepository<VitalValue> _vitalsValuesRepository;
        private readonly IInsightsWebClient _insightsWebClient;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IMediator _mediator;

        public VitalService(
            IGeneralRepository<InputsAggregator> inputsAggregatorRepository, 
            IGeneralRepository<VitalValue> vitalsValuesRepository,
            IInsightsWebClient insightsWebClient,
            IDateTimeProvider dateTimeProvider,
            IMediator mediator)
        {
            _inputAggregatorRepository = inputsAggregatorRepository;
            _vitalsValuesRepository = vitalsValuesRepository;
            _insightsWebClient = insightsWebClient;
            _dateTimeProvider = dateTimeProvider;
            _mediator = mediator;
        }

        /// <inheritdoc/>
        public async Task<ICollection<Vital>> CreateAsync(int patientId, ICollection<CreateVitalModel> inputVitals)
        {
            var inputsAggregator = await GetInputsAggregatorWithInsights(patientId);

            var vitals = MapToDomainEntities(inputVitals, inputsAggregator.GetId());

            inputsAggregator.AddVitalValues(vitals);

            await CommitInputAggregatorChanges(inputsAggregator);

            return GetCreatedVitalsWithValues(inputsAggregator.Vitals, inputVitals);
        }

        /// <inheritdoc/>
        public async Task<ICollection<VitalModel>> GetByDateRangeAsync(int patientId, VitalsDateRangeType dateRangeType)
        {
            var inputsAggregator = await GetInputsAggregatorWithInsights(patientId);

            var allVitalValues = inputsAggregator.VitalValues.ToArray();

            var allInsightValues = inputsAggregator.Insights.ToArray();
            
            var vitals = MapToVitalModels(inputsAggregator.Vitals.ToArray(), allVitalValues, allInsightValues);

            var aggregateVitalValues = new Dictionary<VitalModel, List<IGrouping<int,VitalValueModel>>>();

            var currentDate = _dateTimeProvider.UtcNow();
            
            switch (dateRangeType)
            {
                case VitalsDateRangeType.SevenDays:
                    foreach (var vital in vitals)
                    {
                        aggregateVitalValues.Add(
                            vital,
                            vital
                                .Values
                                .Where(x => x.Date <= currentDate && x.Date >= currentDate.AddDays(-7))
                                .GroupBy(x=> x.Date.Day)
                                .ToList()
                            );
                    }
                    break;
                case VitalsDateRangeType.ThirtyDays:
                    foreach (var vital in vitals)
                    {
                        aggregateVitalValues.Add(
                            vital,
                            vital
                                .Values
                                .Where(x => x.Date <= currentDate && x.Date >= currentDate.AddDays(-30))
                                .GroupBy(x=> x.Date.Day)
                                .ToList()
                        );
                    }
                    break;
                case VitalsDateRangeType.SixMonth:
                    foreach (var vital in vitals)
                    {
                        aggregateVitalValues.Add(
                            vital,
                            vital
                                .Values
                                .Where(x => x.Date <= currentDate && x.Date >= currentDate.AddMonths(-6))
                                .GroupBy(x=> WeekProjector(x.Date))
                                .ToList()
                        );
                    }
                    break;
                case VitalsDateRangeType.Year:
                    foreach (var vital in vitals)
                    {
                        aggregateVitalValues.Add(
                            vital,
                            vital
                                .Values
                                .Where(x => x.Date <= currentDate && x.Date >= currentDate.AddYears(-1))
                                .GroupBy(x=> x.Date.Month)
                                .ToList()
                        );
                    }
                    break;
                default:
                    throw new ArgumentException("Wrong vitals range type.");
            }

            var resultModel = new List<VitalModel>();
            
            foreach (var vital in aggregateVitalValues)
            {
               resultModel.Add(new VitalModel
                   {
                       Dimension = vital.Key.Dimension,
                       Id = vital.Key.Id,
                       Name = vital.Key.Name,
                       DisplayName = vital.Key.DisplayName,
                       Values = vital.Value.Select(x => new VitalValueModel
                       {
                           Date =  TrimDate(x.FirstOrDefault()?.Date ?? _dateTimeProvider.UtcNow()),
                           Id = x.FirstOrDefault()?.Id ?? 0,
                           Value = Math.Round(x.Average(s => s.Value), 2),
                           SourceType = x.FirstOrDefault()?.SourceType ?? VitalValueSourceType.None
                       }).ToArray()
                   });
            }

            SameDatesForRanges(dateRangeType,resultModel);
            
            return resultModel;
        }

        /// <inheritdoc/>
        public async Task<ICollection<Vital>> CreateVitalsValueDataSetAsync(int patientId, DateTime vitalValueDateTime, VitalValueSourceType sourceType)
        {
            var inputsAggregator = await GetInputsAggregatorWithInsights(patientId);

            inputsAggregator.AddVitalDataSet(vitalValueDateTime, sourceType);

            await CommitInputAggregatorChanges(inputsAggregator);

            return inputsAggregator.Vitals;
        }

        /// <inheritdoc/>
        public async Task<ICollection<VitalValue>> DeleteVitalValuesAsync(int patientId, int[] vitalsValuesIds)
        {
            var inputsAggregator = await GetInputsAggregatorWithInsights(patientId);

            var vitalValues = inputsAggregator.VitalValues
                .Where(x => vitalsValuesIds.Contains(x.GetId()))
                .ToArray();

            foreach (var value in vitalValues)
            {
                _vitalsValuesRepository.Delete(value);
            }

            await _vitalsValuesRepository.SaveAsync();

            return vitalValues;
        }

        /// <inheritdoc/>
        public async Task<PaginationModel<VitalModel>> GetAsync(int patientId,
            int page,
            int pageSize,
            DateTime? startRange,
            DateTime? endRange)
        {
            var inputsAggregator = await GetInputsAggregatorWithInsights(
                patientId: patientId, 
                start: startRange, 
                end: endRange
            );

            var allVitalValues = inputsAggregator.VitalValues.ToArray();
            
            var allInsights = inputsAggregator.Insights.ToArray();

            var models = MergeVitalValuesWithInsights(allVitalValues, allInsights);

            var (filteredModels, totalCount) = FilterVitalModels(models, page, pageSize, startRange, endRange);

            var vitals = MapToVitalModels(inputsAggregator.Vitals.ToArray(), filteredModels);

            return new PaginationModel<VitalModel>(vitals, totalCount);
        }

        /// <inheritdoc/>
        public async Task<IDictionary<string, VitalDetailsModel>> GetLatestAsync(int patientId)
        {
            var inputsAggregator = await GetInputsAggregatorWithInsights(patientId, onlyLatestInsights: true);

            var vitalValueGroup = inputsAggregator.GetLatestVitalValue();

            var vitalResult = MapToVitalDetailsModel(inputsAggregator, vitalValueGroup);

            return vitalResult;
        }

        /// <inheritdoc/>
        public Task AssertDateAsync(int patientId, DateTime date)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<ICollection<VitalValue>> UpdateVitalValueAsync(int patientId, ICollection<UpdateVitalValueModel> inputVitalValues)
        {
            var inputsAggregator = await GetInputsAggregatorWithInsights(patientId);

            foreach (var inputVital in inputVitalValues)
            {
                var vitalValue = inputsAggregator.GetVitalValue(inputVital.ValueId);

                if (vitalValue is null)
                {
                    inputsAggregator.AddVitalValues(inputVital.Value, inputVital.DateTime, inputVital.VitalId, inputVital.SourceType);
                }
                else
                {
                    vitalValue.ChangeValue(inputVital.Value);
                }
            }

            await CommitInputAggregatorChanges(inputsAggregator);

            var vitalValues = GetUpdatedValues(inputsAggregator.VitalValues, inputVitalValues);

            await _mediator.Publish(new PatientUpdatedEvent(
                PatientId: patientId,
                NewlyAssignedEmployeeIds: Enumerable.Empty<int>()
            ));

            return vitalValues;
        }
        
        /// <inheritdoc/>
        public async Task ParseAsync(int patientId, ICollection<CreateVitalModel> inputVitals)
        {
            var inputsAggregator = await GetInputsAggregatorWithInsights(patientId);

            var vitals = MapToDomainEntities(inputVitals, inputsAggregator.GetId());

            inputsAggregator.AddVitalValues(vitals);

            await CommitInputAggregatorChanges(inputsAggregator);
        }

        private IEnumerable<Vital> MapToDomainEntities(ICollection<CreateVitalModel> vitals, int aggregatorId)
        {
            return vitals.Select(x =>
            {
                var vital = new Vital(
                    name: VitalNames.GetValidName(x.Name), 
                    displayName: VitalNames.GetDisplayName(x.Name), 
                    dimension: VitalDimensions.GetDimension(x.Name), 
                    aggregatorId: aggregatorId
                );
                
                vital.AddValue(x.DateTime, x.Value, x.SourceType);
                
                return vital;
            });
        }

        private async Task<InputsAggregator> GetInputsAggregatorWithInsights(int patientId, DateTime? start = null, DateTime? end = null, bool onlyLatestInsights = false)
        {
            var aggregator = await GetInputsAggregator(
                patientId: patientId
            );

            var insights = Array.Empty<InsightModel>();
            
            try
            {
                insights = onlyLatestInsights
                    ? await _insightsWebClient.GetLatestInsightsAsync(patientId.ToString())
                    : await _insightsWebClient.GetInsightsAsync(
                        patientId: patientId.ToString(),
                        start: start,
                        end: end
                    );
            }
            catch
            {
                // Ignore
            }
            
            aggregator.Insights = insights
                .Where(o => !string.IsNullOrEmpty(o.Value))
                .Select(x => x.ToInsight())
                .Where(x => !string.IsNullOrEmpty(x.Name))
                .ToArray();

            return aggregator;
        }

        private async Task<InputsAggregator> GetInputsAggregator(int patientId)
        {
            var aggregator = await _inputAggregatorRepository
                .All()
                .RelatedToPatient(patientId)
                .IncludeVitalsAndValues()
                .FirstOrDefaultAsync();

            if (aggregator is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Aggregator does not exist");
            }
            
            var vitals = aggregator.Vitals;

            aggregator.VitalValues = vitals.SelectMany(x => x.Values).ToList();

            return aggregator;
        }

        private async Task CommitInputAggregatorChanges(InputsAggregator inputsAggregator)
        {
            _inputAggregatorRepository.Edit(inputsAggregator);
            
            await _inputAggregatorRepository.SaveAsync();
        }

        private VitalModel[] MapToVitalModels(Vital[] storedVitals, VitalValue[] vitalValues, Insight[] insights)
        {
            var vitals = new List<VitalModel>();

            foreach (var vital in storedVitals)
            {
                var vitalValuesModel = vitalValues
                    .Where(x => x.VitalId == vital.Id)
                    .Select(x => new VitalValueModel
                    {
                        Id = x.GetId(),
                        Name = x.Vital.Name,
                        Date = x.Date,
                        Value = x.Value,
                        SourceType = x.SourceType
                    })
                    .ToArray();

                var insightsModels = insights
                    .Where(x => x.Name == vital.Name)
                    .Select(x => new VitalValueModel
                    {
                        Id = x.Id,
                        Name = x.Name,
                        Date = x.Date,
                        Value = x.Value,
                        SourceType = VitalValueSourceType.MobileApplication
                    });

                vitals.Add(new VitalModel
                {
                    Id = vital.GetId(),
                    Name = vital.Name,
                    Dimension = vital.Dimension,
                    DisplayName = vital.DisplayName,
                    Values = vitalValuesModel.Concat(insightsModels).ToArray()
                });
            }
            
            return vitals.ToArray();
        }

        private VitalValueModel[] MergeVitalValuesWithInsights(VitalValue[] vitalValues, Insight[] insights)
        {
            var vitalValuesModel = vitalValues
                .Select(x => new VitalValueModel
                {
                    Id = x.GetId(),
                    Name = x.Vital.Name,
                    Date = x.Date,
                    Value = x.Value,
                    SourceType = x.SourceType
                })
                .ToArray();

            var insightsModels = insights
                .Select(x => new VitalValueModel
                {
                    Id = x.Id,
                    Name = x.Name,
                    Date = x.Date,
                    Value = x.Value,
                    SourceType = VitalValueSourceType.MobileApplication
                });

            return vitalValuesModel.Concat(insightsModels).ToArray();
        }
        
        private VitalModel[] MapToVitalModels(Vital[] storedVitals, VitalValueModel[] vitalValues)
        {
            var vitals = new List<VitalModel>();

            foreach (var vital in storedVitals)
            {
                var vitalValuesModel = vitalValues
                    .Where(x => x.Name == vital.Name)
                    .ToArray();

                vitals.Add(new VitalModel
                {
                    Id = vital.GetId(),
                    Name = vital.Name,
                    Dimension = vital.Dimension,
                    DisplayName = vital.DisplayName,
                    Values = vitalValuesModel
                });
            }
            
            return vitals.ToArray();
        }

        private (VitalValueModel[], int) FilterVitalModels(
            VitalValueModel[] models,
            int page,
            int pageSize,
            DateTime? startRange,
            DateTime? endRange)
        {
            var matrix = new List<VitalValueModel[]>();

            var totalCount = 0;
            
            var groupedByDates = models
                .Where(x => (!startRange.HasValue || x.Date >= startRange.Value)
                            && (!endRange.HasValue || x.Date <= endRange.Value))
                .GroupBy(x => x.Date)
                .OrderByDescending(x => x.Key);

            foreach (var dateGroup in groupedByDates)
            {
                var groupedByVital = dateGroup.GroupBy(x => x.Name).ToArray();

                var maxCount = groupedByVital.Max(x => x.Count());

                totalCount += maxCount;
                
                for (var n = 0; n <= maxCount - 1; n++)
                {
                    var column = new List<VitalValueModel>();

                    foreach (var v in groupedByVital)
                    {
                        if (v.Count() - 1 < n)
                        {
                            continue;
                        }

                        var arr = v.ToArray();
                        Array.Sort(arr, (x, y) => x.Id >= y.Id ? 1 : 0);
                        column.Add(arr[n]);
                    }
                    
                    matrix.Add(column.ToArray());
                }
            }

            var result = matrix
                .Skip(page * pageSize)
                .Take(pageSize)
                .SelectMany(x => x)
                .ToArray();

            return (result, totalCount);
        }

        private IDictionary<string, VitalDetailsModel> MapToVitalDetailsModel(InputsAggregator inputsAggregator,
            ICollection<VitalValue> vitalValues)
        {
            var vitalResult = new Dictionary<string, VitalDetailsModel>();

            if (vitalValues is null)
            {
                return vitalResult;
            }

            foreach (var vital in inputsAggregator.Vitals)
            {
                var vitalValue = vitalValues.FirstOrDefault(x => x.VitalId == vital.Id);

                var insightValue = inputsAggregator.Insights.FirstOrDefault(o =>
                    o.Name == vital.Name && (vitalValue is null || o.Date >= vitalValue.Date));
                
                vitalResult.Add(vital.Name,   
                    insightValue?.ToDetailsModel() ?? new VitalDetailsModel
                {
                    Id = vital.GetId(),
                    Dimension = vitalValue is null ? default : vital.Dimension,
                    DisplayName = vital.DisplayName,
                    Value = vitalValue?.Value,
                    ValueId = vitalValue?.Id,
                    Date = vitalValue?.Date,
                    SourceType = vitalValue?.SourceType ?? VitalValueSourceType.None
                });
            }

            return vitalResult;
        }

        private ICollection<VitalValue> GetUpdatedValues(ICollection<VitalValue> vitalValues,
            ICollection<UpdateVitalValueModel> inputVitalValues)
        {
            return vitalValues
                .Where(x => inputVitalValues.Any(inputVitalValue => inputVitalValue.ValueId == x.Id))
                .ToArray();
        }

        private ICollection<Vital> GetCreatedVitalsWithValues(ICollection<Vital> vitals, ICollection<CreateVitalModel> inputVitals)
        {
            vitals
                .Where(x => inputVitals.Any(inputVital => inputVital.Name.Equals(x.Name)))
                .ToList()
                .ForEach(vital => RemoveNotRecentVitalValues(vital.Values));

            return vitals;
        }

        private void RemoveNotRecentVitalValues(ICollection<VitalValue> vitalValues)
        {
            vitalValues
                .Where(vitalValue => vitalValue.CreatedAt != vitalValues.Max(v => v.CreatedAt))
                .ToList()
                .ForEach(vitalValue => vitalValues.Remove(vitalValue));
        }

        private DateTime TrimDate(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day);
        }

        private void SameDatesForRanges(VitalsDateRangeType type, ICollection<VitalModel> collection)
        {
            if (VitalsDateRangeType.SevenDays == type || VitalsDateRangeType.None == type)
            {
                return;
            }
            
            var allValues = type switch
            {
                VitalsDateRangeType.Year => collection.SelectMany(x => x.Values).GroupBy(x => x.Date.Month).ToArray(),
                VitalsDateRangeType.SixMonth => collection.SelectMany(x => x.Values).GroupBy(x => WeekProjector(x.Date)).ToArray(),
                VitalsDateRangeType.ThirtyDays => collection.SelectMany(x => x.Values).GroupBy(x => x.Date.Day).ToArray(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            foreach (var valueGroup in allValues)
            {
                var firstDate = valueGroup.FirstOrDefault()?.Date;

                if (!firstDate.HasValue)
                {
                    continue;
                }
                    
                foreach (var vital in valueGroup)
                {
                    vital.Date = firstDate.Value;
                }
            }
        }
        
        private int WeekProjector(DateTime d) => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(d, CalendarWeekRule.FirstFullWeek, DayOfWeek.Sunday);
    }
}
