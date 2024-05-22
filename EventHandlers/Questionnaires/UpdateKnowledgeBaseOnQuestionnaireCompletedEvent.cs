using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Newtonsoft.Json;
using WildHealth.Application.Events.Questionnaires;
using WildHealth.Application.Extensions;
using WildHealth.Application.Extensions.Questionnaire;
using WildHealth.Application.Services.Allergies;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Documents;
using WildHealth.Application.Services.Medications;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Supplements;
using WildHealth.Common.Constants;
using WildHealth.Common.Models.Questionnaires;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Domain.Enums.Questionnaires;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;

namespace WildHealth.Application.EventHandlers.Questionnaires;

public class UpdateKnowledgeBaseOnQuestionnaireCompletedEvent : INotificationHandler<QuestionnaireCompletedEvent>
{
    private const int PatientQuestionnaireDocumentSourceTypeId = 6;
    
    private readonly IPatientsService _patientsService;
    private readonly IBlobFilesService _blobFilesService;
    private readonly IDocumentSourcesService _documentSourcesService;
    private readonly IDocumentSourceTypesService _documentSourceTypesService;
    private readonly IJennyKnowledgeBaseWebClient _jennyKnowledgeBaseWebClient;
    private readonly IPatientMedicationsService _patientMedicationsService;
    private readonly IPatientsSupplementsService _patientsSupplementsService;
    private readonly IPatientAllergiesService _patientAllergiesService;
    
    public UpdateKnowledgeBaseOnQuestionnaireCompletedEvent(
        IPatientsService patientsService, 
        IBlobFilesService blobFilesService,
        IDocumentSourcesService documentSourcesService,
        IDocumentSourceTypesService documentSourceTypesService,
        IJennyKnowledgeBaseWebClient jennyKnowledgeBaseWebClient,
        IPatientMedicationsService patientMedicationsService,
        IPatientsSupplementsService patientsSupplementsService,
        IPatientAllergiesService patientAllergiesService)
    {
        _patientsService = patientsService;
        _blobFilesService = blobFilesService;
        _documentSourcesService = documentSourcesService;
        _documentSourceTypesService = documentSourceTypesService;
        _jennyKnowledgeBaseWebClient = jennyKnowledgeBaseWebClient;
        _patientMedicationsService = patientMedicationsService;
        _patientsSupplementsService = patientsSupplementsService;
        _patientAllergiesService = patientAllergiesService;
    }

    public async Task Handle(QuestionnaireCompletedEvent notification, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByIdAsync(notification.Patient.GetId());
        
        var documentSourceType = await _documentSourceTypesService.GetByIdAsync(PatientQuestionnaireDocumentSourceTypeId);

        var answers = notification.QuestionnaireResult.Answers.ToArray();
        var questions = notification.QuestionnaireResult.Questionnaire.Questions.ToArray();

        var submittedAt = notification.QuestionnaireResult.SubmittedAt;

        if (!submittedAt.HasValue)
        {
            return;
        }

        var chunks = new List<string>(); 

        foreach (var question in questions)
        {
            var answer = answers.FirstOrDefault(a => a.Key == question.Name);
            
            if (answer is null || !string.IsNullOrEmpty(question.InlineParentAnswers)) continue;

            var text = await GetAnswer(question, answer, questions, answers);

            if (text is not null)
            {
                chunks.Add($"{submittedAt.Value.Date:d}\n{text}");
            }
        }

        var fileName = $"{patient.GetId()}/QuestionnaireResults_{notification.QuestionnaireResult.GetId()}.txt";
        var documentSourceName = $"Questionnaire Results ({notification.QuestionnaireResult.GetId()})";
        
        var blobFile = await _blobFilesService.CreateOrUpdateWithBlobAsync(
            Encoding.UTF8.GetBytes(string.Join("\n", chunks)), fileName, AzureBlobContainers.KbDocuments);

        // We are directly creating the document source here instead of using AddDocumentSourceFlow because it's already chunked
        var documentSource = new DocumentSource(
            name: documentSourceName,
            documentSourceType: documentSourceType,
            file: blobFile)
        {
            DocumentChunks = chunks.Select(chunk =>
                new DocumentChunk(
                    content: chunk, 
                    chunkingStrategy: DocumentChunkingStrategy.Other)
                {
                    Tags = Enumerable.Empty<int>().ToArray()
                }).ToArray(),
            PatientDocumentSources = new List<PatientDocumentSource>()
            {
                new () { Patient = patient }
            }
        };

        documentSource = await _documentSourcesService.CreateAsync(documentSource);
        
        await _jennyKnowledgeBaseWebClient.StoreChunks(new DocumentChunkStoreRequestModel
        {
            UserUniversalId = patient.User.UserId(),
            Chunks = documentSource.DocumentChunks.Select((c) => new DocumentChunkStoreModel
            {
                Document = c.Content,
                ResourceId = c.UniversalId.ToString(),
                ResourceType = AiConstants.ResourceTypes.QuestionnaireAnswer,
                Tags = c.Tags.Select(o => o.ToString()).ToArray(),
                Personas = Array.Empty<string>()
            }).ToArray()
        });
    }
    
