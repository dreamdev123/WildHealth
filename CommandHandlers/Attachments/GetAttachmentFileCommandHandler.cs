using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Attachments;
using WildHealth.Application.Services.Attachments;
using WildHealth.Domain.Entities.Attachments;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Conversations;
using WildHealth.Application.Services.Users;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Attachments;
using WildHealth.Shared.Enums;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;

namespace WildHealth.Application.CommandHandlers.Attachments;

public class GetAttachmentFileCommandHandler : IRequestHandler<GetAttachmentFileCommand, (Attachment? attachment, byte[] bytes)>
{
    private readonly IAttachmentsService _attachmentsService;
    private readonly IAuthTicket _authTicket;
    private readonly IPermissionsGuard _permissionsGuard;
    private readonly IScheduledMessagesService _scheduledMessageService;
    private readonly IUsersService _usersService;
    private readonly ILogger<GetAttachmentFileCommandHandler> _logger;

    public GetAttachmentFileCommandHandler(IAttachmentsService attachmentsService, IAuthTicket authTicket, IPermissionsGuard permissionsGuard,
        IScheduledMessagesService scheduledMessagesService,
        IUsersService usersService,
        ILogger<GetAttachmentFileCommandHandler> logger
        )
    {
        _attachmentsService = attachmentsService;
        _authTicket = authTicket;
        _permissionsGuard = permissionsGuard;
        _scheduledMessageService = scheduledMessagesService;
        _usersService = usersService;
        _logger = logger;
    }
    
    public async Task<(Attachment? attachment, byte[] bytes)> Handle(GetAttachmentFileCommand command, CancellationToken cancellationToken)
    {
        var attachment = await _attachmentsService.GetByIdAsync(command.Id);

        if (!_permissionsGuard.IsHighestRole())
        {
            await Authorize(attachment);
            return await ReadFile(attachment);
        }
        
        return await ReadFile(attachment);
    }

    private async Task<(Attachment? attachment, byte[] bytes)> ReadFile(Attachment attachment)
    {
        if (attachment is not null)
        {
            var bytes = await _attachmentsService.GetFileByPathAsync(attachment.Path);
            return (attachment: attachment, bytes: bytes);
        }

        return (
            attachment: null,
            bytes: Array.Empty<byte>()
        );
    }

    private async Task Authorize(Attachment attachment)
    {
        var meId = _authTicket.GetId();
        var ownerId = await GetAssociatedUserId(attachment, meId);

        if (ownerId == null)
        {
            var message = $"Could not determine the owner of attachment {attachment.Id}";
            _logger.LogError(message);
            throw new AppException(HttpStatusCode.InternalServerError, message);
        }

        var owner = await GetUser(ownerId.Value);

        if (meId == owner.Id)
        {
            //The requester is the owner.
            
        }
        else if (_authTicket.GetUserType() == UserType.Employee)
        {
            //The employee can access it if they have practice permissions.
            _permissionsGuard.AssertPermissions(owner);
        }
        else
        {
            //The requester is authenticated, but not authorized.
            throw new AppException(HttpStatusCode.Unauthorized, $"User {meId} does not own attachment {attachment.Id}");
        }
    }

    private async Task<User> GetUser(int owner)
    {
        return (await _usersService.GetByIdAsync(owner))!;
    }

    private async Task<int?> GetAssociatedUserId(Attachment attachment, int meId)
    {
        if (attachment.UserAttachment?.User != null)
        {
            return attachment.UserAttachment.UserId;
        }

        if (attachment.AgreementConfirmationAttachment?.AgreementConfirmation?.Patient != null)
        {
            var userId = attachment.AgreementConfirmationAttachment?.AgreementConfirmation?.Patient?.UserId;
            return userId;
        }

        if (attachment.NoteContentAttachment?.NoteContent?.Note?.Patient != null)
        {
            var userId = attachment.NoteContentAttachment?.NoteContent?.Note?.Patient?.UserId;
            return userId;
        }

        if (attachment.NotePdfAttachment?.Note.Patient != null)
        {
            var userId = attachment.NotePdfAttachment?.Note?.Patient?.UserId;
            return userId;
        }

        if (attachment.Type == AttachmentType.ScheduledMessageAttachment)
        {
            var sm = await _scheduledMessageService.GetByIdAsync(attachment.ReferenceId);
            var okPatient = sm != null && sm.Conversation.PatientParticipants.Any(pp => pp.Patient.UserId == meId);
            if (okPatient)
            {
                //the requester is a patient participant.
                return meId;
            } 
            
            var okEmployee = sm != null && sm.Conversation.EmployeeParticipants.Any(pp => pp.Employee.UserId == meId);
            if (okEmployee)
            {
                //the requester is an employee participant.
                return meId;
            }

            //The first patient participant is an owner.
            var cpp = sm?.Conversation.PatientParticipants.FirstOrDefault();
            return cpp?.Patient?.UserId;
        }
        return null;
    }
}