using System;
using System.Collections.Generic;
using WildHealth.TimeKit.Clients.Models.Customers;
using MediatR;

namespace WildHealth.Application.Events.Scheduler;

public record SchedulerBookingCreatedEvent(int PracticeId,
    string BookingId,
    DateTime Start,
    DateTime End,
    string SchedulerUserId,
    IEnumerable<CustomerModel> Customers) : INotification;