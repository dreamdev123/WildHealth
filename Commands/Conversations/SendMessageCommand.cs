using WildHealth.Domain.Entities.Conversations;
using WildHealth.Twilio.Clients.Models.Media;
using MediatR;
using WildHealth.Twilio.Clients.Models.Conversations;

namespace WildHealth.Application.Commands.Conversations;

public class SendMessageCommand : IRequest<ConversationMessageModel>
{
    public Conversation Conversation { get; }
    public string Author { get; } 
    public string Body { get; }  
    public MediaUploadModel? Media { get; }
    
    public SendMessageCommand(
        Conversation conversation, 
        string author, 
        string body, 
        MediaUploadModel? media)
    {
        Conversation = conversation;
        Author = author;
        Body = body;
        Media = media;
    }
}