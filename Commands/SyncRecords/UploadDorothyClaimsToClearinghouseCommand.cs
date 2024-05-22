using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.SyncRecords;

public class UploadDorothyClaimsToClearinghouseCommand : IRequest
{
    public int PracticeId { get; set; }

    public UploadDorothyClaimsToClearinghouseCommand(int practiceId)
    {
        PracticeId = practiceId;
    }
    
    #region validation
    private class Validator : AbstractValidator<UploadDorothyClaimsToClearinghouseCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PracticeId).GreaterThan(0);
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