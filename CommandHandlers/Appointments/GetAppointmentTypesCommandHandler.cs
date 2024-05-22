using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Appointments;
using WildHealth.Application.Services.Appointments;
using WildHealth.Application.Services.AppointmentsOptions;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Application.Services.PatientProducts;
using WildHealth.Application.Services.Products;
using WildHealth.Domain.Entities.Products;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Models.Appointments;
using WildHealth.Domain.Models.Patient;
using WildHealth.Domain.Enums;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Commands.Employees;
using WildHealth.Domain.Enums.Payments;
using AutoMapper;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Appointments;

public class GetAppointmentTypesCommandHandler : IRequestHandler<GetAppointmentTypesCommand, AppointmentTypeModel[]>
{
    private static readonly IDictionary<AppointmentWithType, int[]> WithTypeRoleIds = new Dictionary<AppointmentWithType, int[]>()
    {
        {AppointmentWithType.HealthCoach, new [] { Roles.CoachId} },
        {AppointmentWithType.Provider, new [] { Roles.ProviderId} },
        {AppointmentWithType.HealthCoachAndProvider, new [] { Roles.CoachId, Roles.ProviderId} },
    };

    private const string SubscriptionPausedReason = "Sorry, your membership is currently paused. To schedule a visit, please reactivate your membership by reaching out to support@wildhealth.com";
    private const string ConsultCompleted = "Consult is already completed";
    private const string AppointmentScheduled = "Appointment is already scheduled";
    private const int DefaultDaysBoost = 1; //days (24 hours)
    
