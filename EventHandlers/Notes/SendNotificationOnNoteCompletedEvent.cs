using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using WildHealth.Application.Events.Notes;
using WildHealth.Application.Services.Notifications;
using WildHealth.Common.Constants;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Notifications.NotificationTypes;
using WildHealth.Domain.Enums.Notes;
using WildHealth.Settings;
using MediatR;

namespace WildHealth.Application.EventHandlers.Notes;

public class SendNotificationOnNoteCompletedEvent : INotificationHandler<NoteCompletedEvent>
{
    private static readonly NoteType[] NoteTypes =
    {
        NoteType.Initial,
        NoteType.FollowUp,
        NoteType.Blank,
        NoteType.HistoryAndPhysicalInitial,
        NoteType.HistoryAndPhysicalFollowUp,
        NoteType.HistoryAndPhysicalGroupVisit,
        NoteType.Soap,
    };
    
    private readonly INotificationService _notificationService;
    private readonly ISettingsManager _settingsManager;
    private readonly AppOptions _options;
    
    public SendNotificationOnNoteCompletedEvent(
        INotificationService notificationService,
        ISettingsManager settingsManager,
        IOptions<AppOptions> options)
    {
        _notificationService = notificationService;
        _settingsManager = settingsManager;
        _options = options.Value;
    }
    
    public async Task Handle(NoteCompletedEvent @event, CancellationToken cancellationToken)
    {
        var note = @event.Note;

        if (!NoteTypes.Contains(note.Type))
        {
            return;
        }
        
        var settings = await _settingsManager.GetSettings(
            keys: new[] {SettingsNames.General.ApplicationBaseUrl},
            practiceId: note.Patient.User.PracticeId);
        
        var applicationUrl = settings[SettingsNames.General.ApplicationBaseUrl];
        var noteUrl = string.Format(_options.NoteUrl, applicationUrl, note.GetId(), note.Type);
        
        var notification = new NoteCompletedNotification(note.Patient, note.GetId(), note.Type, noteUrl);
        await _notificationService.CreateNotificationAsync(notification);
    }
}