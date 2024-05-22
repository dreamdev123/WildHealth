using WildHealth.Domain.Enums.Appointments;
using MediatR;

namespace WildHealth.Application.Events.Scheduler;

public record SchedulerReminderEvent(
    AppointmentReminderType ReminderType,
    string SchedulerBookingId) : INotification
{
    public SchedulerReminderEvent(
        int reminderType,
        string schedulerBookingId) : this((AppointmentReminderType)reminderType, schedulerBookingId)
    {
    }
}