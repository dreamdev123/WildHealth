using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.QuestionnaireResults;
using WildHealth.Application.Services.Questionnaires;
using WildHealth.Application.Utils.QuestionnaireToHealthSummaryParser;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Domain.Entities.HealthSummaries;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.HealthSummaries;

public class MigrateSectionOtherForSurgeryQuestionCommandHandler:IRequestHandler<MigrateSectionOtherForSurgeryQuestionCommand>
{
    private readonly IQuestionnairesService _questionnairesService;
    private readonly IPatientsService _patientsService;
    private readonly IHealthSummaryService _healthSummaryService;
    private readonly IQuestionnaireToHealthSummaryValueFormatter _valueFormatter;
    private readonly IQuestionnaireResultsService _questionnaireResultsService;

    public MigrateSectionOtherForSurgeryQuestionCommandHandler(
        IQuestionnairesService questionnairesService,
        IHealthSummaryService healthSummaryService,
        IQuestionnaireToHealthSummaryValueFormatter valueFormatter, 
        IPatientsService patientsService, 
        IQuestionnaireResultsService questionnaireResultsService)
    {
        _questionnairesService = questionnairesService;
        _healthSummaryService = healthSummaryService;
        _valueFormatter = valueFormatter;
        _patientsService = patientsService;
        _questionnaireResultsService = questionnaireResultsService;
    }
    
    public async Task Handle(MigrateSectionOtherForSurgeryQuestionCommand request, CancellationToken cancellationToken)
    {
        var patientIds = await _patientsService.GetAllPatientIds();

        var healthSummaryMap = await _healthSummaryService.GetMapAsync();

        var mapItem = healthSummaryMap
            .Where(x=> !x.UseJsonData)
            .SelectMany(x => x.Items)
            .First(x=> x.IsOtherSection);
        
        var questionnaire = await _questionnairesService.GetBySubTypeAsync(QuestionnaireSubType.DetailedHistoryIncomplete);
        
        var questions = questionnaire.Questions.ToArray();
        
        foreach (var patientId in patientIds)
        {
            try
            {
                if (!patientId.HasValue)
                {
                    continue;
                }
            
                var result = await _questionnaireResultsService.GetDetailedAsync(patientId.Value);
            
                var answer = result.Answers.FirstOrDefault(x => x.Key == mapItem.Key);
                if (answer is null)
                {
                    continue;
                }
                
                var question = questions.FirstOrDefault(x => x.Name == answer.Key);
                if (question is null)
                {
                    continue;
                }

                var healthSummary = await _healthSummaryService.GetByPatientAsync(patientId.Value);
                if (healthSummary is null || !healthSummary.Any())
                {
                    continue;
                }

                var values = GetJsonValuesFromOther(answer.Value);
                
                var patient = await _patientsService.GetByIdAsync(patientId.Value, PatientSpecifications.Empty);
                
                var healthSummaryValues = values.Select(value => new HealthSummaryValue(
                    patient: patient,
                    key: GetKey(answer.Key),
                    value: mapItem.Map.UseNameValue ? _valueFormatter.FormatValue(mapItem, value, questions) : null,
                    name: mapItem.Map.UseNameValue ? null : _valueFormatter.FormatValue(mapItem, value, questions)
                ));

                foreach (var item in healthSummaryValues)
                {
                    await _healthSummaryService.CreateAsync(item);
                }
            }
            catch (Exception)
            {
                //skip
            }
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
    private string GetKey(string key)
    {
        var rnd = new Random();

        return key + "-" + rnd.Next(10000);
    }
}