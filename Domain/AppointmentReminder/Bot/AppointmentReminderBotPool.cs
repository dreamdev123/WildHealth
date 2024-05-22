using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Extensions;
using WildHealth.Domain.Entities.Appointments;
using static WildHealth.Application.Domain.AppointmentReminder.Bot.ActiveAppointmentReminderBot;

namespace WildHealth.Application.Domain.AppointmentReminder.Bot;

/// <summary>
/// Manages appointment reminders in a given context.
/// </summary>
public record AppointmentReminderBotPool(List<AppointmentReminderData> AllBots, int BotTimeToLiveInHours, DateTime Now, string DashboardLink)
{
    private static List<(string, string)> SupportedCodes = new()
    {
        ("1", "9"),
        ("2", "8"),
        ("3", "7"),
        ("4", "6")
    };
    
    /// <summary>
    /// Creates the new appointment reminder bot and assigns confirmation and cancellation codes in given context.
    /// </summary>
    public IWhChatbot CreateNew(Appointment appointment)
    {
        var activeBots = AllBots
            .Where(b => b.IsActive(Now))
            .Select(b => new ActiveAppointmentReminderBot(b.Bot, b.Appointment, Now, DashboardLink))
            .ToList();
        
        // Currently we don't support more than 4 active bots at a time
        if (activeBots.Count >= SupportedCodes.Count) 
            return new NoneAppointmentReminderBot();
        
        var (yesCode, noCode) = PeekFreeCodePair(activeBots);
        var state = new BotState(appointment.GetId(), yesCode, noCode,new List<ConversationMessage>());
        return new ActiveAppointmentReminderBot(state, appointment, Now, DashboardLink);
    }

    /// <summary>
    /// Finds the appointment reminder bot by Yes/No Code.
    /// </summary>
    public IWhChatbot Find(string code)
    {
        var matchedByCode = AllBots.Where(x => x.MatchCode(code)).ToList();
        if (matchedByCode.Empty())
            return new NoneAppointmentReminderBot(); // no bot found by given code

        var user = matchedByCode.First().Appointment.Patient.User;

        var active = matchedByCode
            .Where(x => x.IsActive(Now))
            .MaxBy(x => x.CreatedAt);
        
        return active is null ? 
            new ExpiredAppointmentReminderBot(user, Now, DashboardLink) :
            new ActiveAppointmentReminderBot(active.Bot, active.Appointment, Now, DashboardLink);
    }
    
    private (string, string) PeekFreeCodePair(List<ActiveAppointmentReminderBot> aliveBots)
    {
        var occupiedCodes = aliveBots
            .Select(x => (x.State.YesCode, x.State.NoCode))
            .ToHashSet();

        return SupportedCodes.First(codePair => !occupiedCodes.Contains(codePair));
    }
}