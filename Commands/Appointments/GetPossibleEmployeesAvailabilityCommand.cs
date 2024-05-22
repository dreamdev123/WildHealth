using System;
using WildHealth.Application.Commands._Base;
using WildHealth.Common.Models.Appointments;
using FluentValidation;
using MediatR;

namespace WildHealth.Application.Commands.Appointments;

public class GetPossibleEmployeesAvailabilityCommand : IRequest<PossiblePatientEmployeesAvailabilityModel>, IValidatabe
{
    public int PatientId { get; }

    public int[] RoleIds { get; }
    
    public int ConfigurationId { get; }

    public DateTime StartDate { get; }

    public DateTime EndDate { get; }
    
    public GetPossibleEmployeesAvailabilityCommand(
        int patientId, 
        int configurationId,
        DateTime startDate, 
        DateTime endDate, 
        int[] roleIds)
    {
        PatientId = patientId;
        ConfigurationId = configurationId;
        StartDate = startDate;
        EndDate = endDate;
        RoleIds = roleIds;
    }

    #region validation

    public bool IsValid() => new Validator().Validate(this).IsValid;

    public void Validate() => new Validator().ValidateAndThrow(this);

    private class Validator : AbstractValidator<GetPossibleEmployeesAvailabilityCommand>
    {
        public Validator()
        {
            RuleFor(x => x.PatientId).GreaterThan(0);

            RuleFor(x => x.ConfigurationId).GreaterThan(0);

            RuleFor(x => x.RoleIds).NotNull().NotEmpty();

            RuleFor(x => x.EndDate)
                .GreaterThan(x => x.StartDate)
                .WithMessage("End date cannot be greater than start date.");
        }
    }

    #endregion
}