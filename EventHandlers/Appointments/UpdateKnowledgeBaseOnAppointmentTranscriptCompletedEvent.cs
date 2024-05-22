using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;
using WildHealth.Application.CommandHandlers.Documents.Flows;
using WildHealth.Application.Events.Appointments;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.AzureBlobService;
using WildHealth.Application.Services.BlobFiles;
using WildHealth.Application.Services.Documents;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.Documents;
using WildHealth.Domain.Enums.Recommendations;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Extensions;
using WildHealth.Jenny.Clients.Models;
using WildHealth.Jenny.Clients.WebClients;
using WildHealth.Zoom.Clients.Exceptions;
using WildHealth.Zoom.Clients.WebClient;

namespace WildHealth.Application.EventHandlers.Appointments;

public class UpdateKnowledgeBaseOnAppointmentTranscriptCompletedEvent : INotificationHandler<AppointmentTranscriptCompletedEvent>
{
    private const int TranscriptDocumentSourceTypeId = 4;

    private readonly IZoomMeetingsWebClient _zoomMeetingsWebClient;
    private readonly IDocumentSourceTypesService _documentSourceTypesService;
    private readonly IBlobFilesService _blobFilesService;
    private readonly IAppointmentsService _appointmentsService;
    private readonly ILogger<UpdateKnowledgeBaseOnAppointmentTranscriptCompletedEvent> _logger;
    private readonly MaterializeFlow _materialize;
    
    public UpdateKnowledgeBaseOnAppointmentTranscriptCompletedEvent(
        IZoomMeetingsWebClient zoomMeetingsWebClient,
        IDocumentSourceTypesService documentSourceTypesService,
        IBlobFilesService blobFilesService,
        IAppointmentsService appointmentsService,
        ILogger<UpdateKnowledgeBaseOnAppointmentTranscriptCompletedEvent> logger,
        MaterializeFlow materialize)
    {
        _zoomMeetingsWebClient = zoomMeetingsWebClient;
        _documentSourceTypesService = documentSourceTypesService;
        _blobFilesService = blobFilesService;
        _appointmentsService = appointmentsService;
        _logger = logger;
        _materialize = materialize;
    }

    public async Task Handle(AppointmentTranscriptCompletedEvent notification, CancellationToken cancellationToken)
    {
        var meetingId = notification.MeetingId;
        var downloadToken = notification.DownloadToken;
        var downloadUrls = notification.DownloadUrls;

        var appointmentResult = await _appointmentsService.GetByMeetingSystemIdAsync(meetingId).ToTry();

        if (appointmentResult.IsError())
        {
            //This can happen because the 3 apps in zoom each receive all of the transcription webhooks.
            _logger.LogInformation($"The appointment with meetingId {meetingId} was not found in this environment");
            return;
        }

        var appointment = appointmentResult.Value();

        var patientId = appointment.PatientId;

        if (!patientId.HasValue)
        {
            throw new DomainException($"Patient Id is null for Appointment Id = {appointment.GetId()}");
        }
        
        var documentSourceType = await _documentSourceTypesService.GetByIdAsync(TranscriptDocumentSourceTypeId);
        
        
        var retryPolicy = Policy
            .Handle<ZoomException>()
            .WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000), TimeSpan.FromMilliseconds(5000) });

        var transcripts = await Task.WhenAll(downloadUrls.Select(async url =>
        {
            return await retryPolicy.ExecuteAsync(async () => await _zoomMeetingsWebClient.GetTranscriptAsync(downloadToken, url));
        }));

        var transcript = string.Join("\n", transcripts);

        var fileName = GenerateFileName(patientId.Value, meetingId);
        
        var blobFile = await _blobFilesService.CreateOrUpdateWithBlobAsync(Encoding.UTF8.GetBytes(transcript), fileName, AzureBlobContainers.KbDocuments);
        
        // Want to check if Tags are provided, if they are not, then we want to reach out to the Jenny service to tag a document
        var recommendedTags = Enumerable.Empty<HealthCategoryTag>().ToArray();

        await new AddDocumentSourceFlow(
            name: $"Meeting Transcript ({meetingId})", 
            documentSourceType: documentSourceType, 
            personaIds: null, 
            recommendedTags: recommendedTags,
            tags: null, 
            file: blobFile, 
            url: null,
            patientId: patientId.Value).Materialize(_materialize);
    }

    #region private

    private string GenerateFileName(int patientId, long meetingId)
        {
            return $"{patientId}/MeetingTranscript_{meetingId}.txt";
        }

    #endregion
}