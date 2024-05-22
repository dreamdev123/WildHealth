using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Enums.Attachments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Attachments;

public class GetEmployeeAttachmentFileCommand : IRequest<byte[]>, IValidatabe
{
    public int EmployeeId { get; }

    public AttachmentType AttachmentType { get; }
    
    public GetEmployeeAttachmentFileCommand(int employeeId, AttachmentType attachmentType)
    {
        EmployeeId = employeeId;
        AttachmentType = attachmentType;
    }

    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetEmployeeAttachmentFileCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).GreaterThan(0);
            RuleFor(x => x.AttachmentType).IsInEnum();
        }
    }

    #endregion
}