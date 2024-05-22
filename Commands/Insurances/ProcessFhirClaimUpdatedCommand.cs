using FluentValidation;
using MediatR;
using WildHealth.Application.Commands._Base;

namespace WildHealth.Application.Commands.Insurances;

public class ProcessFhirClaimUpdatedCommand : IRequest, IValidatabe
{
    public string ClaimIntegrationId { get; }
    
    public string AppointmentIntegrationId { get; }
    
    public string PatientIntegrationId { get; }
    
    public string InsuranceIntegrationId { get; }
    
    public decimal TotalCost { get; }

    public decimal InsuranceBalance { get; }
    
    public decimal PatientBalance { get; }

    public ProcessFhirClaimUpdatedCommand(
        string claimIntegrationId,
        string appointmentIntegrationId,
        string patientIntegrationId,
        string insuranceIntegrationId,
        decimal totalCost,
        decimal insuranceBalance,
        decimal patientBalance)
    {
        ClaimIntegrationId = claimIntegrationId;
        AppointmentIntegrationId = appointmentIntegrationId;
        PatientIntegrationId = patientIntegrationId;
        InsuranceIntegrationId = insuranceIntegrationId;
        TotalCost = totalCost;
        InsuranceBalance = insuranceBalance;
        PatientBalance = patientBalance;
    }

    #region validation

    private class Validator : AbstractValidator<ProcessFhirClaimUpdatedCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ClaimIntegrationId).NotNull().NotEmpty();
            RuleFor(x => x.AppointmentIntegrationId).NotNull().NotEmpty();
            RuleFor(x => x.PatientIntegrationId).NotNull().NotEmpty();
            RuleFor(x => x.InsuranceIntegrationId).NotNull().NotEmpty();
            RuleFor(x => x.TotalCost).NotNull();
            RuleFor(x => x.InsuranceBalance).NotNull();
            RuleFor(x => x.PatientBalance).NotNull();
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