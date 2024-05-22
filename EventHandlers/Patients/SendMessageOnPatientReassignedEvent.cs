using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Events.Patients;
using MediatR;
using Twilio.Rest.Preview.HostedNumbers;
using WildHealth.Application.Commands.Conversations;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.EmailIntegrations;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Messaging.Base;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Constants;
using WildHealth.Common.Enums.Patients;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Conversations;
using WildHealth.Licensing.Api.Models.Practices;
using WildHealth.Infrastructure.EmailFactory;
using WildHealth.Infrastructure.EmailFactory.Models;
using WildHealth.Infrastructure.EmailFactory.Models.Base;
using WildHealth.Settings;
using WildHealth.Twilio.Clients.Models.Conversations;
using WildHealth.Twilio.Clients.WebClient;

namespace WildHealth.Application.EventHandlers.Patients
{
    public class SendMessageOnPatientReassignedEvent : MessagingBaseService, INotificationHandler<PatientReassignedEvent>
    {
        private readonly string _surveyLink = "https://4jnhzg70v42.typeform.com/to/mNs5rRn6";
        private readonly string _supportEmail = "support@wildhealth.com";
        private readonly string _clinicalCareTeamUrl = "www.wildhealth.com/team";
        
        private readonly IMediator _mediator;
        private readonly IPatientsService _patientsService;
        private readonly ITwilioWebClient _client;
        private readonly IConversationsService _conversationsService;
        

        public SendMessageOnPatientReassignedEvent(
            IMediator mediator,
            IPatientsService patientsService,
            ITwilioWebClient client,
            IConversationsService conversationsService,
            ISettingsManager settingsManager
            ) : base(settingsManager)
        {
            _mediator = mediator;
            _patientsService = patientsService;
            _client = client;
            _conversationsService = conversationsService;
        }

        public async Task Handle(PatientReassignedEvent notification, CancellationToken cancellationToken)
        {
            var patient = await _patientsService.GetByIdAsync(notification.PatientId);

            var message = GetMessage(notification.PatientReassignmentType, patient, notification.SummaryModels);
            
            var conversation = await _conversationsService.GetHealthConversationByPatientAsync(notification.PatientId);

            var author = GetAuthor(patient, notification.PatientReassignmentType, notification.SummaryModels);
            
            var model = new CreateConversationMessageModel
            {
                ConversationSid = conversation.VendorExternalId,
                Author = author,
                Body = message
            };
            
            var credentials = await GetMessagingCredentialsAsync(conversation.PracticeId);
            _client.Initialize(credentials);
            await _client.CreateConversationMessageAsync(model);
            
            var messagesResponse = await _client.GetMessagesAsync(conversation.VendorExternalId, MessagesOrderType.desc.ToString(), 1 );

            // Mark it as though the author read the message so they don't get a notification
            await _mediator.Send(new LastReadMessageUpdateCommand(
                conversationId: conversation.GetId(),
                conversationExternalVendorId: conversation.VendorExternalId,
                participantExternalVendorId: author,
                lastMessageReadIndex: messagesResponse.Messages.First().Index
            ));
        }

        private string GetAuthor(Patient patient, PatientReassignmentType reassignmentType, NewStaffSummaryModel[] models)
        {
            switch (reassignmentType)
            {
                case PatientReassignmentType.Both:
                case PatientReassignmentType.HealthCoachOnly:
                    return models.First(o => o.newUser.Employee.RoleId == Roles.CoachId).newUser
                        .MessagingIdentity();

                case PatientReassignmentType.ProviderOnly:
                    return models.First(o => o.newUser.Employee.RoleId == Roles.ProviderId).newUser
                        .MessagingIdentity();
            }
            
            throw new Exception($"Unable to determine author for message to send for [PatientEmail] = {patient.User.Email}");
        }

        private string GetTimeZoneString(string? timeZoneId)
        {
            if (String.IsNullOrEmpty(timeZoneId))
            {
                return string.Empty;
            }
            
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId).DisplayName;
        }
        
