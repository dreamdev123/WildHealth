using MediatR;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;

namespace WildHealth.Application.Commands.Conversations
{
    public class SendSMSNotificationForConversationReminderCommand : IRequest
    {
        public string Body { get; }
        public Practice Practice { get; }
        public Patient Patient { get; set; }
        public int UnreadMessageCount { get; set; }
        public string LastMessageSentDateFormatted { get; }
        public string MessageLocationText { get; }

        public SendSMSNotificationForConversationReminderCommand(
            Patient patient, 
            Practice practice, 
            int count,
            string lastMessageSentDateFormatted,
            string messageLocationText,
            string messagingLink
            )
        {
            Body = $"Hi {patient.User.FirstName}, you have a new unread message in your {messageLocationText} (sent at {lastMessageSentDateFormatted}). Read & respond here: {messagingLink}";
            Patient = patient;
            UnreadMessageCount = count;
            Practice = practice;
            LastMessageSentDateFormatted = lastMessageSentDateFormatted;
            MessageLocationText = messageLocationText;
        }
    }
}
