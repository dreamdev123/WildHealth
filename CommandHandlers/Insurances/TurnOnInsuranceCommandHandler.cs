using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Payments;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Application.Commands.Tags;
using MediatR;
using WildHealth.Application.Events.Insurances;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class TurnOnInsuranceCommandHandler : IRequestHandler<TurnOnInsuranceCommand>
{
    private readonly IPatientsService _patientsService;
    private readonly IPaymentPlansService _paymentPlansService;
    private readonly IMediator _mediator;
    private readonly ILogger _logger;

    public TurnOnInsuranceCommandHandler(
        IPatientsService patientsService, 
        IPaymentPlansService paymentPlansService, 
        IMediator mediator,
        ILogger<TurnOnInsuranceCommandHandler> logger)
    {
        _patientsService = patientsService;
        _paymentPlansService = paymentPlansService;
        _mediator = mediator;
        _logger = logger;
    }
    
    public Task Handle(TurnOnInsuranceCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Insurance turn on process for patient with [Id] = {command.PatientId} started.");

        throw new AppException(statusCode: HttpStatusCode.Forbidden, $"Turning on insurance has been disabled from the application");
        //
        // var patientSpecification = PatientSpecifications.PatientWithSubscription;
        //
        // var patient = await _patientsService.GetByIdAsync(command.PatientId, patientSpecification);
        //
        // var subscription = patient.MostRecentSubscription;
        //
        // if (!AssertSubscriptionType(subscription, SubscriptionType.Regular))
        // {
        //     _logger.LogInformation($"Insurance turn on process for patient with [Id] = {command.PatientId} was skipped.");
        //     
        //     return;
        // }
        //
        // var (altPaymentPrice, altPromoCode) = await _paymentPlansService.GetAlternativePaymentPrice(subscription);
        //
        // var changeSubscriptionCommand = new ChangeSubscriptionPaymentPriceCommand(
        //     currentSubscriptionId: subscription.GetId(),
        //     newPaymentPriceId: altPaymentPrice.GetId(),
        //     startDate: null,
        //     endDate: null,
        //     couponCode: altPromoCode,
        //     employerProductId: null
        // );
        //
        // var newSubscription =  await _mediator.Send(changeSubscriptionCommand, cancellationToken);
        // await _mediator.Send(new CreateTagCommand(patient, Common.Constants.Tags.InsurancePending), cancellationToken);
        // await _mediator.Send(new UpdateFhirPatientCommand(patient.GetId()), cancellationToken);
        // await _mediator.Publish(new PatientUpdatedEvent(patient.GetId(), Enumerable.Empty<int>()), cancellationToken);
        //
        // await _mediator.Publish(new SubscriptionPaymentFlowChangedEvent(
        //     subscriptionId: subscription.GetId(),
        //     priorFlow: subscription.GetSubscriptionType().ToString(),
        //     newFlow: newSubscription.GetSubscriptionType().ToString()
        // ));
        //
        // _logger.LogInformation($"Insurance turn on process for patient with [Id] = {command.PatientId} finished.");
    }
    
    #region private

    /// <summary>
    /// Asserts subscription is insurance type
    /// </summary>
    /// <param name="subscription"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private bool AssertSubscriptionType(Subscription subscription, SubscriptionType type)
    {
        if (subscription is null)
        {
            return false;
        }

        return subscription.GetSubscriptionType() == type;
    }

    #endregion
}