using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.States;
using WildHealth.Common.Extensions;
using WildHealth.Infrastructure.Data.Specifications;
using MediatR;
using WildHealth.Application.Services.PaymentPlans;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class GetAvailableInsurancesCommandHandler : IRequestHandler<GetAvailableInsurancesCommand, Insurance[]>
{
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IPatientsService _patientsService;
    private readonly IStatesService _statesService;
    private readonly IMediator _mediator;

    public GetAvailableInsurancesCommandHandler(
        IPaymentPlansService paymentPlansService,
        IPatientsService patientsService,
        IStatesService statesService,
        IMediator mediator)
    {
        _paymentPlansService = paymentPlansService;
        _patientsService = patientsService;
        _statesService = statesService;
        _mediator = mediator;
    }

    public async Task<Insurance[]> Handle(GetAvailableInsurancesCommand command, CancellationToken cancellationToken)
    {
        var specification = PatientSpecifications.PatientWithSubscription;

        var patient = await _patientsService.GetByIdAsync(command.PatientId, specification);

        var state = await _statesService.GetByValue(patient.User.BillingAddress.State);
        
        var age = patient.User.Birthday?.Age();

        var stateName = patient.User.BillingAddress.State;
        
        var subscription = patient.CurrentSubscription;

        if (subscription is null)
        {
            return Array.Empty<Insurance>();
        }

        var paymentPlanId = subscription.PaymentPrice.PaymentPeriod.PaymentPlanId;
        
        var allPlans = await _paymentPlansService.GetAllAsync(patient.User.PracticeId);

        var paymentPlan = allPlans.FirstOrDefault(x => x.GetId() == paymentPlanId);

        if (paymentPlan is null)
        {
            return Array.Empty<Insurance>();
        }

        if (paymentPlan.PaymentPlanInsuranceStates.Any(x => x.State.Name == stateName || x.State.Abbreviation == stateName))
        {

            var getOrganizationsCommand = new GetInsurancesCommand(
                age: null,
                stateId: state.GetId()
            );
        
            return await _mediator.Send(getOrganizationsCommand, cancellationToken);
        }
        else
        {
            var getOrganizationsCommand = new GetInsurancesCommand(
                age: age,
                stateId: state.GetId()
            );
        
            return await _mediator.Send(getOrganizationsCommand, cancellationToken);
        }
    }
}