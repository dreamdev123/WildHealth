using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Recommendations;

public class UploadRecommendationFileCommand : IRequest, IValidatabe
{
    public IFormFile File { get; }
    public int StartAtRow { get; }
    
    public UploadRecommendationFileCommand(IFormFile file, int startAtRow)
    {
        File = file;
        StartAtRow = startAtRow;
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
    
    private class Validator : AbstractValidator<UploadRecommendationFileCommand>
    {
        public Validator()
        {
            RuleFor(x => x.File).NotNull();
        }
    }
    
    #endregion
}