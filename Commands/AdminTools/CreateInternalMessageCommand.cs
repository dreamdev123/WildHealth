using System.Collections.Generic;
using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Messages;
using WildHealth.Domain.Enums.Messages;

namespace WildHealth.Application.Commands.AdminTools;

public class CreateInternalMessageCommand: IRequest<ICollection<Message>>, IValidatabe
{
    public string Subject { get; }
    
    public string Body { get; }
    
    public MessageType[] Types { get; }
    
    public MyPatientsFilterModel FilterModel { get; }

    public int EmployeeId { get; }

    public CreateInternalMessageCommand(string subject, string body, MessageType[] types, MyPatientsFilterModel filterModel, int employeeId)
    {
        Subject = subject;
        Body = body;
        Types = types;
        FilterModel = filterModel;
        EmployeeId = employeeId;
    }
    
    #region validation

    private class Validator : AbstractValidator<CreateInternalMessageCommand>
    {
        public Validator()
        {
            RuleFor(x => x.EmployeeId).GreaterThan(0);
            RuleFor(x => x.Subject).NotNull().NotEmpty().MaximumLength(39);
            RuleFor(x => x.Body).NotNull().NotEmpty().MaximumLength(160);
            RuleFor(x => x.Types).NotNull().NotEmpty();
        }
    }

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}