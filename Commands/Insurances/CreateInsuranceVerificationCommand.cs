using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Insurances;

namespace WildHealth.Application.Commands.Insurances;

public class CreateInsuranceVerificationCommand: IRequest<InsuranceVerification>, IValidatabe
{
    public int PatientId { get; set; }

    public bool IsVerified { get; set; }

    public decimal? Copay { get; set; }

    public int CoverageId { get; set; }
    
    public string? ErrorCode { get; set; }
    
    public string? Raw271 { get; set; }

    public CreateInsuranceVerificationCommand(
        int patientId, 
        bool isVerified, 
        decimal? copay, 
        int coverageId, 
        string? errorCode = null,
        string? raw271 = null)
    {
        PatientId = patientId;
        IsVerified = isVerified;
        Copay = copay;
        CoverageId = coverageId;
        ErrorCode = errorCode;
        Raw271 = raw271;
    }

    #region validation

    private class Validator : AbstractValidator<CreateInsuranceVerificationCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
            RuleFor(x => x.CoverageId).NotEmpty().NotEmpty();
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