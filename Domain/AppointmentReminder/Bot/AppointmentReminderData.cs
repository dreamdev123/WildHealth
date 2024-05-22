using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Chatbots;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.Domain.AppointmentReminder.Bot;

public record AppointmentReminderData
{
    public DateTime CreatedAt => Bot.CreatedAt;
    public Chatbot Bot { get; }
    public Appointment Appointment { get; }

    private readonly ActiveAppointmentReminderBot.BotState _state;

    public AppointmentReminderData(Chatbot bot, Appointment appointment)
    {
        Bot = bot;
        Appointment = appointment;
        _state = ParseState(bot);
    }

    public bool IsActive(DateTime now) => !IsDead(now);
    
    private bool IsDead(DateTime now) =>
        Appointment.Status == AppointmentStatus.Canceled || // appointment cancelled outside of the Bot
        _state.Status == ActiveAppointmentReminderBot.Status.Canceled || _state.Status == ActiveAppointmentReminderBot.Status.Confirmed || // patient confirmed or cancelled the appointment
        Appointment.StartDate <= now; // appointment start time passed

    
    public static int GetAppointmentId(Chatbot data)
    {
        if (data.Type != ChatbotType.AppointmentReminder)
            throw new DomainException($"Expected chatbot type is {nameof(ChatbotType.AppointmentReminder)}");

        var state = ParseState(data);

        return state.AppointmentId;
    }
    
    public static Appointment LookupAppointment(Chatbot data, IEnumerable<Appointment> appointments)
    {
        var targetAppointmentId = GetAppointmentId(data);
        var appointment = appointments.FirstOrDefault(a => a.Id == targetAppointmentId);
        return appointment ?? throw new DomainException($"Couldn't find an appointment with id - {targetAppointmentId}");
    }
    
    public static ActiveAppointmentReminderBot.BotState ParseState(Chatbot data)
    {
        var state = JsonConvert.DeserializeObject<ActiveAppointmentReminderBot.BotState>(data.State);
        if (state is null)
            throw new DomainException("Reminder bot state is not initialized");
        
        return state;
    }
    
    public bool MatchCode(string code) => 
        _state.YesCode.ToLower() == code.ToLower() || _state.NoCode.ToLower() == code;
}