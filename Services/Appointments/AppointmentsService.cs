using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using WildHealth.Application.Services.AppointmentsOptions;
using WildHealth.Application.Services.Patients;
using WildHealth.Common.Models.Appointments;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Utils.DateTimes;
using WildHealth.Application.Services.Orders.Lab;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Application.Services.Employees;
using WildHealth.Shared.DistributedCache.Services;
using WildHealth.Application.Extensions.Query;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Application.Utils.AppointmentTag;
using WildHealth.Domain.Models.Appointments;
using RoleConstants = WildHealth.Domain.Constants.Roles;
using AutoMapper;
using WildHealth.Application.Services.FeatureFlags;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Exceptions;

namespace WildHealth.Application.Services.Appointments
{
    public class AppointmentsService : IAppointmentsService
    {
        private readonly IGeneralRepository<AppointmentType> _appointmentTypeRepository;
        private readonly IGeneralRepository<Appointment> _appointmentsRepository;
        private readonly IGeneralRepository<AppointmentInsuranceType> _appointmentInsuranceTypeRepository;
        private readonly IGeneralRepository<AppointmentSignOff> _appointmentSignOffRepository;
        private readonly IAppointmentOptionsService _appointmentOptionsService;
        private readonly IFeatureFlagsService _featureFlagsService;
        private readonly IPatientsService _patientsService;
        private readonly ILabOrdersService _labOrdersService;
        private readonly IEmployeeService _employeesService;
        private readonly IMapper _mapper;
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly IWildHealthSpecificCacheService<AppointmentsService, AppointmentType[]> _wildHealthSpecificCacheAppointmentTypesService;
        private readonly IAppointmentTagsMapperHelper _appointmentTagsMapperHelper;

