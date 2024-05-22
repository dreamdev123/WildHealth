using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Files.Blobs;
using WildHealth.Domain.Enums.Inputs;

namespace WildHealth.Application.Commands.InsuranceConfigurations;

public class UploadInsuranceLogoCommand : IRequest<string>
{
    public int InsuranceConfigurationId { get; set; }
                
    public IFormFile File { get; }

    public UploadInsuranceLogoCommand(
        int insuranceConfigurationId,
        IFormFile file)
    {
        InsuranceConfigurationId = insuranceConfigurationId;
        File = file;
    }
    
    #region validation

    private class Validator : AbstractValidator<UploadInsuranceLogoCommand>
    {
        public Validator()
        {
            RuleFor(x => x.File).NotNull();
            RuleFor(x => x.InsuranceConfigurationId).GreaterThan(0);
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
    /// <returns></returns>
    public void Validate() => new Validator().ValidateAndThrow(this);

    #endregion
}