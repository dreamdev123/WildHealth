using MediatR;

namespace WildHealth.Application.Events.Scheduler;

public record SchedulerBookingCancelledEvent(
    string SchedulerBookingId, 
    string SchedulerUserId) : INotification;