using WildHealth.Application.Events.Appointments;
using WildHealth.IntegrationEvents.Meetings.Payloads;

namespace WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;

public static class MeetingIntegrationEventExtensions
{
    public static AppointmentTranscriptCompletedEvent ToAppointmentTranscriptCompletedEvent(this RecordingTranscriptCompletedPayload source) =>
        new(source.MeetingId, source.DownloadToken, source.DownloadUrls);
}