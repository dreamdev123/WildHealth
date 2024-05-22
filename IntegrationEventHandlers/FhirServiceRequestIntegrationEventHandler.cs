using System;
using System.Net;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Infrastructure.Communication.MessageBus;
using WildHealth.IntegrationEvents.Fhir;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WildHealth.Application.IntegrationEventHandlers.Extensions;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.IntegrationEvents.Fhir.Payloads;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.IntegrationEventHandlers;

public class FhirServiceRequestIntegrationEventHandler : IEventHandler<FhirServiceRequestIntegrationEvent>
{
    private readonly IMediator _mediator;
    private readonly ILogger<FhirServiceRequestIntegrationEventHandler> _logger;
    private readonly IDnaOrdersService _dnaOrdersService;
    private readonly IGeneralRepository<WildHealth.Domain.Entities.Integrations.Integration> _integrationsRepository;
    private readonly IIntegrationsService _integrationService;

    public FhirServiceRequestIntegrationEventHandler(
        IMediator mediator,
        ILogger<FhirServiceRequestIntegrationEventHandler> logger,
        IDnaOrdersService dnaOrdersService,
        IGeneralRepository<WildHealth.Domain.Entities.Integrations.Integration> integrationsRepository,
        IIntegrationsService integrationService)
    {
        _mediator = mediator;
        _logger = logger;
        _dnaOrdersService = dnaOrdersService;
        _integrationsRepository = integrationsRepository;
        _integrationService = integrationService;
    }

    public async Task Handle(FhirServiceRequestIntegrationEvent @event)
    {
        _logger.LogInformation($"Started processing Fhir service request integration event {@event.Id} with payload type: {@event.PayloadType} - {@event.Payload}");

        try
        {
            switch (@event.PayloadType)
            {
                case nameof(FhirServiceRequestUpdatedPayload):
                    var payload = @event.DeserializePayload<FhirServiceRequestUpdatedPayload>();

                    await payload.Handle(
                        _logger, 
                        _integrationsRepository, 
                        _dnaOrdersService,
                        _integrationService);
                    
                    break;

                default: throw new AppException(HttpStatusCode.NotFound,"Unsupported Fhir service request integration event payload");
            }
        }
        catch (Exception e)
        {
            _logger.LogError($"Failed processing Fhir service request integration event {@event.Id} with payload: {@event.Payload}. {e}");
            throw;
        }
        
        _logger.LogInformation($"Processed Fhir service request integration event {@event.Id} with payload: {@event.Payload}");
    }

}