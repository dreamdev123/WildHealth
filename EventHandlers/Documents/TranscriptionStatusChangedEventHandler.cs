using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Application.Events.Documents;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Documents;
using WildHealth.Application.Services.Integrations;
using WildHealth.AzureCognitiveServices.Clients.Models;
using WildHealth.AzureCognitiveServices.Clients.WebClients;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.EventHandlers.Documents;


public class TranscriptionStatusChangedEventHandler : INotificationHandler<TranscriptionStatusChangedEvent>
{
    private readonly IIntegrationsService _integrationsService;
    private readonly IAzureCognitiveServicesSpeechToTextWebClient _azureCognitiveServicesSpeechToTextWebClient;
    private readonly IBlobFilesService _blobFilesService;
    private readonly IDocumentSourceTypesService _documentSourceTypesService;
    private readonly IGeneralRepository<AutomatedDocumentSourceItem> _automateddocumentSourceItemRepository;
    private readonly MaterializeFlow _materialize;
    private readonly ILogger<TranscriptionStatusChangedEventHandler> _logger;

    public TranscriptionStatusChangedEventHandler(
            IIntegrationsService integrationsService, 
            IAzureCognitiveServicesSpeechToTextWebClient azureCognitiveServicesSpeechToTextWebClient,
            IBlobFilesService blobFilesService,
            IDocumentSourceTypesService documentSourceTypesService,
            IGeneralRepository<AutomatedDocumentSourceItem> automateddocumentSourceItemRepository,
            MaterializeFlow materialize,
            ILogger<TranscriptionStatusChangedEventHandler> logger)
    {
        _integrationsService = integrationsService;
        _azureCognitiveServicesSpeechToTextWebClient = azureCognitiveServicesSpeechToTextWebClient;
        _blobFilesService = blobFilesService;
        _documentSourceTypesService = documentSourceTypesService;
        _automateddocumentSourceItemRepository = automateddocumentSourceItemRepository;
        _materialize = materialize;
        _logger = logger;
    }

    public async Task Handle(TranscriptionStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Received transcription status changed notification for [Status] = {notification.Status} for [TranscriptionId] = {notification.TranscriptionId}");
        
        var integration =
            await _integrationsService.GetForAutomatedDocumentSourceItemAsync(IntegrationVendor.AzureCognitiveServices, notification.TranscriptionId);

        if (integration?.AutomatedDocumentSourceItemIntegration?.AutomatedDocumentSourceItem is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Could not locate [IntegrationId]: {notification.TranscriptionId} on transcription status changed");
        }

        var automatedDocumentSourceItem =
            integration.AutomatedDocumentSourceItemIntegration.AutomatedDocumentSourceItem;

        if (notification.Status == AutomatedDocumentSourceItemStatus.None)
        {
            _logger.LogInformation($"The [Status]: {notification.Status} is not supported for processing");
            
            return;
        }
        
        automatedDocumentSourceItem.SetStatus(notification.Status);

        var result = notification.Status switch
        {
            AutomatedDocumentSourceItemStatus.DocumentGenerationSucceeded => await ProcessDocumentGenerationSucceeded(automatedDocumentSourceItem, notification),
            _ => null
        };
    }

    private async Task<AutomatedDocumentSourceItem?> ProcessDocumentGenerationSucceeded(AutomatedDocumentSourceItem item,
        TranscriptionStatusChangedEvent notification)
    {
        var transcript = await GetTranscription(notification.TranscriptionId).ToTry();

        if (transcript.IsError())
        {
            _logger.LogError($"Unable to get [TranscriptionId] = {notification.TranscriptionId} from the vendor, [Details] = {transcript.ErrorValue()}");

            return default;
        }
        
        var fileName = GenerateFileName(notification.TranscriptionId);
        
        var documentSourceType = await _documentSourceTypesService.GetByAutomatedDocumentSourceType(item.AutomatedDocumentSource.Type);
        
        var blobFile = await _blobFilesService.CreateOrUpdateWithBlobAsync(Encoding.UTF8.GetBytes(transcript.SuccessValue()), fileName, AzureBlobContainers.KbDocuments);

        // Want to check if Tags are provided, if they are not, then we want to reach out to the Jenny service to tag a document
        var recommendedTags = Enumerable.Empty<HealthCategoryTag>().ToArray();

        var documentSourceName = $"Automated Document Source Item ({notification.TranscriptionId})";
        
        _logger.LogInformation($"Creating [DocumentSourceName] = {documentSourceName}, with [Type] = {documentSourceType}, [RecommendedTags] = {string.Join(", ", recommendedTags)}, [BlobFile] = {blobFile?.Name}");
        
        var documentSource = await new AddDocumentSourceFlow(
                name: documentSourceName,
                documentSourceType: documentSourceType,
                personaIds: null,
                recommendedTags: recommendedTags,
                tags: null,
                file: blobFile,
                url: null).Materialize(_materialize)
            .Select<DocumentSource>();

        _logger.LogInformation($"Created [DocumentSourceId] = {documentSource.GetId()}");
        
        item.DocumentSourceId = documentSource.GetId();
        
        _automateddocumentSourceItemRepository.Edit(item);

        await _automateddocumentSourceItemRepository.SaveAsync();

        return item;
    }

    private string GenerateFileName(string transcriptionId)
    {
        return $"AutomatedDocumentSource/Integration/{transcriptionId}.txt";
    }
    
    private async Task<string> GetTranscription(string transcriptionId)
    {
        var fileResponse =
            await _azureCognitiveServicesSpeechToTextWebClient.GetTranscriptionFiles(transcriptionId);
        
        var url = fileResponse.Values.FirstOrDefault(o => o.Kind == "Transcription")?.Links.ContentUrl;

        if (url is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"Unable to locate the url for the [TranscriptionId] = {transcriptionId}");
        }


        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(url);
            var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var responseModel = JsonConvert.DeserializeObject<TranscriptFileTextResponseModel>(responseBody);

            if (responseModel != null && !responseModel.CombinedRecognizedPhrases.Any())
            {
                throw new AppException(HttpStatusCode.NotFound,
                    $"Unable to evaluate the transcript in the response file for [TranscriptionId]: {transcriptionId}");
            }
            
            var transcription = responseModel?.CombinedRecognizedPhrases.FirstOrDefault()?.Display;

            if (transcription is null)
            {
                throw new AppException(HttpStatusCode.NotFound,
                    $"The files response for [TranscriptionId]: {transcriptionId} resulted in not transcript information");
            }

            return transcription;
        }
    }
}