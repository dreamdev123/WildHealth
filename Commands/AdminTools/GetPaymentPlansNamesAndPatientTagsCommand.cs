using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.AdminTools;

namespace WildHealth.Application.Commands.AdminTools;

public class GetPaymentPlansNamesAndPatientTagsCommand : IRequest<PlansNamesAndTagsModel>, IValidatabe
{
    public int PracticeId { get; }

    public GetPaymentPlansNamesAndPatientTagsCommand(int practiceId)
    {
        PracticeId = practiceId;
    }

    #region Validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetPaymentPlansNamesAndPatientTagsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PracticeId).GreaterThan(0);
        }
    }

    #endregion
}