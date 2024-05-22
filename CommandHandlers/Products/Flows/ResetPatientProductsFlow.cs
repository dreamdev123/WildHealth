using System;
using System.Collections.Generic;
using System.Linq;
using WildHealth.Application.Functional.Flow;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Models;

namespace WildHealth.Application.CommandHandlers.Products.Flows;

public class ResetPatientProductsFlow : IMaterialisableFlow
{
    private readonly Subscription _subscription;
    private readonly PatientProduct[] _builtInPatientProducts;
    private readonly Log _log;


    private static IList<AppointmentWithType> _providerWithTypes = new List<AppointmentWithType>()
    {
        AppointmentWithType.Provider,
        AppointmentWithType.HealthCoachAndProvider
    };
    
    public ResetPatientProductsFlow(Subscription subscription, PatientProduct[] builtInPatientProducts, Log log)
    {
        _subscription = subscription;
        _builtInPatientProducts = builtInPatientProducts;
        _log = log;
    }

    public MaterialisableFlowResult Execute()
    {
        var builtInPhysicianPatientProducts = GetBuiltInPhysicianPatientProducts();

        ////////////////////////////////////////////////////////////////
        // UnUse all of the builtIns first
        ////////////////////////////////////////////////////////////////
        builtInPhysicianPatientProducts.ForEach(p => p.UnUseProduct());
        //var updatedProducts = builtInPhysicianPatientProducts.Select(x => x.Updated()).ToList();
        _log($"Found {builtInPhysicianPatientProducts.Count} built in physician patient products");
        
        ////////////////////////////////////////////////////////////////
        /// Now use all of the products on the visits
        ////////////////////////////////////////////////////////////////
        var physicianVisitAppointments = GetUnassociatedOrBuiltInSubscriptionVisits();
        _log($"Found {physicianVisitAppointments.Count} physician visit appointments");
        var updatedAppointments = physicianVisitAppointments.Select(appointment =>
        {
            appointment.ProductId = null;

            _log($"Checking [AppointmentId] = {appointment.GetId()}");

            var availableVisit = builtInPhysicianPatientProducts.FirstOrDefault(x => x.CanUseProduct());
            if (availableVisit is null)
            {
                _log($"Unable to find a patient product for [AppointmentId] = {appointment.GetId()}");
                return (appointment.Updated(), EntityAction.None.Instance);
            }

            _log($"Found [PatientProductId] = {availableVisit.GetId()}");

            availableVisit.UseProduct("appointment migration", appointment.CreatedAt);

            appointment.ProductId = availableVisit.GetId();
            
            _log($"Using [PatientProductId] = {availableVisit.GetId()}, for [Date] = {appointment.CreatedAt}");
            
            return (appointment.Updated(), availableVisit.Updated());
        }).ToList();

        var appointmentProducts = updatedAppointments
            .Select(x => x.Item2)
            .Where(x => x.HasValue)
            .ToList();
        
        var appointmentProductIds = appointmentProducts.Select(x => x.Entity.GetId());
        var unusedProducts = builtInPhysicianPatientProducts
            .Where(x => !appointmentProductIds.Contains(x.GetId()))
            .Select(x => x.Updated())
            .ToList();
        
        var appointments = updatedAppointments.Select(x => x.Item1).ToList();
        
        return appointments.Concat(unusedProducts).Concat(appointmentProducts).ToFlowResult();
    }

    private List<PatientProduct> GetBuiltInPhysicianPatientProducts()
    {
        return _builtInPatientProducts
            .Where(o => o.ProductType == ProductType.PhysicianVisit)
            .ToList();
    }

    private List<Appointment> GetUnassociatedOrBuiltInSubscriptionVisits()
    {
        return (_subscription.Patient.Appointments ?? Array.Empty<Appointment>())
            .Where(x => x.StartDate >= _subscription.StartDate && x.StartDate < _subscription.EndDate)
            .Where(x => _providerWithTypes.Contains(x.WithType))
            .Where(x => x.Status == AppointmentStatus.Submitted)
            .Where(x => !x.IsNoShow)
            .Where(x => x.PatientProduct == null || x.PatientProduct.ProductSubType == ProductSubType.BuiltIn)  // Only get appointments that are not associated with a product or associated with built in product
            .ToList();
    }
}