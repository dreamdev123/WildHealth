using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.SyncRecords;

public class SendDorothyFormReceivedCommsCommand : IRequest, IValidatabe
{
    public string SubmissionId { get; }

    public SendDorothyFormReceivedCommsCommand(string submissionId)
    {
        SubmissionId = submissionId;
    }
    
    #region validation

    private class Validator : AbstractValidator<SendDorothyFormReceivedCommsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.SubmissionId).NotNull().NotEmpty();
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