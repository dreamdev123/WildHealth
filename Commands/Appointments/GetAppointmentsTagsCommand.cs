using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Appointments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Appointments;

public class GetAppointmentsTagsCommand : IRequest<AppointmentTagsModel[]>, IValidatabe
{
    public int Id { get; }
    
    public GetAppointmentsTagsCommand(int id)
    {
        Id = id;
    }
    
    #region validation
    
    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetAppointmentsTagsCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
    
    #endregion
}