        private string GetMessage(PatientReassignmentType reassignmentType, Patient patient, NewStaffSummaryModel[] summaryModels)
        {
            var isAtLeastOneNewAppointmentRescheduled = summaryModels.Any(o => !o.IsSameAppointmentSlot && !String.IsNullOrEmpty(o.TimeZoneId));
            var wereNoAppointmentsToReschedule = summaryModels.All(o => o.WereNoAppointmentsToReschedule);

            var patientName = $"{patient.User.FirstName}";
            var drName = summaryModels.FirstOrDefault(o => o.priorUser.Employee.RoleId == Roles.ProviderId)?.newUser
                ?.FirstName;
            var hcName = summaryModels.FirstOrDefault(o => o.priorUser.Employee.RoleId == Roles.CoachId)?.newUser
                ?.FirstName;
            var sameSlotAppointmentDt = summaryModels.FirstOrDefault(o => o.IsSameAppointmentSlot)?.AppointmentDateTime;
            var newSlotAppointmentDt = summaryModels.FirstOrDefault()?.AppointmentDateTime;
            var commsAppointmentDt = newSlotAppointmentDt ?? sameSlotAppointmentDt;
            string appointmentDayTime = string.Empty;

            if (commsAppointmentDt is not null)
            {
                var day = commsAppointmentDt.Value.ToString("dddd, MMMM d");
                var time = commsAppointmentDt.Value.ToString("hh:mm tt");
                var timeZone = GetTimeZoneString(summaryModels.FirstOrDefault()?.TimeZoneId);
                appointmentDayTime = $"{day} at {time} {timeZone}";
            }
            
            switch (reassignmentType)
            {
                case PatientReassignmentType.Both:

                    if (isAtLeastOneNewAppointmentRescheduled)
                    {
                        return $@"Hi {patientName},

The physician and health coach who you were working with previously are no longer with Wild Health, so I wanted to introduce myself, as your new health coach, as well as your new physician.

I’m {hcName}, a Precision-Trained Health Coach and {drName} is a board-certified Precision Medicine Physician. We’re both so excited to support you in achieving your best health. You can read our bios and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

I apologize for any inconvenience, but we needed to reschedule your upcoming visit to the next available time slot for each. If this new time does not work for your schedule, you can easily reschedule the appointment within the Clarity Patient Portal > Appointments > Upcoming Appointments > Click the '...' next to the appropriate visit > Reschedule. Know that both myself and your physician have access to the forms and visit notes we have on-file for you, so we’re caught up on your care journey to-date!

If you have any immediate questions or concerns, you can send them my way or contact the Wild Health support team at {_supportEmail}. We’re all here to help.

Lastly, if you’d like to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}.

Looking forward to working with you!

In Good Health,
{hcName}
";
                    }

                    if (wereNoAppointmentsToReschedule)
                    {
                        return $@"Hi {patientName},

The physician and health coach who you were working with previously are no longer with Wild Health, so I wanted to introduce myself, as your new health coach, as well as your new physician.

I’m {hcName}, a Precision-Trained Health Coach and {drName} is a board-certified Precision Medicine Physician. We’re both so excited to support you in achieving your best health. You can read our bios and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

If you’d like to schedule a Health Coaching Session or Physician Visit with either of us, you can easily do so within the Clarity Patient Portal > Appointments > Upcoming Appointments > Schedule Appointment. Both myself and your physician have access to your forms and visit notes on-file, so we’re caught up on your care journey to-date!

If you have any immediate questions or concerns, you can send them my way or contact the Wild Health support team at {_supportEmail}. We’re all here to help.

Lastly, if you’d like to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}. 

Looking forward to working with you!

In Good Health,
{hcName}
";
                    }

                    // Appointments were rescheduled to same slot
                    return $@"Hi {patientName},

The physician and health coach who you were working with previously are no longer with Wild Health, so I wanted to introduce myself, as your new health coach, as well as your new physician.

I’m {hcName}, a Precision-Trained Health Coach and {drName} is a board-certified Precision Medicine Physician. We’re both so excited to support you in achieving your best health. You can read our bios and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

Know that both myself and your physician have access to the forms and visit notes on-file for you, so we’re caught up on your care journey to-date and prepared for your upcoming visit on {appointmentDayTime}.

If you have any immediate questions or concerns, you can send them my way or contact the Wild Health support team at {_supportEmail}. We’re all here to help.

Lastly, if you’d like to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}.

Looking forward to working with you!

In Good Health,
{hcName}
";
                
                case PatientReassignmentType.ProviderOnly:
                    if (isAtLeastOneNewAppointmentRescheduled)
                    {
                        return $@"Hi {patientName},

The physician who you were working with previously is no longer with Wild Health, so I wanted to introduce myself, as I’ll be your new provider.

I’m {drName}, a board-certified Physician with a specialized training in Precision Medicine. I’m excited to support you in achieving your best health, alongside your health coach and the wider Wild Health team. You can read my bio and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

Rest assured that I have access to, and have reviewed, your chart, so I’m caught up on your care. That said, I apologize for the inconvenience, but we needed to reschedule your upcoming Provider Visit to the next available time slot. If this new time does not work for your schedule, you can reschedule the session within the Clarity Patient Portal > Appointments > Upcoming Appointments > Click the ‘...’ next to the scheduled Provider Visit > Reschedule. Note that if you opt to cancel the visit altogether, those Visit Credits will be reapplied to your account automatically.

If you have any immediate questions or concerns, feel free to contact myself, your coach, or our support team at {_supportEmail} accordingly. We’re all here to help.

If you would like to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}.

