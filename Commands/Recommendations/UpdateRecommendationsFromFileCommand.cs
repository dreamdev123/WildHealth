using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Recommendations;

public class UpdateRecommendationsFromFileCommand : IRequest, IValidatabe
{
    public IFormFile RecommendationsFile { get; }
    
    public UpdateRecommendationsFromFileCommand(IFormFile recommendationsFile)
    {
        RecommendationsFile = recommendationsFile;
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
    
    private class Validator : AbstractValidator<UpdateRecommendationsFromFileCommand>
    {
        public Validator()
        {
            RuleFor(x => x.RecommendationsFile).NotNull();
        }
    }
    
    #endregion
}