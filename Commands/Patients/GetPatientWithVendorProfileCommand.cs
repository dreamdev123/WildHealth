using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Patients;
using WildHealth.Infrastructure.Data.Specifications.Enums;

namespace WildHealth.Application.Commands.Patients;

public class GetPatientWithVendorProfileCommand: IRequest<PatientModel>, IValidatabe
{
    public int PatientId { get; }
    public SpecificationsEnum Specs { get; }
    
    public GetPatientWithVendorProfileCommand(int id, SpecificationsEnum specs)
    {
        PatientId = id;
        Specs = specs;
    }
    
    #region validation

    private class Validator : AbstractValidator<GetPatientWithVendorProfileCommand>
    {
        public Validator()
        {
#pragma warning disable CS0618
            RuleFor(x => x.PatientId).Cascade(CascadeMode.StopOnFirstFailure).GreaterThan(0);
#pragma warning restore CS0618
#pragma warning disable CS0618
            RuleFor(x => x.Specs).Cascade(CascadeMode.StopOnFirstFailure).NotEmpty();
#pragma warning restore CS0618
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