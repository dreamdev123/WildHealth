using WildHealth.Application.Commands._Base;
using WildHealth.Domain.Entities.Attachments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Insurances;

public class GetInsuranceUploadsCommand : IRequest<Attachment[]>, IValidatabe
{
    public int? UserId { get; }
    
    public int? PatientId { get; }

    protected GetInsuranceUploadsCommand(int? userId, int? patientId)
    {
        UserId = userId;
        PatientId = patientId;
    }
    
    public static GetInsuranceUploadsCommand ByUser(int userId)
    {
        return new GetInsuranceUploadsCommand(
            userId: userId,
            patientId: null
        );
    }
        
    public static GetInsuranceUploadsCommand OnBehalf(int patientId)
    {
        return new GetInsuranceUploadsCommand(
            userId: null,
            patientId: patientId
        );
    }
    
    #region validation

    private class Validator : AbstractValidator<GetInsuranceUploadsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue);
            RuleFor(x => x.PatientId).GreaterThan(0).When(x => x.PatientId.HasValue);
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