        public AppointmentsService(
            IGeneralRepository<AppointmentType> appointmentTypeRepository,
            IGeneralRepository<Appointment> appointmentsRepository,
            IGeneralRepository<AppointmentInsuranceType> appointmentInsuranceTypeRepository,
            IGeneralRepository<AppointmentSignOff> appointmentSignOffRepository,
            IAppointmentOptionsService appointmentOptionsService,
            IFeatureFlagsService featureFlagsService,
            IPatientsService patientsService,
            ILabOrdersService labOrdersService,
            IEmployeeService employeesService,
            IMapper mapper,
            IDateTimeProvider dateTimeProvider,
            IWildHealthSpecificCacheService<AppointmentsService, AppointmentType[]> wildHealthSpecificCacheAppointmentTypesService,
            IAppointmentTagsMapperHelper appointmentTagsMapperHelper)
        {
            _appointmentOptionsService = appointmentOptionsService;
            _appointmentTypeRepository = appointmentTypeRepository;
            _appointmentInsuranceTypeRepository = appointmentInsuranceTypeRepository;
            _appointmentsRepository = appointmentsRepository;
            _appointmentSignOffRepository = appointmentSignOffRepository;
            _featureFlagsService = featureFlagsService;
            _patientsService = patientsService;
            _labOrdersService = labOrdersService;
            _employeesService = employeesService;
            _mapper = mapper;
            _dateTimeProvider = dateTimeProvider;
            _wildHealthSpecificCacheAppointmentTypesService = wildHealthSpecificCacheAppointmentTypesService;
            _appointmentTagsMapperHelper = appointmentTagsMapperHelper;
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Appointment> GetByIdAsync(int id)
        {
            var appointment = await _appointmentsRepository
                .All()
                .ById(id)
                .IncludePatient()
                .IncludeEmployee()
                .IncludeConfigurations()
                .IncludeIntegrations<Appointment, AppointmentIntegration>()
                .IncludeLocation()
                .FirstOrDefaultAsync();

            if (appointment is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Appointment does not exist.", exceptionParam);
            }

            return appointment;
        }

        public async Task<Appointment[]> GetByIdsAsync(int[] ids)
        {
            return await _appointmentsRepository
                .All()
                .Where(x => ids.Any(id => id == x.Id!.Value))
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetBySchedulerSystemIdAsync(string)"/>
        /// </summary>
        /// <param name="schedulerSystemId"></param>
        /// <returns></returns>
        public async Task<Appointment?> GetBySchedulerSystemIdAsync(string schedulerSystemId)
        {
            return await _appointmentsRepository
                .Get(x => x.SchedulerSystemId == schedulerSystemId)
                .IncludePatient()
                .IncludeEmployee()
                .IncludeLocation()
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetPatientAppointmentsAsync(int, AppointmentStatus, DateTime?, DateTime?)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="status"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public Task<IEnumerable<Appointment>> GetPatientAppointmentsAsync(int patientId, AppointmentStatus status = AppointmentStatus.All, DateTime? startDate = null, DateTime? endDate = null)
        {
            return GetPatientsAppointmentsAsync(new[] { patientId }, status, startDate, endDate);
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetEmployeeAppointmentsAsync(int, AppointmentStatus, DateTime?, DateTime?, bool)"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="status"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="onlyActive"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Appointment>> GetEmployeeAppointmentsAsync(int employeeId, AppointmentStatus status = AppointmentStatus.All, DateTime? startDate = null, DateTime? endDate = null, bool onlyActive = false)
        {
            var result = await _appointmentsRepository
                .All()
                .ByEmployeeId(employeeId)
                .ByStatus(status)
                .OnlyActive(onlyActive)
                .ByDateRange(startDate, endDate)
                .IncludePatient()
                .IncludeEmployee()
                .IncludeLocation()
                .IncludeNote()
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IAppointmentsService.CreateAppointmentAsync"/>
        /// </summary>
        /// <param name="appointment"></param>
        /// <returns></returns>
        public async Task<Appointment> CreateAppointmentAsync(Appointment appointment)
        {
            await _appointmentsRepository.AddAsync(appointment);
            await _appointmentsRepository.SaveAsync();

            return appointment;
        }

        /// <summary>
        /// <see cref="IAppointmentsService.EditAppointmentAsync"/>
        /// </summary>
        /// <param name="appointment"></param>
        /// <returns></returns>
        public async Task<Appointment> EditAppointmentAsync(Appointment appointment)
        {
            _appointmentsRepository.Edit(appointment);
            await _appointmentsRepository.SaveAsync();

            return appointment;
        }

        /// <summary>
        /// <see cref="IAppointmentsService.AssertTimeAvailableAsync"/>
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public async Task<bool> AssertTimeAvailableAsync(DateTime from, DateTime to, int employeeId)
        {
            var existingAppointments = await GetEmployeeAppointmentsAsync(
                employeeId: employeeId,
                status: AppointmentStatus.All,
                startDate: from,
                endDate: to);

            return !existingAppointments.Any(x =>
                x.Status == AppointmentStatus.Submitted ||
                x.Status == AppointmentStatus.Pending);
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetAvailableTypesAsync"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AppointmentTypeModel>> GetAvailableTypesAsync(int patientId)
        {
            var specification = PatientSpecifications.PatientWithSubscriptionAndIntegrations;
            
            var patient = await _patientsService.GetByIdAsync(patientId, specification);

            var practiceId = patient.User.PracticeId;

            var availableAppointmentTypes = (await GetAllTypesAsync(practiceId))
                .Where(x => x.AvailableForPatients)
                .ToArray();

            bool IsPracticeTypes() => availableAppointmentTypes.Any(x => x.PracticeId == practiceId);
            if (IsPracticeTypes())
            {
                availableAppointmentTypes = FilterAppointmentTypesByPaymentPlan(availableAppointmentTypes, patient);
                
                availableAppointmentTypes = await FilterAppointmentTypesByEmployeeAssignment(availableAppointmentTypes, patientId);
                
                availableAppointmentTypes = await FilterAppointmentTypesByEmployeeAssignment(availableAppointmentTypes, patientId);

                availableAppointmentTypes = await FilterAppointmentTypesByCompletedAppointments(availableAppointmentTypes, patientId);

                availableAppointmentTypes = availableAppointmentTypes.Where(x => !x.RequiredDnaStatus.HasValue || x.RequiredDnaStatus.Value == patient.DnaStatus).ToArray();

                availableAppointmentTypes = await FilterAppointmentTypesByLabOrders(availableAppointmentTypes, patientId);
            }

            var appointmentTypes = _mapper.Map<AppointmentTypeModel[]>(availableAppointmentTypes);

            appointmentTypes = await ApplyOptionsAsync(patientId, appointmentTypes);

            return appointmentTypes
                .OrderBy(x => x.Purpose)
                .ToArray();
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetAllTypesAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AppointmentType>> GetAllTypesAsync(int practiceId)
        {
            var allAppointmentTypes = await _wildHealthSpecificCacheAppointmentTypesService
                .GetAsync($"{practiceId.GetHashCode()}",
                    async () => await _appointmentTypeRepository
                        .All()
                        .NotDeleted()
                        .IncludeConfigurations()
                        .AsNoTracking()
                        .ToArrayAsync());

            bool IsAppointmentTypesExist() => allAppointmentTypes
                .Any(x => x.PracticeId == practiceId);

            bool PracticeTypes(AppointmentType x) => x.PracticeId == practiceId;
            bool DefaultTypes(AppointmentType x) => x.PracticeId == null;

            // A temporary solution due to the impossibility of using the flag feature at the database level
            if (!_featureFlagsService.GetFeatureFlag(Common.Constants.FeatureFlags.SplitImc))
            {
                var splitImcAppTypeIds = new[]
                {
                    70,
                    80
                };
                
                allAppointmentTypes = allAppointmentTypes
                    .Where(x => !splitImcAppTypeIds.Contains(x.GetId()))
                    .ToArray();
            }
            else
            {
                allAppointmentTypes = allAppointmentTypes
                    .Where(x => x.Id != 20)
                    .ToArray();
            }

            return allAppointmentTypes
                .Where(IsAppointmentTypesExist() ? (Func<AppointmentType, bool>)PracticeTypes : DefaultTypes)
                .ToArray();
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetTypeByConfigurationIdAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="configurationId"></param>
        /// <returns></returns>
        public async Task<(AppointmentType, AppointmentTypeConfiguration)> GetTypeByConfigurationIdAsync(
            int practiceId, 
            int configurationId)
        {
            var allTypes = await GetAllTypesAsync(practiceId);

            var type = allTypes.FirstOrDefault(x => x.Configurations.Any(k => k.Id == configurationId));

            if (type is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Corresponding appointment type does not exist");
            }

            var configuration = type.Configurations.FirstOrDefault(x => x.Id == configurationId);

            if (configuration is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Corresponding appointment type does not exist");
            }

            return (type, configuration);
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetAppointmentInsuranceTypeByAppointmentPurpose"/>
        /// </summary>
        /// <param name="appointmentPurpose"></param>
        /// <returns></returns>
        public Task<AppointmentInsuranceType?> GetAppointmentInsuranceTypeByAppointmentPurpose(AppointmentPurpose appointmentPurpose)
        {
            var result = _appointmentInsuranceTypeRepository
                .Get(x => x.AppointmentPurpose == appointmentPurpose)
                .FirstOrDefaultAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetByIntegrationIdAsync"/>
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <param name="purpose"></param>
        /// <returns></returns>
        public async Task<Appointment?> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor, string purpose)
        {
            var result = await _appointmentsRepository
                .All()
                .ByIntegrationId<Appointment, AppointmentIntegration>(integrationId, vendor, purpose)
                .IncludePatientProduct()
                .FirstOrDefaultAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetEmployeeAppointmentsByIdsAsync"/>
        /// </summary>
        /// <param name="appointmentIds"></param>
        /// <returns></returns>
        public async Task<EmployeeAppointmentModel[]> GetEmployeeAppointmentsByIdsAsync(int[] appointmentIds)
        {
            var appointments = await _appointmentsRepository
                .All()
                .ByIds(appointmentIds)
                .IncludePatient()
                .IncludePatientWithLabs()
                .IncludePatientSubscription()
                .IncludeEmployee()
                .AsNoTracking()
                .ToListAsync();

            return _appointmentTagsMapperHelper.MapAppointmentWithTags(appointments).ToArray();
        }

        /// <summary>
        /// <see cref="IAppointmentsService.GetSequenceNumbers"/>
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public async Task<IEnumerable<AppointmentsSequenceInfoModel>> GetSequenceNumbers(AppointmentSequenceNumbersModel[] models)
        {
            var patientIds = models.Select(x => x.PatientId).ToArray();
            
            var allAppointments = await GetPatientsAppointmentsAsync(patientIds);
            
            var result = models.Select(o =>
            {
                var appointments = allAppointments.Where(x => x.PatientId == o.PatientId);

                var appointment = appointments.FirstOrDefault(x => x.Id == o.Id);

                if (appointment is null)
                {
                    return null;
                }

                return AppointmentDomain.Create(appointment).SequenceInfo();
            });

            return result.Where(o => o != null)!;
        }

        /// <summary>
        /// <see cref="IAppointmentsService.SignOffAppointment"/>
        /// </summary>
        /// <param name="appointmentId"></param>
        /// <param name="appointmentSignOffType"></param>
        /// <returns></returns>
        public async Task<Appointment> SignOffAppointment(int appointmentId, AppointmentSignOffType appointmentSignOffType)
        {
            var appointmentSignOff = await _appointmentSignOffRepository
                .All()
                .Where(o => o.AppointmentId == appointmentId)
                .FirstOrDefaultAsync();

            if (appointmentSignOff is not null)
            {
                appointmentSignOff.SignOffType = appointmentSignOffType;
                
                _appointmentSignOffRepository.Edit(appointmentSignOff);

                await _appointmentSignOffRepository.SaveAsync();

                return await GetByIdAsync(appointmentId);
            }

            appointmentSignOff = new AppointmentSignOff(
                appointmentId: appointmentId,
                signOffType: appointmentSignOffType);

            await _appointmentSignOffRepository.AddAsync(appointmentSignOff);

            await _appointmentSignOffRepository.SaveAsync();

            return await GetByIdAsync(appointmentId);
        }

        public async Task<Appointment> GetByMeetingSystemIdAsync(long meetingSystemId)
        {
            var appointment = await _appointmentsRepository
                .All()
                .Where(o => o.MeetingSystemId == meetingSystemId)
                .FindAsync();

            return appointment;
        }

        #region private
        
        private async Task<AppointmentTypeModel[]> ApplyOptionsAsync(int patientId,AppointmentTypeModel[] types)
        {
            var appointmentOptions = (await _appointmentOptionsService.GetByPatientAsync(patientId))
                .Where(x => x.NextAppointmentDate.Date >= DateTime.UtcNow.Date)
                .ToArray();

            foreach (var type in types)
            {
                foreach (var configuration in type.Configurations)
                {
                    var option = appointmentOptions
                        .FirstOrDefault(x => 
                            x.Purpose == type.Purpose &&
                            x.WithType == configuration.WithType &&
                            x.NextAppointmentDate > _dateTimeProvider.UtcNow());

                    if (option is null)
                    {
                        continue;
                    }

                    configuration.SuggestedEarliestNextDate = option.NextAppointmentDate;
                }
            }

            return types;
        }

        private async Task<AppointmentType[]> FilterAppointmentTypesByCompletedAppointments(AppointmentType[] appointmentTypes, int patientId)
        {
            var pastAppointments = await GetPatientAppointmentsAsync(
                patientId: patientId,
                endDate: _dateTimeProvider.Now()
            );

            var submittedAppointments = pastAppointments
                .Where(x => x.Status == AppointmentStatus.Submitted)
                .ToArray();

            var result = new List<AppointmentType>();

            foreach (var appointmentType in appointmentTypes)
            {
                if (appointmentType.Purpose == AppointmentPurpose.Consult && 
                    submittedAppointments.Any(x => x.Purpose == appointmentType.Purpose && appointmentType.Configurations.Any(k => k.WithType == x.WithType)))
                {
                    continue;
                }

                if (!appointmentType.RequiredTypeIds.Any())
                {
                    result.Add(appointmentType);
                    continue;
                }

                var requiredTypes = appointmentTypes.Where(x => appointmentType.RequiredTypeIds.Contains(x.GetId()));

                var requiredAppointmentsCompleted = requiredTypes.All(x => submittedAppointments.Any(a => a.Purpose == x.Purpose && x.Configurations.Any(k => k.WithType == a.WithType)));

                if (requiredAppointmentsCompleted)
                {
                    result.Add(appointmentType);
                }
            }

            return result.ToArray();
        }

        private async Task<AppointmentType[]> FilterAppointmentTypesByLabOrders(IEnumerable<AppointmentType> appointmentTypes, int patientId)
        {
            var labOrders = await _labOrdersService.GetPatientOrdersAsync(patientId);

            return appointmentTypes.Where(x => !x.RequireLabResults || labOrders.Any(o => o.Status == OrderStatus.Completed) || !labOrders.Any()).ToArray();
        }

        private AppointmentType[] FilterAppointmentTypesByPaymentPlan(AppointmentType[] appointmentTypes, Patient patient)
        {
            var currentSubscription = patient.CurrentSubscription;

            if (currentSubscription is null)
            {
                return Array.Empty<AppointmentType>();
            }

            var paymentPlanId = currentSubscription.PaymentPrice.PaymentPeriod.PaymentPlanId;
            return appointmentTypes
                .Where(x => x.Configurations.Any(k => k.PaymentPlans.Any(t => t.PaymentPlanId == paymentPlanId)))
                .ToArray();
        }
        
        private async Task<AppointmentType[]> FilterAppointmentTypesByEmployeeAssignment(AppointmentType[] appointmentTypes, int patientId)
        {
            var employees = await _employeesService.GetAssignedToAsync(patientId);

            return appointmentTypes.Where(x => x.Configurations.Any(k => k.WithType == AppointmentWithType.HealthCoachAndProvider)
                ? employees.Any(e => e.RoleId == RoleConstants.ProviderId) && employees.Any(e => e.RoleId == RoleConstants.CoachId)
                : employees.Any(e => e.RoleId == RoleConstants.CoachId)
            ).ToArray();
        }
        
        private async Task<IEnumerable<Appointment>> GetPatientsAppointmentsAsync(int[] patientIds, AppointmentStatus status = AppointmentStatus.All, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = await _appointmentsRepository
                .All()
                .Where(x => x.PatientId != null && patientIds.Contains(x.PatientId.Value))
                .ByStatus(status)
                .ByDateRange(startDate, endDate)
                .IncludePatient()
                .IncludeEmployee()
                .IncludeNote()
                .IncludePatientProduct()
                .IncludeLocation()
                .ToListAsync();

            return result;
        }

        #endregion
    }
}
