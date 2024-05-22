using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.Orders;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Orders;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Models.Patient;
using WildHealth.Kit.Clients.Services;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Orders
{
    public class PlaceDnaOrderCommandHandler : IRequestHandler<PlaceDnaOrderCommand>
    {
        private readonly IPatientsService _patientsService;
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IIntegrationsService _integrationsService;
        private readonly IKitService _kitService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public PlaceDnaOrderCommandHandler(
            IPatientsService patientsService,
            IDnaOrdersService dnaOrdersService,
            IIntegrationsService integrationsService,
            IKitService kitService,
            IMediator mediator,
            ILogger<PlaceDnaOrderCommandHandler> logger)
        {
            _patientsService = patientsService;
            _dnaOrdersService = dnaOrdersService;
            _integrationsService = integrationsService;
            _kitService = kitService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task Handle(PlaceDnaOrderCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Placing DNA order for patient with id: {command.PatientId} has been started.");
            
            var order = await _dnaOrdersService.GetByIdAsync(command.OrderId);
        
            var patient = await _patientsService.GetByIdAsync(command.PatientId);

            var patientDomain = PatientDomain.Create(patient);
            
            if (!patientDomain.IsLinkedWithIntegrationSystem(IntegrationVendor.Kit))
            {
                var kitPatient = await _kitService.CreatePatientAsync(patient);

                if (kitPatient is null)
                {
                    _logger.LogError($"Placing DNA order for patient with id: {command.PatientId} has failed.");
                    throw new AppException(HttpStatusCode.BadRequest, $"Failed to create Kit Patient for Patient Id: {command.PatientId}");
                }
                
                var patientIntegration = new PatientIntegration(
                    patient: patient,
                    purpose: IntegrationPurposes.Patient.ExternalId,
                    vendor: IntegrationVendor.Kit,
                    value: kitPatient.Id);
            
                await _integrationsService.CreateAsync(patientIntegration);
            }

            var kitPatientId = patient.GetIntegrationId(IntegrationVendor.Kit);

            var serviceRequest = await _kitService.CreateServiceRequestAsync(patient, kitPatientId, order.Items);

            if (serviceRequest is null)
            {
                _logger.LogError($"Placing DNA order for patient with id: {command.PatientId} has failed.");
                throw new AppException(HttpStatusCode.BadRequest, $"Failed to create Kit Service Request for Patient Id: {command.PatientId}");
            }

            var orderIntegration = new OrderIntegration(
                order: order,
                purpose: IntegrationPurposes.Order.ExternalId,
                vendor: IntegrationVendor.Kit,
                value: serviceRequest.Id);

            await _integrationsService.CreateAsync(orderIntegration);

            var items = CreateOrderItems(order.Items);

            await _mediator.Send(
                new MarkDnaOrderAsPlacedCommand(
                    order.GetId(), 
                    serviceRequest.Id, 
                    DateTime.UtcNow, 
                    items), 
                cancellationToken);

            _logger.LogInformation($"Placing DNA order for patient with id: {command.PatientId} has been finished.");
        }
        
        #region private
        
        /// <summary>
        /// Creates and returns order items based on command
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private PlaceOrderItemModel[] CreateOrderItems(ICollection<OrderItem> items)
        {
            return items.Select(x => new PlaceOrderItemModel()
                {
                    Id = x.GetId(),
                    Description = x.Description,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    Sku = x.Sku
                }
            ).ToArray();
        }
        
        #endregion
    }
}