using MediatR;

namespace WildHealth.Application.Events.Appointments;

public record AppointmentTranscriptCompletedEvent(long MeetingId, string DownloadToken, string[] DownloadUrls) : INotification
{
    
}