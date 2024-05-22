using System;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Attachments;
using WildHealth.Application.Services.Attachments;
using WildHealth.Application.Services.Employees;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Attachments;

public class GetEmployeeAttachmentFileCommandHandler : IRequestHandler<GetEmployeeAttachmentFileCommand, byte[]>
{
    private readonly IAttachmentsService _attachmentsService;
    private readonly IEmployeeService _employeeService;

    public GetEmployeeAttachmentFileCommandHandler(
        IAttachmentsService attachmentsService, 
        IEmployeeService employeeService)
    {
        _attachmentsService = attachmentsService;
        _employeeService = employeeService;
    }

    public async Task<byte[]> Handle(GetEmployeeAttachmentFileCommand request, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(request.EmployeeId);

        var attachment = await _attachmentsService.GetUserAttachmentByTypeAsync(employee.UserId, request.AttachmentType);

        if (attachment is not null)
        {
            return await _attachmentsService.GetFileByPathAsync(attachment.Path);
        }

        return Array.Empty<byte>();
    }
}