using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Payments;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Shared.Exceptions;
using MediatR;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Infrastructure.Data.Specifications;

namespace WildHealth.Application.CommandHandlers.Patients;

public class MigrateExistingSubscriptionsCommandHandler : IRequestHandler<MigrateExistingSubscriptionsCommand>
{
    private readonly IPatientProductsService _patientProductsService;
    private readonly IPatientsService _patientsService;
    private readonly IMediator _mediator;

    public MigrateExistingSubscriptionsCommandHandler(
        IPatientProductsService patientProductsService, 
        IPatientsService patientsService, 
        IMediator mediator)
    {
        _patientProductsService = patientProductsService;
        _patientsService = patientsService;
        _mediator = mediator;
    }

    public async Task Handle(MigrateExistingSubscriptionsCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetByIdAsync(request.PatientId, PatientSpecifications.PatientWithEmployerProductSpecification);

        var subscription = patient.CurrentSubscription;
        if (subscription is null)
        {
            throw new AppException(HttpStatusCode.BadRequest, "Patient does not have an active subscription.");
        }

        var currentPatientProducts = await _patientProductsService.GetByPatientIdAndProductTypeAsync(patient.GetId(), ProductType.PhysicianVisit, ProductSubType.BuiltIn);
        if (currentPatientProducts.Any())
        {
            throw new AppException(HttpStatusCode.BadRequest, "Products are already created for this patient.");
        }

        var command = new CreateBuiltInProductsCommand(subscription.GetId());

        await _mediator.Send(command, cancellationToken);
        
        await UsePatientProducts(subscription);
    }
    
    private Appointment[] GetSubscriptionVisits(Subscription subscription)
    {
        return subscription.Patient.Appointments
            .Where(x => x.StartDate >= subscription.StartDate && x.StartDate < subscription.EndDate)
            .Where(x => x.WithType == AppointmentWithType.HealthCoachAndProvider)
            .Where(x => AppointmentDomain.Create(x).IsCompleted(DateTime.UtcNow))
            .ToArray();
    }
    
    private async Task UsePatientProducts(Subscription subscription)
    {
        var patientId = subscription.Patient.GetId();
        
        var appointments = GetSubscriptionVisits(subscription);
        
        var patientProducts = await _patientProductsService.GetBuiltInByPatientAsync(patientId);

        foreach (var appointment in appointments)
        {
            var availableVisit = patientProducts
                .FirstOrDefault(x => x.ProductType == ProductType.PhysicianVisit && x.CanUseProduct());

            if (availableVisit is null)
            {
                return;
            }
            
            availableVisit.UseProduct("appointment migration", appointment.CreatedAt);
        }
        
        await _patientProductsService.UpdateAsync(patientProducts.ToArray());
    }
}