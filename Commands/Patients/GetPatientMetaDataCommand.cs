using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Patients;

namespace WildHealth.Application.Commands.Patients;

public class GetPatientMetaDataCommand : IRequest<PatientMetaDataModel>, IValidatabe
{
    public int PatientId { get; }
    
    public GetPatientMetaDataCommand(int patientId)
    {
        PatientId = patientId;
    }
    
    #region validation

    /// <summary>
    /// <see cref="IValidatabe.IsValid"/>
    /// </summary>
    /// <returns></returns>
    public bool IsValid() => new Validator().Validate(this).IsValid;

    /// <summary>
    /// <see cref="IValidatabe.Validate"/>
    /// </summary>
    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetPatientMetaDataCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
        }
    }

    #endregion
}