using System;
using System.Collections.Generic;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Conversations;
using WildHealth.Domain.Entities.Attachments;

namespace WildHealth.Application.Commands.Conversations;

/// <summary>
/// This command provide updating scheduled message for conversation from employee
/// </summary>
public class UpdateScheduledMessageCommand : IRequest<ScheduledMessageModel>, IValidatabe
{
    public int ScheduledMessageId { get; }
    public int ConversationId { get; }
    public string Message { get; }
    public int ParticipantEmployeeId { get; }
    public DateTime TimeToSend { get; }
    public IEnumerable<Attachment> UploadedAttachments { get; }
    public IFormFile[] Attachments { get; }
    
    public int[] RemoveAttachments { get; }
    
    public UpdateScheduledMessageCommand(
        int scheduledMessageId,
        int conversationId,
        string message,
        int participantEmployeeId,
        DateTime timeToSend,
        IFormFile[] attachments, 
        int[] removeAttachments, 
        IEnumerable<Attachment> uploadedAttachments)
    {
        ScheduledMessageId = scheduledMessageId;
        ConversationId = conversationId;
        Message = message;
        ParticipantEmployeeId = participantEmployeeId;
        TimeToSend = timeToSend;
        Attachments = attachments;
        RemoveAttachments = removeAttachments;
        UploadedAttachments = uploadedAttachments;
    }

    #region validation

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new UpdateScheduledMessageCommand.Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new UpdateScheduledMessageCommand.Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<UpdateScheduledMessageCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Message).NotNull().NotEmpty();
            RuleFor(x => x.ConversationId).GreaterThan(0);
            RuleFor(x => x.ParticipantEmployeeId).GreaterThan(0);
        }
    }

    #endregion
}