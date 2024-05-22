using System.Collections.Generic;
using MediatR;
using WildHealth.IntegrationEvents.Bookings.Models;

namespace WildHealth.Application.Events.Scheduler;

public record SchedulerBookingCompletedEvent(
    int practiceId, 
    string BookingId, 
    string SchedulerUserId, 
    string Status, 
    bool Completed, 
    IEnumerable<CustomerModel> Customers) : INotification;