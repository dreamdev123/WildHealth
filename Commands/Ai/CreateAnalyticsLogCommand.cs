using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Ai;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Ai;

public class CreateAnalyticsLogCommand : IRequest<Unit>, IValidatabe
{
    public AiAnalyticsLoggingModel LoggingModel { get; }
        
    public CreateAnalyticsLogCommand(
        AiAnalyticsLoggingModel loggingModel)
    {
        LoggingModel = loggingModel;
    }
    
    #region Validation
    
    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);
    
    private class Validator : AbstractValidator<CreateAnalyticsLogCommand>
    {
        public Validator()
        {
            RuleFor(x => x.LoggingModel).NotNull();
        }
    }
    
    #endregion
}