using System;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.Domain.AppointmentReminder.Bot;

public record ExpiredAppointmentReminderBot(User User, DateTime Now, string DashboardLink) : IWhChatbot
{
    public MaterialisableFlowResult Tell(string? inputMessage = null)
    {
        var smsResponse = new AppointmentReminderSmsNotification($"The conversation has ended. Please log in to Clarity to make any changes {DashboardLink}",User.ToEnumerable(), Now);
        return smsResponse.ToFlowResult();
    }
}