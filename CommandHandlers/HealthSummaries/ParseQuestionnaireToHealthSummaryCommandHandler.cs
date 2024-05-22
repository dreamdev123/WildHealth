using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Application.Services.Questionnaires;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Enums.Questionnaires;
using MediatR;
using Newtonsoft.Json;
using WildHealth.Application.Utils.QuestionnaireToHealthSummaryParser;

namespace WildHealth.Application.CommandHandlers.HealthSummaries;

public class ParseQuestionnaireToHealthSummaryCommandHandler : IRequestHandler<ParseQuestionnaireToHealthSummaryCommand>
{
    private readonly ILogger<ParseQuestionnaireToHealthSummaryCommandHandler> _logger;
    private readonly IQuestionnairesService _questionnairesService;
    private readonly IHealthSummaryService _healthSummaryService;
    private readonly IQuestionnaireToHealthSummaryValueFormatter _valueFormatter;

    public ParseQuestionnaireToHealthSummaryCommandHandler(
        ILogger<ParseQuestionnaireToHealthSummaryCommandHandler> logger,
        IQuestionnairesService questionnairesService,
        IHealthSummaryService healthSummaryService, 
        IQuestionnaireToHealthSummaryValueFormatter valueFormatter)
    {
        _questionnairesService = questionnairesService;
        _healthSummaryService = healthSummaryService;
        _valueFormatter = valueFormatter;
        _logger = logger;
    }
    
    public async Task Handle(ParseQuestionnaireToHealthSummaryCommand request, CancellationToken cancellationToken)
    {
        var patient = request.Patient;
        var result = request.Result;
        var answers = result.Answers;
        
        var questionnaire = await _questionnairesService.GetByIdAsync(request.Result.QuestionnaireId);
        var questions = questionnaire.Questions.ToArray();

        var currentValues = await _healthSummaryService.GetByPatientAsync(patient.GetId());
        
        try
        {
            var healthSummaryMap = await _healthSummaryService.GetMapAsync();

            var mapItems = healthSummaryMap
                .Where(x=> !x.UseJsonData)
                .SelectMany(x => x.Items)
                .Where(x => x.RelatedToPatient(patient))
                .ToArray();
            
            var healthSummaries = new List<HealthSummaryValue>();
            foreach (var mapItem in mapItems)
            {
                var answer = answers.FirstOrDefault(x => x.Key == mapItem.Key);

                if (!string.IsNullOrEmpty(mapItem.RequiredAnswerKey) && !string.IsNullOrEmpty(mapItem.RequiredAnswerValue))
                {
                    var previous = answers.FirstOrDefault(x => x.Key.Equals(mapItem.RequiredAnswerKey));

                    if (previous == null || !previous.Value.Equals(mapItem.RequiredAnswerValue))
                    {
                        continue;
                    }
                }
                
                if (answer is null)
                {
                    continue;
                }
                
                var question = questions.FirstOrDefault(x => x.Name == answer.Key);
                if (question is null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(mapItem.ViewLabelCondition) && !mapItem.ViewLabelCondition.Contains("[value]") && !mapItem.ViewLabelCondition.Equals(answer.Value))
                {
                    continue;
                }
                
                await ClearOldValuesAsync(currentValues, mapItem.Key, patient.GetId());
                    
                var isJsonAnswer = IsJsonAnswer(question, answer);
                
                if (isJsonAnswer && !mapItem.IsOtherSection)
                {
                    var values = GetJsonValues(answer.Value);

                    var healthSummaryValues = values.Select(value => new HealthSummaryValue(
                        patient: patient,
                        key: GetKey(answer.Key),
                        value: mapItem.Map.UseNameValue ? null : _valueFormatter.FormatValue(mapItem, value, questions),
                            name: mapItem.Map.UseNameValue ? _valueFormatter.FormatValue(mapItem, value, questions) : null));

                    healthSummaries.AddRange(healthSummaryValues);
                }
                else if (mapItem.IsOtherSection)
                {
                    var values = GetJsonValuesFromOther(answer.Value);
                
                    var healthSummaryValues = values.Select(value => new HealthSummaryValue(
                        patient: patient,
                        key: GetKey(answer.Key),
                        value: mapItem.Map.UseNameValue ? _valueFormatter.FormatValue(mapItem, value, questions) : null,
                        name: mapItem.Map.UseNameValue ? null : _valueFormatter.FormatValue(mapItem, value, questions)
                        ));
                
                    healthSummaries.AddRange(healthSummaryValues);
                }
                else
                {
                    healthSummaries.Add(new HealthSummaryValue(
                        patient: patient,
                        key: answer.Key,
                        value: mapItem.Map.UseNameValue ? null : _valueFormatter.FormatValue(mapItem, answer.Value, questions),
                        name: mapItem.Map.UseNameValue ? _valueFormatter.FormatValue(mapItem, answer.Value, questions) : null));
                }
            }

            
            await _healthSummaryService.CreateBatchAsync(healthSummaries.ToArray());
        }
        catch (Exception e)
        {
            _logger.LogError($"Saving Health Summary for patient {patient.GetId()} was failed with [Error]: {e.ToString()}");
        }
    }

    #region private

    private async Task ClearOldValuesAsync(HealthSummaryValue[] currentValues, string itemKey, int patientId)
    {
        var oldValues = currentValues
            .Where(x => ParseKey(x.Key) == itemKey)
            .ToArray();

        foreach (var oldValue in oldValues)
        {
            await _healthSummaryService.DeleteAsync(patientId, oldValue.Key);
        }
    }

    private string GetKey(string key)
    {
        var rnd = new Random();

        return key + "-" + rnd.Next(10000);
    }

    private string? ParseKey(string key)
    {
        return key.Split('-').FirstOrDefault();
    }

    private string[] GetJsonValues(string json)
    {
        try
        {
            var jsonValue = JsonConvert.DeserializeObject<CheckManyQuestionResultModel>(json);

            return jsonValue is null 
                ? Array.Empty<string>() 
                : jsonValue.V.Concat(jsonValue.O).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
    
    private string[] GetJsonValuesFromOther(string json)
    {
        try
        {
            var jsonValue = JsonConvert.DeserializeObject<CheckManyQuestionResultModel>(json);

            return jsonValue is null 
                ? Array.Empty<string>() 
                : jsonValue.O.ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
    
    private bool IsJsonAnswer(Question question, Answer answer)
    {
        return question.Type switch
        {
            QuestionType.CheckMany => true,
            QuestionType.SelectMany => true,
            QuestionType.Rate => true,
            QuestionType.SelectOne => false,
            QuestionType.CheckOne => false,
            QuestionType.TextInput => false,
            QuestionType.NumericInput => false,
            QuestionType.TimeInput => false,
            QuestionType.FillOutForm => false,
            QuestionType.DateInput => false,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    #endregion
}