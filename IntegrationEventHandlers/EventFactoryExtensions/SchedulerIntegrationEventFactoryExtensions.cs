using System;
using System.Linq;
using System.Net;
using WildHealth.Application.Events.Scheduler;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.IntegrationEvents.Bookings.Payloads;
using WildHealth.IntegrationEvents.Scheduler.Payloads;
using WildHealth.Shared.Exceptions;
using WildHealth.TimeKit.Clients.Models.Customers;

namespace WildHealth.Application.IntegrationEventHandlers.EventFactoryExtensions;

public static class SchedulerIntegrationEventFactoryExtensions
{
    public static SchedulerBookingCreatedEvent ToBookingCreatedEvent(this SchedulerBookingCreatedPayload source) =>
        new(source.PracticeId, 
            source.BookingId, 
            source.Start, 
            source.End,
            source.SchedulerUserId, 
            source.Customers.Select(x => new CustomerModel
            {
                Name = x.Name,
                Email = x.Email
            }));
    
    public static SchedulerBookingCompletedEvent ToBookingCompletedEvent(this SchedulerBookingCompletedPayload source) =>
        new(source.PracticeId,
            source.BookingId,
            source.SchedulerUserId,
            source.Status,
            source.Completed,
            source.Customers);

    public static SchedulerBookingCancelledEvent ToBookingCancelledEvent(this SchedulerBookingCancelledPayload source) =>
        new(source.SchedulerBookingId, source.SchedulerUserId);

    public static SchedulerReminderEvent ToReminderSentEvent(this SchedulerReminderSentPayload source) =>
        new((int)source.ReminderType, source.SchedulerBookingId);
    
    public static SchedulerReminderEvent ToReminderSentEvent(this ReminderBookingPayload source)
    {
        var minutesUntilStart = Int32.Parse(source.TimeBeforeBookingStarts);

        var reminderType = GetReminderTypeFromMinutes(minutesUntilStart);
        
        return new((int)reminderType, source.BookingId);
    }

    private static AppointmentReminderType GetReminderTypeFromMinutes(int minutes)
    {
        if (minutes <= 60)
        {
            return AppointmentReminderType.Hour;
        }
        else if (minutes <= 1440)
        {
            return AppointmentReminderType.Day;
        }
        else if (minutes <= 2880)
        {
            return AppointmentReminderType.TwoDays;
        }
        else if (minutes <= 10080)
        {
            return AppointmentReminderType.Week;
        }

        throw new AppException(HttpStatusCode.BadRequest,
            $"Received reminder for {minutes} minutes which cannot be matched to an Appointment Reminder Type");
    }
        
}