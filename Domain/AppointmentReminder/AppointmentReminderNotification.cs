using System;
using System.Collections.Generic;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Notifications.Abstracts;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums;

namespace WildHealth.Application.Domain.AppointmentReminder;

public record AppointmentReminderSmsNotification(string SMSBody, IEnumerable<User> Users, DateTime CreatedAt) : ISMSNotification
{
    public NotificationType Type => NotificationType.AppointmentReminder;
    public DateTime CreatedAt { get; set; } = CreatedAt;
    public string Subject { get; set; } = "";
    public IEnumerable<User> Users { get; set; } = Users;
    public string SMSBody { get; set; } = SMSBody;
    public string MessagingService => SettingsNames.Twilio.AppointmentReminderMessagingServiceSid;
}