using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Documents;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Documents;
using System.Net.Http;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Documents;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Shared.Data.Repository;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Integrations;
using WildHealth.AzureCognitiveServices.Clients.Models;
using WildHealth.AzureCognitiveServices.Clients.WebClients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Documents;

public class ImportAutomatedDocumentSourcesCommandHandler : IRequestHandler<ImportAutomatedDocumentSourcesCommand>
{
    private readonly IGeneralRepository<AutomatedDocumentSource> _automatedDocumentSourceRepository;
    private readonly IGeneralRepository<AutomatedDocumentSourceItem> _automatedDocumentSourceItemRepository;
    private readonly IAzureCognitiveServicesSpeechToTextWebClient _azureCognitiveServicesSpeechToTextWebClient;
    private readonly MaterializeFlow _materialize;
    private readonly ILogger<ImportAutomatedDocumentSourcesCommandHandler> _logger;
    // private readonly int _transcriptDocumentSourceTypeId = 4;
    // private const string _kbContainer = AzureBlobContainers.KbDocuments;

    public ImportAutomatedDocumentSourcesCommandHandler(
        IGeneralRepository<AutomatedDocumentSource> automatedDocumentSourceRepository,
        IGeneralRepository<AutomatedDocumentSourceItem> automatedDocumentSourceItemRepository,
        IAzureCognitiveServicesSpeechToTextWebClient azureCognitiveServicesSpeechToTextWebClient,
        MaterializeFlow materialize,
        ILogger<ImportAutomatedDocumentSourcesCommandHandler> logger)
    {
        _automatedDocumentSourceRepository = automatedDocumentSourceRepository;
        _automatedDocumentSourceItemRepository = automatedDocumentSourceItemRepository;
        _azureCognitiveServicesSpeechToTextWebClient = azureCognitiveServicesSpeechToTextWebClient;
        _materialize = materialize;
        _logger = logger;
    }

    public async Task Handle(ImportAutomatedDocumentSourcesCommand command, CancellationToken cancellationToken)
    {
        var automatedDocumentSources = await _automatedDocumentSourceRepository.All().ToArrayAsync(cancellationToken: cancellationToken);

        foreach (var automatedDocumentSource in automatedDocumentSources)
        {
            var result = automatedDocumentSource.Type switch
            {
                AutomatedDocumentSourceType.Podcast => await HandlePodcast(
                    automatedDocumentSource: automatedDocumentSource,
                    cancellationToken: cancellationToken),
                _ => throw new NotImplementedException($"The automated document source type: {automatedDocumentSource.Type} handling has not been implemented yet")
            };
        }
    }


    private async Task<AutomatedDocumentSourceItem[]> HandlePodcast(AutomatedDocumentSource automatedDocumentSource, CancellationToken cancellationToken)
    {
        var results = new List<AutomatedDocumentSourceItem>();
        
        using (var client = new HttpClient())
        {
            var xmlString = await client.GetStringAsync(automatedDocumentSource.Url, cancellationToken);
            
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(xmlString);

            XmlNodeList? items = xml.SelectNodes("/rss/channel/item");
            
            foreach (XmlNode item in items!)
            {
                var title = item["title"]!.InnerText;
                var abbreviatedTitle = title.Substring(0, Math.Min(50, title.Length-1));
                var url = item["enclosure"]!.Attributes["url"]!.Value;

                var existingItem = await _automatedDocumentSourceItemRepository
                    .All()
                    .Include(o => o.DocumentSource)
                    .Where(o => o.DocumentTitle == title)
                    .Where(o => o.AutomatedDocumentSourceId == automatedDocumentSource.GetId())
                    .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                if (existingItem is null)
                {
                    var transcriptionId = await StartTranscription(title, url);

                    if (transcriptionId is null)
                    {
                        _logger.LogError($"Unable to initiate transcription for file: {title}");
                        
                        continue;
                    }

                    results.Add(await (new AddAutomatedDocumentSourceItemFlow(
                            automatedDocumentSourceId: automatedDocumentSource.GetId(),
                            documentTitle: title,
                            integrationId: transcriptionId,
                            integrationVendor: IntegrationVendor.AzureCognitiveServices,
                            integrationPurpose: IntegrationPurposes.AutomatedDocumentSourceItem.TranscriptionId)
                        .Materialize(_materialize)
                        .Select<AutomatedDocumentSourceItem>()));
                }
            }
        }

        return results.ToArray();
    }

    /// <summary>
    /// Use Azure Cognitive Services to get the transcript 
    /// </summary>
    /// <param name="title"></param>
    /// <param name="podcastUrl"></param>
    /// <returns></returns>
    private async Task<string?> StartTranscription(string title, string podcastUrl)
    {
        var response = await _azureCognitiveServicesSpeechToTextWebClient.CreateTranscription(
            new CreateTranscriptionModel()
            {
                ContentUrls = new[] {podcastUrl},
                DisplayName = title
            });

        return response.Self?.Split("/").Last();
    }
}