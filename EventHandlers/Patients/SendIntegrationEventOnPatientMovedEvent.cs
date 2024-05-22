// using System;
// using System.Linq;
// using System.Threading;
// using System.Threading.Tasks;
// using System.Collections.Generic;
// using WildHealth.Application.Events.Patients;
// using WildHealth.Application.Services.AddOns;
// using WildHealth.Domain.Enums.Orders;
// using WildHealth.Domain.Entities.Vitals;
// using WildHealth.Infrastructure.Communication.MessageBus;
// using WildHealth.IntegrationEvents._Base;
// using WildHealth.IntegrationEvents.Patients;
// using WildHealth.IntegrationEvents.Patients.Payloads;
// using WildHealth.Infrastructure.Communication.MessageBus.Provider;
// using IntegrationInputs = WildHealth.IntegrationEvents.Inputs.Models;
// using WildHealth.Application.Services.Inputs;
// using AutoMapper;
// using MediatR;

// namespace WildHealth.Application.EventHandlers.Patients
// {
//     public class SendIntegrationEventOnPatientMovedEvent : INotificationHandler<PatientMovedEvent>
//     {
//         private readonly IEventBus _eventBus;
//         private readonly IMapper _mapper;

//         public SendIntegrationEventOnPatientMovedEvent(IMapper mapper)
//         {
//             _eventBus = EventBusProvider.Get();
//             _mapper = mapper;
//         }

//         public async Task Handle(PatientMovedEvent notification, CancellationToken cancellationToken)
//         {
//             var oldPatient = notification.OldPatient;
//             var newPatient = notification.NewPatient;

//             await _eventBus.Publish(new PatientIntegrationEvent(
//                 payload: new PatientMovedPayload(
//                     oldPatient.User.UserId(),
//                     oldPatient.User.Email
//                 ),
//                 patient: new PatientMetadataModel(newPatient.GetId(), newPatient.User.UserId()),
//                 practice: new PracticeMetadataModel(newPatient.User.PracticeId),
//                 eventDate: newPatient.CreatedAt), cancellationToken);
//         }

//     }
// }