    private readonly IAppointmentOptionsService _appointmentOptionsService;
    private readonly IPatientProductsService _patientProductsService;
    private readonly IAppointmentsService _appointmentsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IPatientsService _patientsService;
    private readonly IProductsService _productsService;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public GetAppointmentTypesCommandHandler(
        IAppointmentOptionsService appointmentOptionsService,
        IPatientProductsService patientProductsService,
        IAppointmentsService appointmentsService,
        IDateTimeProvider dateTimeProvider,
        IPatientsService patientsService,
        IProductsService productsService,
        IMediator mediator,
        IMapper mapper
        )
    {
        _appointmentOptionsService = appointmentOptionsService;
        _patientProductsService = patientProductsService;
        _appointmentsService = appointmentsService;
        _dateTimeProvider = dateTimeProvider;
        _patientsService = patientsService;
        _productsService = productsService;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<AppointmentTypeModel[]> Handle(GetAppointmentTypesCommand request, CancellationToken cancellationToken)
    {
        var patient = await _patientsService.GetPatientWithAppointments(request.PatientId);

        var patientDomain = PatientDomain.Create(patient);
        
        var mostRecentSubscription = patientDomain.MostRecentSubscription;

        var planId = mostRecentSubscription.PaymentPrice?.PaymentPeriod?.PaymentPlanId;

        var assignedEmployees = patient.GetAssignedEmployees();

        var practiceId = patient.User.PracticeId;
        
        var appointmentTypes = (await _appointmentsService.GetAllTypesAsync(practiceId)).ToArray();

        var appointmentOptions = await _appointmentOptionsService.GetByPatientAsync(patient.GetId());

        var patientProducts = await _patientProductsService.GetActiveAsync(patient.GetId());
        
        var allProducts = await _productsService.GetAsync(practiceId);
        
        var result = new List<AppointmentTypeModel>();

        foreach (var appointmentType in appointmentTypes)
        {
            var type = _mapper.Map<AppointmentTypeModel>(appointmentType);

            // Need to zer out automapping result
            type.Configurations = new List<AppointmentTypeConfigurationModel>();
            
            foreach (var appointmentConfiguration in appointmentType.Configurations.Where(o => o.PaymentPlans.Any(t => t.PaymentPlanId == planId)))
            {
                var configuration = _mapper.Map<AppointmentTypeConfigurationModel>(appointmentConfiguration);

                _mapper.Map(GetEmployeesForWithType(assignedEmployees, configuration.WithType), configuration);

                foreach (var employee in configuration.Employees)
                {
                    var photoUrl = await _mediator.Send(new GetEmployeePhotoUrlCommand(employee.UserId), cancellationToken);

                    employee.PhotoUrl = photoUrl;
                }
                
                var targetOption = appointmentOptions.FirstOrDefault(x =>
                    x.Purpose == type.Purpose 
                    && x.WithType == appointmentConfiguration.WithType
                    && x.NextAppointmentDate > _dateTimeProvider.UtcNow());
                
                configuration.SuggestedEarliestNextDate = targetOption?.NextAppointmentDate;

                configuration.EarliestNextDate = GetEarliestNextDate();

                type.Configurations.Add(configuration);
            }
            
            var unavailabilityReasons = GetUnavailableReason(
                patient: patient, 
                appointmentType: appointmentType,
                appointmentTypes: appointmentTypes, 
                patientProducts: patientProducts, 
                allProducts: allProducts
            );
            
            UpdateAppointmentTypeMessage(type, unavailabilityReasons);
            
            type.UnavailabilityReason = string.Join("; ", unavailabilityReasons.ToArray());
            type.IsCreateAvailable = appointmentType.AvailableForPatients;
            type.IsExcluded = IsTypeExcluded(type);


            result.Add(type);
        }

        return result.Where(x => x.Configurations.Any()).OrderBy(x=> x.Purpose).ToArray();
    }

    #region Type availablility logic

    private void UpdateAppointmentTypeMessage(AppointmentTypeModel appointmentType, List<string> unavailabilityReasons)
    {
        if (unavailabilityReasons.Any())
        {
            appointmentType.DashboardMessageTitle = "You cannot schedule an appointment yet.";
            appointmentType.DashboardMessageDescription = GetUnavailableDashboardMessage(unavailabilityReasons);
        }
        else if (appointmentType.Purpose is AppointmentPurpose.FollowUp 
                 && appointmentType.Configurations.All(x => x.SuggestedEarliestNextDate.HasValue && x.EarliestNextDate.HasValue))
        {
            var suggestedEarliestNextDate = appointmentType
                .Configurations
                .Where(x => x.SuggestedEarliestNextDate.HasValue)
                .OrderBy(x => x.SuggestedEarliestNextDate)
                .Select(x => x.SuggestedEarliestNextDate)
                .FirstOrDefault();

            if (suggestedEarliestNextDate.HasValue)
            {
                var dateString = suggestedEarliestNextDate.Value.ToString("MMM d, yyyy");
                appointmentType.DashboardMessageTitle = $"Your recommended follow-up appointment is not until {dateString}.";
                appointmentType.DashboardMessageDescription = "However, you can schedule an appointment sooner below.";
            }
        }
    }

    private string GetUnavailableDashboardMessage(IEnumerable<string> unavailabilityReasons)
    {
        return unavailabilityReasons.First();
    }
    
    private bool IsTypeExcluded(AppointmentTypeModel typeModel)
    {
        bool IsExcluded(AppointmentPurpose purpose, string reason)
        {
            return typeModel.Purpose == purpose && typeModel.UnavailabilityReason.Contains(reason);
        }

        if (IsExcluded(AppointmentPurpose.Consult, ConsultCompleted))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Detects reason why target appointment types is unavailable for a patient.
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="appointmentType"></param>
    /// <param name="appointmentTypes"></param>
    /// <param name="patientProducts"></param>
    /// <param name="allProducts"></param>
    /// <returns></returns>
    private List<string> GetUnavailableReason(
        Patient patient, 
        AppointmentType appointmentType, 
        AppointmentType[] appointmentTypes,
        PatientProduct[] patientProducts,
        Product[] allProducts)
    {
        var reasons = new List<string>();

        var patientDomain = PatientDomain.Create(patient);

        var mostRecentSubscription = patientDomain.MostRecentSubscription;

        var typeName = appointmentType.Purpose.ToName();

        if (mostRecentSubscription is not null && mostRecentSubscription.GetStatus() == SubscriptionStatus.Paused)
        {
            reasons.Add(SubscriptionPausedReason);
        }
        
        // Checks products
        if (appointmentType.RequiredProductType.HasValue)
        {
            var product = allProducts.FirstOrDefault(x => x.Type == appointmentType.RequiredProductType);

            if (product is not null && !product.CanBuyProduct())
            {
                var patientProduct = patientProducts.FirstOrDefault(x => x.ProductType == product.Type);
                if (patientProduct is null || !patientProduct.CanUseProduct())
                {
                    reasons.Add($"{typeName} unavailable based on subscription type.");
                }
            }
        }
        
        // Check employee assignment
        var assignedIssues = EmployeeAssigned(patient, appointmentType);
        if (!string.IsNullOrEmpty(assignedIssues))
        {
            reasons.Add(assignedIssues);
        }

        // Check lab and dna statues
        if (appointmentType.RequiredDnaStatus.HasValue && appointmentType.RequiredDnaStatus != patient.DnaStatus)
        {
            reasons.Add("You must complete your lab work and DNA kit before you are able to schedule with your Provider");
        }

        if (appointmentType.RequireLabResults && !patientDomain.AreLabsCompletedIfOrdered)
        {
            reasons.Add("You must complete your lab work and DNA kit before you are able to schedule with your Provider");
        }

        // Check required previous appointments 
        var requiredAppointmentsCompleted = RequiredAppointmentsCompleted(
            patient: patient,
            appointments: patient.Appointments.ToArray(),
            targetType: appointmentType,
            appointmentTypes: appointmentTypes);
        
        if (!string.IsNullOrEmpty(requiredAppointmentsCompleted))
        {
            reasons.Add(requiredAppointmentsCompleted);
        }

        // Check if consult is already completed
        // A patient should only schedule a single consult, every subsequent scheduling should be a follow up
        var isConsultCompleted = IsAppointmentCompleted(patient.Appointments.ToArray(), appointmentType, appointmentType.Purpose);
        if (isConsultCompleted)
        {
            reasons.Add(ConsultCompleted);
        }

        // If appointment type is scheduled in the future, that should be unavailable for additional scheduling
        var isTypeAlreadyScheduled = IsAppointmentAlreadyScheduled(patient.Appointments.ToArray(), appointmentType);
        if (!reasons.Any() && isTypeAlreadyScheduled)
        {
            reasons.Add(AppointmentScheduled);
        }

        return reasons;
    }

    private bool IsAppointmentCompleted(
        Appointment[] appointments,
        AppointmentType appointmentType,
        AppointmentPurpose purpose)
    {
        // This is temporary solution, as new appointment type released after some patients already had
        // a medical consult appointment and those patients should not have particular appointment type
        // https://wildhealth.atlassian.net/jira/software/c/projects/CLAR/boards/10?selectedIssue=CLAR-7227
        if (appointmentType.Type == AppointmentTypes.PhysicianVisit || appointmentType.Type == AppointmentTypes.PhysicianVisitDnaReview)
        {
            var isImcCompleted = appointments.Any(x =>
                x.ConfigurationId == 20 && // Initial medical consult 
                AppointmentDomain.Create(x).IsCompleted(_dateTimeProvider.UtcNow())
            );
            
            if (isImcCompleted)
            {
                return true;
            }
        }
        
        return purpose == AppointmentPurpose.Consult &&
               appointments.Any(x =>
                   appointmentType.Configurations.Any(t => t.Id == x.ConfigurationId)
                   && AppointmentDomain.Create(x).IsCompleted(_dateTimeProvider.UtcNow()));
    }

    private bool IsAppointmentAlreadyScheduled(
        Appointment[] appointments,
        AppointmentType appointmentType)
    {
        // This is temporary solution, as new appointment type released after some patients already had
        // a medical consult appointment and those patients should not have particular appointment type
        // https://wildhealth.atlassian.net/jira/software/c/projects/CLAR/boards/10?selectedIssue=CLAR-7227
        if (appointmentType.Type == AppointmentTypes.PhysicianVisit || appointmentType.Type == AppointmentTypes.PhysicianVisitDnaReview)
        {
            var isImcScheduled = appointments.Any(x =>
                x.ConfigurationId == 20 && // Initial medical consult 
                AppointmentDomain.Create(x).IsCompleted(_dateTimeProvider.UtcNow())
            );
            
            if (isImcScheduled)
            {
                return true;
            }
        }
        
        return appointments.Any(x =>
            appointmentType.Configurations.Any(k => k.Id == x.ConfigurationId) &&
            AppointmentDomain.Create(x).IsScheduled(_dateTimeProvider.UtcNow())
        );
    }

    private string? RequiredAppointmentsCompleted(
        Patient patient,
        Appointment[] appointments,
        AppointmentType targetType,
        AppointmentType[] appointmentTypes)
    {
        if (!targetType.RequiredTypeIds.Any())
        {
            return null;
        }

        var recentSubscription = PatientDomain.Create(patient).MostRecentSubscription;

        var planId = recentSubscription?.PaymentPrice.PaymentPeriod.PaymentPlanId;
        
        foreach (var id in targetType.RequiredTypeIds)
        {
            var requiredType = appointmentTypes.FirstOrDefault(x => x.Id == id);

            if (requiredType is null || !requiredType.Configurations.Any(x => x.PaymentPlans.Any(t => t.PaymentPlanId == planId)))
            {
                continue;
            }
            
            if (!IsAppointmentCompleted(appointments, requiredType, requiredType.Purpose))
            {
                return $"The {requiredType.Name} should be completed";
            }
        }

        return null;
    }

    private Employee[] GetEmployeesForWithType(Employee[] employees, AppointmentWithType withType)
    {
        return WithTypeRoleIds.ContainsKey(withType) ? 
            employees.Where(o => WithTypeRoleIds[withType].Contains(o.RoleId)).ToArray() :
            throw new AppException(HttpStatusCode.NotFound, $"Unrecognized Appointment type = {withType} when trying to evaluate staff");
    }

    #endregion

    #region Earliest Date of Appointment logic

    private DateTime GetBoostDate(DateTime dateTime)
    {
        var result = dateTime;
        var remainingDays = DefaultDaysBoost;

        bool IsWeekend(DayOfWeek day) => day is DayOfWeek.Saturday or DayOfWeek.Sunday;

        if (IsWeekend(dateTime.DayOfWeek))
        {
            var addDays = dateTime.DayOfWeek == DayOfWeek.Sunday ? 2 : 1;

            return dateTime.AddDays(addDays + DefaultDaysBoost).Date;
        }
        
        while (remainingDays > 0)
        {
            result = result.AddDays(1);
            if (!IsWeekend(result.DayOfWeek))
            {
                remainingDays--;
            }
        }

        return result;
    }

    private DateTime ApplyTimeBoost(DateTime? earliestDate = null)
    {
        var defaultBoostTime = GetBoostDate(_dateTimeProvider.UtcNow());
        
        if (earliestDate is null || defaultBoostTime > earliestDate)
        {
            return defaultBoostTime;
        }

        return earliestDate.Value;
    }
    
    private DateTime? GetEarliestNextDate()
    {
        return ApplyTimeBoost();
    }

    #endregion

    #region Patient Assigned

    private string? EmployeeAssigned(Patient patient, AppointmentType type)
    {
        var patientDomain = PatientDomain.Create(patient);
        
        var withType = type.Configurations.First().WithType;
        
        string? CheckHealthCoachAndProvider()
        {
            if (!patientDomain.IsAssignedHealthCoach && !patientDomain.IsAssignedProvider)
            {
                return "Health coach and Provider are not yet assigned";
            }

            if (!patientDomain.IsAssignedHealthCoach)
            {
                return "As soon as your Health Coach is assigned you will be able to schedule an appointment.";
            }

            if (!patientDomain.IsAssignedProvider)
            {
                if (type.SelectEmployee)
                {
                    return null;
                }
                
                return "Provider is not yet assigned";
            }

            return null;
        }

        return withType switch
        {
            AppointmentWithType.HealthCoach => patientDomain.IsAssignedHealthCoach
                ? null
                : "Health coach is not yet assigned",
            AppointmentWithType.Provider => patientDomain.IsAssignedProvider
                ? null
                : "Provider is not yet assigned",
            AppointmentWithType.HealthCoachAndProvider => CheckHealthCoachAndProvider(),
            _ => throw new ArgumentOutOfRangeException(nameof(withType), withType, null)
        };
    }

    #endregion
}