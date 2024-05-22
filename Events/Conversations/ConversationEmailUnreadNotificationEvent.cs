using System;
using MediatR;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Domain.Enums.Conversations;

namespace WildHealth.Application.Events.Conversations
{
    /// <summary>
    /// This event is responsible for receiving a unread message notification to email.
    /// </summary>
    public class ConversationEmailUnreadNotificationEvent : INotification
    {
        public Patient Patient { get; }
        public int UnreadMessageCount { get; }
        
        public Practice Practice { get; }
        
        public ConversationType ConversationType { get; }
        public DateTime? LastMessageSentDate { get; }
        public string LastMessageSentDateFormatted { get; }
        public string MessageLocationText { get; }

        public ConversationEmailUnreadNotificationEvent(
            Patient patient,
            int unreadMessageCount,
            Practice practice,
            ConversationType conversationType,
            DateTime? lastMessageSentDate,
            string lastMessageSentDateFormatted,
            string messageLocationText
        )
        {
            Patient = patient;
            UnreadMessageCount = unreadMessageCount;
            Practice = practice;
            ConversationType = conversationType;
            LastMessageSentDate = lastMessageSentDate;
            LastMessageSentDateFormatted = lastMessageSentDateFormatted;
            MessageLocationText = messageLocationText;
        }
    }
}