using WildHealth.Common.Models.Appointments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Appointments;

public class GetAppointmentTypesCommand : IRequest<AppointmentTypeModel[]>
{
    public int PatientId { get; }

    public GetAppointmentTypesCommand(int patientId)
    {
        PatientId = patientId;
    }
    
    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetAppointmentTypesCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);
        }
    }

    #endregion
}