I look forward to meeting you!

In Good Health,
{drName}
";
                    }

                    if (wereNoAppointmentsToReschedule)
                    {
                        return $@"Hi {patientName},

The physician who you were working with previously is no longer with Wild Health, so I wanted to introduce myself, as I’ll be your new provider.

I’m {drName}, a board-certified Physician with a specialized training in Precision Medicine. I’m excited to support you in achieving your best health, alongside your health coach and the wider Wild Health team. You can read my bio and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

To schedule a Physician Visit in the near future, you can easily do so within the Clarity Patient Portal > Appointments > Upcoming Appointments > Provider Visits (you may need to ‘Purchase Visit Credits’ for a Physician Visit).  Rest assured that I have access to, and have reviewed, your chart, so I’m all caught up on your care.

If you have any immediate questions or concerns, feel free to contact myself, your coach, or our support team at {_supportEmail} accordingly. We’re all here to help.

If you would like to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}.

I look forward to meeting you!

In Good Health,
{drName}
";
                    }

                    // Appointments were rescheduled to same slot
                    return $@"Hi {patientName},

The physician who you were working with previously is no longer with Wild Health, so I wanted to introduce myself, as I’ll be your new provider.

I’m {drName}, a board-certified Physician with a specialized training in Precision Medicine. I’m excited to support you in achieving your best health. You can read my bio and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

Rest assured that I have access to, and have reviewed, your chart, so I’m caught up and prepared for your upcoming visit on {appointmentDayTime}. If you have any immediate questions or concerns, though, feel free to contact myself or your coach.

If you would like to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}. As ever, our support team at {_supportEmail} is another resource that’s always available to you.

I look forward to working with you!

In Good Health,
{drName}
";
                
                case PatientReassignmentType.HealthCoachOnly:

                    if (isAtLeastOneNewAppointmentRescheduled)
                    {
                        return $@"Hi {patientName},

The health coach who you were working with previously is no longer with Wild Health, so I wanted to introduce myself, as I’ll be your new coach.

I’m {hcName}, a Precision-Trained Health Coach and I’m so excited to support you in achieving your best health. You can read my bio and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

I apologize for the inconvenience, but we needed to reschedule your upcoming coaching session to the next available time slot. If this new time does not work for you, you can reschedule the session within the Clarity Patient Portal > Appointments > Upcoming Appointments > Click the ‘...’ next to the scheduled Health Coaching Visit > Reschedule.

Rest assured that I have access to, and have reviewed, your chart, so I’m caught up on your care! If you have any immediate questions or concerns, though, feel free to send them my way.

If you want to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}. As ever, our support team at {_supportEmail} is another resource that’s always available to you.

Looking forward to working with you!

In Good Health,
{hcName}
";
                    }

                    if (wereNoAppointmentsToReschedule)
                    {
                        return $@"Hi {patientName},

The health coach who you were working with previously is no longer with Wild Health, so I wanted to introduce myself, as I’ll be your new coach.

I’m {hcName}, a Precision-Trained Health Coach and I’m so excited to support you in achieving your best health. You can read my bio and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

To schedule your next Health Coaching Session, you can easily do so within the Clarity Patient Portal > Appointments > Upcoming Appointments > Schedule Appointment.

Rest assured that I have access to, and have reviewed, your chart, so I’m already caught up on your care! If you have any immediate questions or concerns, though, feel free to send them my way. As ever, our support team at {_supportEmail} is another resource that’s always available to you.

If you want to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}.

In Good Health,
{hcName}
";
                    }
                    
                    // Appointments were rescheduled to same slot
                    return $@"Hi {patientName},

The health coach who you were working with previously is no longer with Wild Health, so I wanted to introduce myself, as I’ll be your new coach.

I’m {hcName}, a Precision-Trained Health Coach and I’m so excited to support you in achieving your best health. You can read my bio and get to know the full Wild Health Clinical team here: {_clinicalCareTeamUrl}.

Rest assured that I have access to, and have reviewed, your chart, so I’m caught up and prepared for our upcoming session on {appointmentDayTime}. If you have any immediate questions or concerns, though, feel free to send them my way.

If you want to share feedback about your membership with Wild Health thus far, this questionnaire is an opportunity to do so: {_surveyLink}. As ever, our support team at {_supportEmail} is another resource that’s always available to you.

Looking forward to working with you!

In Good Health,
{hcName}
";
            }

            throw new Exception($"Unable to determine type of message to send for [PatientEmail] = {patient.User.Email}");
        }
    }
}