    #region private

    private async Task<string?> GetAnswer(Question question, Answer answer, Question[] questions, Answer[] answers)
    {
        switch (question.Type)
        {
            case QuestionType.CheckMany:
            case QuestionType.SelectMany:
                return await GetJsonAnswer(question, answer, questions, answers);
            case QuestionType.CheckOne:
            case QuestionType.SelectOne:
            case QuestionType.TextInput:
            case QuestionType.NumericInput:
            case QuestionType.TimeInput:
            case QuestionType.DateInput:
                return await GetPlainTextAnswer(question, answer, questions, answers);
            case QuestionType.Rate:
                return await GetRateAnswer(question, answer, questions, answers);
            case QuestionType.FillOutForm:
                return await GetFillOutAnswer(question, answer);
            default:
                throw new ArgumentOutOfRangeException($"Unrecognized question type = {question.Type}");
        }
    }

    private async Task<string> GetPlainTextAnswer(Question question, Answer answer, Question[] questions, Answer[] answers)
    {
        var strings = new List<string> { $"Question: {question.DisplayName}\nAnswer: {answer.Value}" };
        
        foreach (var name in question.ChildrenNames)
        {
            var childQuestion = questions.FirstOrDefault(o => o.Name == name);
            var childAnswer = answers.FirstOrDefault(o => o.Key == name);
            
            if (childAnswer is null || childQuestion is null) continue;

            var text = await GetAnswer(childQuestion, childAnswer, questions, answers);

            if (text is not null)
            {
                strings.Add(text);
            }
        }
        
        return string.Join("\n", strings);
    }

    private async Task<string> GetJsonAnswer(Question question, Answer answer, Question[] questions, Answer[] answers)
    {
        var results = JsonConvert.DeserializeObject<CheckManyQuestionResultModel>(answer.Value)?.V!;
        
        var strings = new List<string> { $"Question: {question.DisplayName}\nAnswer:{string.Join(", ", results)}" };

        foreach (var name in question.ChildrenNames)
        {
            var childQuestion = questions.FirstOrDefault(o => o.Name == name);
            var childAnswer = answers.FirstOrDefault(o => o.Key == name);
            
            if (childAnswer is null || childQuestion is null) continue;

            var text = await GetAnswer(childQuestion, childAnswer, questions, answers);

            if (text is not null)
            {
                strings.Add(text);
            }
        }
        
        return string.Join("\n", strings);
    }

    private async Task<string?> GetRateAnswer(Question question, Answer answer, Question[] questions, Answer[] answers)
    {
        var results = JsonConvert.DeserializeObject<CheckManyQuestionResultModel>(answer.Value)?.V!;
        var rate = results.FirstOrDefault();
        if (rate is not null)
        {
            var min = question.Min;
            var max = question.Max;
            var range = question.Variants;
            
            var strings = new List<string> { $"Question: On a scale of {min} to {max} ({min} being {range[0]} and {max} being {range[1]}), {question.DisplayName}\nAnswer: {rate}" };

            foreach (var name in question.ChildrenNames)
            {
                var childQuestion = questions.FirstOrDefault(o => o.Name == name);
                var childAnswer = answers.FirstOrDefault(o => o.Key == name);
            
                if (childAnswer is null || childQuestion is null) continue;

                var text = await GetAnswer(childQuestion, childAnswer, questions, answers);

                if (text is not null)
                {
                    strings.Add(text);
                }
            }
                        
            return string.Join("\n", strings);
        }

        return null;
    }

    private async Task<string?> GetFillOutAnswer(Question question, Answer answer)
    {
        var ids = answer.Value.Split(",").Select(id => Convert.ToInt32(id)).ToArray();
        switch (answer.Key)
        {
            case QuestionKey.Medications:
                var medications = await _patientMedicationsService.GetByIdsAsync(ids);
                return $"Question: {question.DisplayName}\nAnswer: {string.Join(", ", medications.Select(m => $"{m.Name}"))}";
            case QuestionKey.Supplement:
                var supplements = await _patientsSupplementsService.GetByIdsAsync(ids);
                return $"Question: {question.DisplayName}\nAnswer: {string.Join(", ", supplements.Select(m => $"{m.Name}"))}";
            case QuestionKey.Allergies:
                var allergies = await _patientAllergiesService.GetByIdsAsync(ids);
                return $"Question: {question.DisplayName}\nAnswer: {string.Join(", ", allergies.Select(m => $"{m.Name}"))}";
        }

        return null;
    }
    
    #endregion
}