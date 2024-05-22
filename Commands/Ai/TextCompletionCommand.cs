using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;
using WildHealth.Common.Enums;
using WildHealth.Common.Models.Ai.FlowTypes;
using WildHealth.Jenny.Clients.Models;

namespace WildHealth.Application.Commands.Ai;

public class TextCompletionCommand : IRequest<TextCompletionResponseModel>, IValidatabe
{
    public BaseFlowTypeModel FlowTypeModel { get; }
    
    public FlowType FlowType { get; }
    
    public string UserId { get; }
    
    public string AuthorId { get; }
        
    public TextCompletionCommand(
        string userId,
        string authorId,
        BaseFlowTypeModel flowTypeModel,
        FlowType flowType)
    {
        FlowTypeModel = flowTypeModel;
        UserId = userId;
        AuthorId = authorId;
        FlowType = flowType;
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
    
    private class Validator : AbstractValidator<TextCompletionCommand>
    {
        public Validator()
        {
            RuleFor(x => x.FlowTypeModel).NotNull();
            RuleFor(x => x.UserId).NotNull().NotEmpty();
            RuleFor(x => x.UserId).NotNull().NotEmpty();
            RuleFor(x => x.FlowType).IsInEnum();
        }
    }
    
    #endregion
}