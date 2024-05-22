using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Options;
using WildHealth.Application.Events.Notes;
using WildHealth.Application.Services.Notifications;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Settings;

namespace WildHealth.Application.EventHandlers.Notes;

public class SendNotificationOnNoteSignOffRequestedEvent : INotificationHandler<NoteSignOffRequestedEvent>
{
    private readonly INotificationService _notificationService;
    private readonly ISettingsManager _settingsManager;
    private readonly AppOptions _options;

    public SendNotificationOnNoteSignOffRequestedEvent(
        INotificationService notificationService,
        ISettingsManager settingsManager,
        IOptions<AppOptions> options)
    {
        _notificationService = notificationService;
        _settingsManager = settingsManager;
        _options = options.Value;
    }

    public async Task Handle(NoteSignOffRequestedEvent @event, CancellationToken cancellationToken)
    {
        var note = @event.Note;
        
        var settings = await _settingsManager.GetSettings(
            keys: new[] {SettingsNames.General.ApplicationBaseUrl},
            practiceId: note.Employee.User.PracticeId);
        
        var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
        var noteSignOffUrl = string.Format(_options.NoteSignOffUrl, applicationUrl, note.GetId(), note.Type);
        
        var notification = new NoteSignOffNotification(note.Patient, noteSignOffUrl, note.GetId(), note.Type, @event.AssignedTo);
        await _notificationService.CreateNotificationAsync(notification);
    }
}