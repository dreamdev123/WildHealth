using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Models.Appointments;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Appointments;

public class GetAppointmentSummaryCommandHandler: IRequestHandler<GetAppointmentSummaryCommand,AppointmentSummaryModel>
{
    private readonly int _implicitUnlimitedAmount = 10_000;
    private readonly IAppointmentsService _appointmentsService;
    private readonly IPatientsService _patientsService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetAppointmentSummaryCommandHandler(
        IAppointmentsService appointmentsService,
        IPatientsService patientsService, 
        IDateTimeProvider dateTimeProvider)
    {
        _appointmentsService = appointmentsService;
        _patientsService = patientsService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AppointmentSummaryModel> Handle(GetAppointmentSummaryCommand request, CancellationToken cancellationToken)
    {
        var specification = PatientSpecifications.PatientWithProductSpecification;
        
        var patient = await _patientsService.GetByIdAsync(request.PatientId, specification);

        var allAppointments = await _appointmentsService.GetPatientAppointmentsAsync(request.PatientId);

        var completed = allAppointments
            .Where(x => AppointmentDomain.Create(x).IsCompleted(_dateTimeProvider.UtcNow()))
            .ToArray();
        
        var completedOrPending = allAppointments
            .Where(x => AppointmentDomain.Create(x).IsCompletedOrPending())
            .ToArray();

        var currentSubscription = patient.CurrentSubscription;

        var completedThisSubscription = completed.Where(o =>
                currentSubscription != null &&
                o.StartDate >= currentSubscription.StartDate && o.StartDate <= currentSubscription.EndDate)
            .ToArray();

        var completedProviderMembershipVisits = CompletedProviderMembershipVisits(patient);
        var availableProviderMembershipVisits = AvailableProviderMembershipVisits(patient);
        var completedOrPendingProviderAdditionalVisits = CompletedAdditionalProviderVisits(completedOrPending);
        var availableProviderAdditionalVisits = AvailableAdditionalProviderVisits(patient);

        return new AppointmentSummaryModel
        {
            CompletedCoachAppointments = CompletedCoachAppointments(completedThisSubscription),
            AvailableCoachAppointments = AvailableCoachAppointments(patient),
            CompletedProviderMembershipVisits = completedProviderMembershipVisits,
            AvailableProviderMembershipVisits = completedProviderMembershipVisits + availableProviderMembershipVisits,
            CompletedAdditionalProviderVisits = completedOrPendingProviderAdditionalVisits,
            AvailableAdditionalProviderVisits = completedOrPendingProviderAdditionalVisits + availableProviderAdditionalVisits
        };
    }
    
    #region private

    private int CompletedCoachAppointments(Appointment[] appointments)
    {
        return appointments.Count(x => x.WithType == AppointmentWithType.HealthCoach);
    }
    
    private int? AvailableCoachAppointments(Patient patient)
    {
        var currentSubscriptionUniversalId = patient.CurrentSubscription?.UniversalId;
        
        var patientProducts = patient.PatientProducts.Where(x => 
            x.CanUseProduct() 
            && x.ProductType == ProductType.HealthCoachVisit 
            && x.ProductSubType == ProductSubType.BuiltIn
            && x.SourceId == currentSubscriptionUniversalId);

        return patientProducts.Any(o => !o.IsLimited) ? _implicitUnlimitedAmount : patientProducts.Count();
    }
    
    // private int CompletedProviderMembershipVisits(Appointment[] appointments)
    // {
    //     return appointments.Count(x => 
    //         (x.WithType == AppointmentWithType.Provider || x.WithType == AppointmentWithType.HealthCoachAndProvider)
    //         && x.PatientProduct?.ProductSubType == ProductSubType.BuiltIn
    //         && x.PatientProduct.IsUsed);
    // }
    
    private int CompletedProviderMembershipVisits(Patient patient)
    {
        var currentSubscriptionUniversalId = patient.CurrentSubscription?.UniversalId;
        
        return patient.PatientProducts.Count(x => 
            !x.CanUseProduct() 
            && x.ProductType == ProductType.PhysicianVisit 
            && x.ProductSubType == ProductSubType.BuiltIn
            && x.SourceId == currentSubscriptionUniversalId);
    }
    
    private int AvailableProviderMembershipVisits(Patient patient)
    {
        var currentSubscriptionUniversalId = patient.CurrentSubscription?.UniversalId;
        
        return patient.PatientProducts.Count(x => 
            x.CanUseProduct() 
            && x.ProductType == ProductType.PhysicianVisit 
            && x.ProductSubType == ProductSubType.BuiltIn
            && x.SourceId == currentSubscriptionUniversalId);
    }
    
    private int CompletedAdditionalProviderVisits(Appointment[] appointments)
    {
        return appointments.Count(x =>
            (x.WithType == AppointmentWithType.Provider || x.WithType == AppointmentWithType.HealthCoachAndProvider)
            && x.PatientProduct?.ProductSubType == ProductSubType.Additional
            && x.PatientProduct.IsUsed);
    }
    
    private int AvailableAdditionalProviderVisits(Patient patient)
    {
        return patient.PatientProducts.Count(x => 
            x.CanUseProduct() 
            && x.ProductType == ProductType.PhysicianVisit 
            && x.ProductSubType == ProductSubType.Additional);
    }
    
    #endregion
}