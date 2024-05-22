using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Insurance;

namespace WildHealth.Application.Commands.Insurances;

public class UploadClaimsToClearinghouseCommand : IRequest, IValidatabe
{
    public ProfessionalClaimModel[] Claims { get; set; }
    
    public int PracticeId { get; set; }

    public UploadClaimsToClearinghouseCommand(ProfessionalClaimModel[] claims, int practiceId)
    {
        Claims = claims;
        PracticeId = practiceId;
    }
    
    #region validation
    private class Validator : AbstractValidator<UploadClaimsToClearinghouseCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PracticeId).GreaterThan(0);
            RuleFor(x => x.Claims).NotNull().NotEmpty();
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