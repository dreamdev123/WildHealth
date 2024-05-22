using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.Subscriptions;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Subscriptions;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Subscriptions;

public class CheckIntegrationStatusCommandHandler : IRequestHandler<CheckIntegrationStatusCommand, Dictionary<string, SubscriptionIntegrationModel[]>>
{
    private readonly IIntegrationServiceFactory _integrationServiceFactory;
    private readonly IPatientsService _patientsService;

    public CheckIntegrationStatusCommandHandler(
        IIntegrationServiceFactory integrationServiceFactory, 
        IPatientsService patientsService)
    {
        _integrationServiceFactory = integrationServiceFactory;
        _patientsService = patientsService;
    }

    public async Task<Dictionary<string, SubscriptionIntegrationModel[]>> Handle(CheckIntegrationStatusCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByIdAsync(request.PatientId);

        var subscription = patient.CurrentSubscription;
        if (subscription is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Patient does not have any subscription.");
        }

        var integrationService = await _integrationServiceFactory.CreateAsync(patient.User.PracticeId);

        var hint = await integrationService.GetPatientSubscriptionsAsync(patient, IntegrationVendor.Hint);

        var stripe = await integrationService.GetPatientSubscriptionsAsync(patient);

        return new Dictionary<string, SubscriptionIntegrationModel[]>()
        {
            {"Hint", hint},
            {"Stripe", stripe}
        };
    }
}