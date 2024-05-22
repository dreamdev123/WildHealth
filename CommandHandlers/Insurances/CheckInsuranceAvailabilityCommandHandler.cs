using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;
using WildHealth.Common.Models.Insurance;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class CheckInsuranceAvailabilityCommandHandler : IRequestHandler<CheckInsuranceAvailabilityCommand, InsuranceAvailabilityResponseModel>
{
    private readonly IPatientsService _patientsService;
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CheckInsuranceAvailabilityCommandHandler(
        IPatientsService patientsService, 
        IPaymentPlansService paymentPlansService, 
        IDateTimeProvider dateTimeProvider)
    {
        _patientsService = patientsService;
        _paymentPlansService = paymentPlansService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<InsuranceAvailabilityResponseModel> Handle(CheckInsuranceAvailabilityCommand command, CancellationToken cancellationToken)
    {
        var now = _dateTimeProvider.UtcNow();
        
        var specification = PatientSpecifications.PatientWithSubscription;

        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);

        if (patient.User.Birthday <= now.AddYears(-65))
        {
            return new InsuranceAvailabilityResponseModel()
            {
                IsAvailable = true
            };
        }

        if (patient.CurrentSubscription is null)
        {
            return new InsuranceAvailabilityResponseModel()
            {
                IsAvailable = false,
                UnavailabilityReason = $"This patient does not have an active subscription"
            };
        }

        var subscription = patient.CurrentSubscription;

        var paymentPlanId = subscription.PaymentPrice.PaymentPeriod.PaymentPlanId;
        
        var allPlans = await _paymentPlansService.GetAllAsync(patient.User.PracticeId);

        var paymentPlan = allPlans.FirstOrDefault(x => x.GetId() == paymentPlanId);

        if (paymentPlan is null)
        {
            return new InsuranceAvailabilityResponseModel()
            {
                IsAvailable = false,
                UnavailabilityReason = $"The subscription for this patient is not associated with a valid payment plan for this practice"
            };
        }

        var state = patient.User.BillingAddress.State;

        var result = paymentPlan.PaymentPlanInsuranceStates.Any(x => x.State.Name == state || x.State.Abbreviation == state);
        
        return new InsuranceAvailabilityResponseModel()
        {
            IsAvailable = result,
            UnavailabilityReason = result == false ? $"Clarity currently does not support insurance for Payment Plan: {paymentPlan.DisplayName} in state: {state}" : null
        };
    }
}