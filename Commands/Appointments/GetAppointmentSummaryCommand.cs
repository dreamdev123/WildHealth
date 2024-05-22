using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Appointments;

namespace WildHealth.Application.Commands.Appointments;

public class GetAppointmentSummaryCommand: IRequest<AppointmentSummaryModel>, IValidatabe
{
    public int PatientId { get; set; }

    public GetAppointmentSummaryCommand(int patientId)
    {
        PatientId = patientId;
    }
    
    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetAppointmentSummaryCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
        }
    }

    #endregion
}