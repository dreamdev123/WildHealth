using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WildHealth.Application.Functional.Flow;
using WildHealth.Common.Constants;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Chatbots;
using WildHealth.Domain.Exceptions;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Domain.Models.Extensions;
using WildHealth.IntegrationEvents._Base;
using WildHealth.IntegrationEvents.Appointments;
using WildHealth.IntegrationEvents.Appointments.Payloads;
using static WildHealth.Application.Domain.AppointmentReminder.Bot.ActiveAppointmentReminderBot;

namespace WildHealth.Application.Domain.AppointmentReminder.Bot;

public record ActiveAppointmentReminderBot(BotState State, Appointment Appointment, DateTime Now, string DashboardLink) : IWhChatbot
{
    public Chatbot? Bot { get; set; }
    public BotState State { get; set; } = State;
    public Appointment Appointment { get; set; } = Appointment;
    public DateTime Now { get; set; } = Now;
    
    private string BotName => nameof(ActiveAppointmentReminderBot);
    
    public ActiveAppointmentReminderBot(Chatbot bot, Appointment appointment, DateTime now, string DashboardLink) : this(AppointmentReminderData.ParseState(bot), appointment, now, DashboardLink)
    {
        Bot = bot;
    }
    
    public MaterialisableFlowResult Tell(string? inputMessage = null)
    {
        var response = inputMessage switch
        {
            null => InitialMessage(),
            _ when inputMessage == State.YesCode => ConfirmAppointment(),
            _ when inputMessage == State.NoCode => CancelAppointment(),
            _ => MaterialisableFlowResult.Empty
        };

        return response;
    }
    
    private MaterialisableFlowResult InitialMessage()
    {
        var appointmentDomain = AppointmentDomain.Create(Appointment);
        var joinLink = string.IsNullOrEmpty(Appointment.JoinLink) ? "" : $"Link to join: {Appointment.JoinLink}.";
        var smsBody = $"Your appt is the day after tomorrow at {appointmentDomain.GetTimeZoneStartTime(true)} {appointmentDomain.GetTimezoneDisplayName(true)}. {joinLink} \nPlease reply “{State.YesCode}” to confirm your appointment or “{State.NoCode}” to cancel. To make changes, log in to Clarity: {DashboardLink}. Reply STOP to opt out.";
        var smsNotification = new AppointmentReminderSmsNotification(smsBody,Appointment.Patient.User.ToEnumerable(), Now);
        
        State = State with
        {
            Messages = new List<ConversationMessage>{ new(BotName, "Patient", smsBody, Now) }   // add message to the state
        };

        var newBot = new Chatbot
        {
            Name = BotName,
            Type = ChatbotType.AppointmentReminder,
            PatientId = Appointment.PatientId!.Value,
            MessageServiceType = SettingsNames.Twilio.AppointmentReminderMessagingServiceSid,
            State = JsonConvert.SerializeObject(State)
        }.Added();
        
        return newBot + smsNotification;
    }
    
    private MaterialisableFlowResult ConfirmAppointment()
    {
        if (Bot is null) 
            throw new DomainException("AppointmentReminderBot does not exist");

        var patientMessageLog = new ConversationMessage("Patient", BotName, State.YesCode, Now);
        var botResponseMessage = new ConversationMessage(
            nameof(ActiveAppointmentReminderBot), 
            $"Patient",
            $"Great, done. Thanks!\nYour appointment has been confirmed.",
            Now);
        State = State with
        {
            Status = Status.Confirmed,
            Messages = State.Messages.Concat(new []{patientMessageLog, botResponseMessage}).ToList()  // add new messages to the state
        }; 
        Bot.State = JsonConvert.SerializeObject(State);

        var integrationEvent = new AppointmentIntegrationEvent(
            new AppointmentVerifiedPayload("sms", Appointment.Purpose.ToString(), Appointment.Comment, Appointment.JoinLink, "none"), 
            new PatientMetadataModel(Appointment.Patient.Id.GetValueOrDefault(), Appointment.Patient.User.UniversalId.ToString()), 
            Now);
        
        var smsResponse = new AppointmentReminderSmsNotification(botResponseMessage.Body,Appointment.Patient.User.ToEnumerable(), Now);
        
        return Bot.Updated() + smsResponse + integrationEvent;
    }

    private MaterialisableFlowResult CancelAppointment()
    {
        if (Bot is null) 
            throw new DomainException("AppointmentReminderBot does not exist");
      
        var patientMessageLog = new ConversationMessage("Patient", nameof(ActiveAppointmentReminderBot), State.NoCode, Now);
        var botResponseMessage = new ConversationMessage(
            nameof(ActiveAppointmentReminderBot), 
            $"Patient",
            $"Thanks! Your appointment has been cancelled. To schedule a new appointment, log in to Clarity: {DashboardLink}.",
            Now);
        
        State = State with
        {
            Status = Status.Canceled,
            Messages = State.Messages.Concat(new []{patientMessageLog, botResponseMessage}).ToList()  // add new messages to the state
        }; 
        Bot.State = JsonConvert.SerializeObject(State);
        
        var smsResponse = new AppointmentReminderSmsNotification(botResponseMessage.Body,Appointment.Patient.User.ToEnumerable(), Now);
        
        return Bot.Updated() + smsResponse + new AppointmentCancellationRequestedEvent(State.AppointmentId, Appointment.Patient.UserId);
    }

    public record BotState(int AppointmentId, string YesCode, string NoCode, List<ConversationMessage> Messages, Status Status = Status.New);
    
    public enum Status
    {
        New = 0,
        Confirmed = 10,
        Canceled = 20
    }
    
    public record ConversationMessage(string From, string To, string Body, DateTime Timestamp);
}