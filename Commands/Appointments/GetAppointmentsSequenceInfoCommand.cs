using WildHealth.Application.Commands._Base;
using FluentValidation;
using MediatR;
using WildHealth.Common.Models.Appointments;

namespace WildHealth.Application.Commands.Appointments;

public class GetAppointmentsSequenceInfoCommand : IRequest<AppointmentsSequenceInfoModel>, IValidatabe
{
    public int Id { get; }
    
    public int PatientId { get; }
    
    public GetAppointmentsSequenceInfoCommand(int id, int patientId)
    {
        Id = id;
        PatientId = patientId;
    }
    
    #region validation
    
    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetAppointmentsSequenceInfoCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
                
            RuleFor(x => x.PatientId).GreaterThan(0);
        }
    }
    
    #endregion
}