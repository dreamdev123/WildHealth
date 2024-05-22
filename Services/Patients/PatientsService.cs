using System;
using System.Net;
using System.Collections.Generic;
using WildHealth.Domain.Constants;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Models.Patients;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Domain.Entities.Employees;
using WildHealth.Domain.Entities.Notes;
using WildHealth.Domain.Enums.Employees;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Inputs;
using WildHealth.Application.Services.QuestionnaireResults;
using WildHealth.Common.Models.Tags;
using WildHealth.Domain.Entities.Appointments;
using WildHealth.Domain.Entities.Medication;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Entities.Questionnaires;
using WildHealth.Domain.Entities.Supplement;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Shared.Data.Extensions;
using WildHealth.Shared.Data.Helpers;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Domain.Enums.Orders;
using WildHealth.Infrastructure.Data.Queries.CustomSql.Models;
using WildHealth.Infrastructure.Data.Queries.CustomSql;
using WildHealth.Application.Utils.Patients;
using AutoMapper;
using WildHealth.Application.Extensions.Query;
using WildHealth.Common.Constants;
using WildHealth.Domain.Enums.Appointments;
using WildHealth.Domain.Enums.Products;
using WildHealth.Domain.Models.Patient;
using WildHealth.Domain.Models.Timeline;

namespace WildHealth.Application.Services.Patients
{
    public class PatientsService : IPatientsService
    {
        private readonly IGeneralRepository<Patient> _patientsRepository;
        private readonly IGeneralRepository<Employee> _employeeRepository;
        private readonly IGeneralRepository<Subscription> _subscriptionsRepository;
        private readonly IGeneralRepository<PatientTimelineEvent> _timelineRepository;
        private readonly IGeneralRepository<PatientSupplement> _supplementsRepository;
        private readonly IGeneralRepository<PatientMedication> _medicationsRepository;
        private readonly IGeneralRepository<Appointment> _appointmentsRepository;
        private readonly IGeneralRepository<QuestionnaireResult> _questionnaireResultsRepository;
        private readonly IGeneralRepository<Note> _notesRepository;
        private readonly IEmployeeService _employeeService;
        private readonly IInputsService _inputsService;
        private readonly IQuestionnaireResultsService _questionnaireResultsService;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly ICustomSqlDataRunner _customSqlDataRunner;
        private readonly IPatientFilterHelper _filterHelper;
        private readonly IGeneralRepository<PatientEmployee> _patientEmployeeRepository;

        private const int MyPatientsQueryTimeoutSeconds = 360;

        public PatientsService(
            IGeneralRepository<Patient> patientsRepository,
            IGeneralRepository<Note> notesRepository,
            IGeneralRepository<Subscription> subscriptionsRepository,
            IEmployeeService employeeService,
            IInputsService inputsService,
            IQuestionnaireResultsService questionnaireResultsService,
            ILogger<PatientsService> logger, IGeneralRepository<PatientSupplement> supplementsRepository,
            IMapper mapper,
            IGeneralRepository<PatientMedication> medicationsRepository,
            IGeneralRepository<Appointment> appointmentsRepository,
            IGeneralRepository<QuestionnaireResult> questionnaireResultsRepository,
            ICustomSqlDataRunner customSqlDataRunner,
            IPatientFilterHelper filterHelper, 
            IGeneralRepository<Employee> employeeRepository, 
            IGeneralRepository<PatientTimelineEvent> timelineRepository,
            IGeneralRepository<PatientEmployee> patientEmployeeRepository)
        {
            _patientsRepository = patientsRepository;
            _subscriptionsRepository = subscriptionsRepository;
            _notesRepository = notesRepository;
            _employeeService = employeeService;
            _inputsService = inputsService;
            _questionnaireResultsService = questionnaireResultsService;
            _logger = logger;
            _mapper = mapper;
            _supplementsRepository = supplementsRepository;
            _medicationsRepository = medicationsRepository;
            _appointmentsRepository = appointmentsRepository;
            _questionnaireResultsRepository = questionnaireResultsRepository;
            _customSqlDataRunner = customSqlDataRunner;
            _filterHelper = filterHelper;
            _employeeRepository = employeeRepository;
            _timelineRepository = timelineRepository;
            _patientEmployeeRepository = patientEmployeeRepository;
        }

        ///<inheritdoc/>
        public async Task<(IEnumerable<Patient> patients, int totalCount)> SelectPatientsAsync(int practiceId,
            int[] locationIds,
            int? assignedTo = null,
            PatientOnBoardingStatus? onBoardingStatus = null,
            PatientJourneyStatus[]? journeyStatuses = null,
            bool? patientFellowship = null,
            int[]? periodsIds = null,
            int[]? coachesIds = null,
            int[]? providersIds = null,
            string? searchQuery = null,
            string? sortingSource = null,
            string? sortingDirection = null,
            int? skip = null,
            int? take = null,
            bool ignoreWithoutCompletedRegistration = true,
            OrderType[]? orderTypes = null,
            string[]? stateAbbreviations = null,
            bool? isPlanActive = null)
        {
            var queryData = _patientsRepository
                .All()
                .Active()
                .IncludeUser()
                .WithCompletedUserRegistration(ignoreWithoutCompletedRegistration)
                .IncludePaymentPlans()
                .IncludeEmployee()
                .IncludeAppointment()
                .IncludeLocation()
                .IncludeOrders()
                .IncludeConversations()
                .IncludeIntegrations()
                .RelatedToPractice(practiceId)
                .RelatedToLocations(locationIds)
                .AssignedToEmployee(assignedTo)
                .ByJourneyStatus(journeyStatuses)
                .ByPaymentPlans(periodsIds)
                .ByEmployees(coachesIds)
                .ByEmployees(providersIds)
                .ByFellowship(patientFellowship)
                .ByOrderTypes(orderTypes)
                .ByOnBoardingStatus(onBoardingStatus)
                .BySearchQuery(searchQuery)
                .ByPlanStatus(isPlanActive)
                .Sort(sortingSource, sortingDirection)
                .ByStateAbbreviations(stateAbbreviations)
                .AsNoTracking();

            var totalCount = await queryData.CountAsync();
            var patients = await queryData.Pagination(skip, take).ToArrayAsync();
            
            _logger.LogInformation($"SelectPatients: {totalCount} results.");

            return (patients, totalCount);
        }

        public async Task<int?[]> GetAllPatientIds()
        {
            var result = await _patientsRepository
                .All()
                .Select(x => x.Id)
                .ToArrayAsync();

            return result;
        }

        /// <summary>
        /// <see cref="IPatientsService.GetByIdAsync(int, ISpecification{Patient})"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="specification"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Patient> GetByIdAsync(int id, ISpecification<Patient> specification)
        {
            var patient = await _patientsRepository
                .All()
                .ById(id)
                .ApplySpecification(specification)
                .FirstOrDefaultAsync();

            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist", exceptionParam);
            }

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.GetForAvailability(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Patient> GetForAvailability(int id)
        {
            var patient = await _patientsRepository
                .All()
                .ById(id)
                .ApplySpecification(PatientSpecifications.CheckPatientAppointmentAvailabilitySpecification)
                .FirstOrDefaultAsync();

            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist", exceptionParam);
            }

            patient.Notes = await _notesRepository.All().Where(o => o.PatientId == id).ToArrayAsync();
            patient.Appointments = await _appointmentsRepository.All().Where(o => o.PatientId == id).ToArrayAsync();

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.GetByIdForCloneAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Patient> GetByIdForCloneAsync(int id)
        {
            var patient = await this.GetByIdAsync(id, PatientSpecifications.CopyPatientSpecification);
            ICollection<QuestionnaireResult> qResults = new List<QuestionnaireResult>();

            try
            {
                qResults = (await _questionnaireResultsService.GetForPatient(id)).ToArray();
            } 
            catch (AppException ex) when(ex.StatusCode == HttpStatusCode.NotFound)
            {
                // ignore
            }

            patient.SetInputsAggregator(await _inputsService.GetAggregatorAsync(id));
            patient.SetQuestionnaireResults(qResults);

            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist", exceptionParam);
            }

            return patient;
        }


        /// <summary>
        /// <see cref="IPatientsService.GetByIntakeIdAsync(Guid)"/>
        /// </summary>
        /// <param name="intakeId"></param>
        /// <returns></returns>
        public async Task<Patient> GetByIntakeIdAsync(Guid intakeId)
        {
            var patient = await _patientsRepository
                .All()
                .Active()
                .ByIntakeId(intakeId)
                .IncludeUser()
                .IncludeOrders()
                .IncludeEmployee()
                .IncludePaymentPlans()
                .FirstOrDefaultAsync();

            if (patient is null)
            {
                _logger.LogInformation($"Patient with [IntakeId] = {intakeId} does not exist.");
                var exceptionParam = new AppException.ExceptionParameter(nameof(intakeId), intakeId);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist", exceptionParam);
            }

            return patient;
        }

        public async Task<Patient> GetByUserIdAsync(int userId)
        {
            var patient = await _patientsRepository
                .All()
                .Active()
                .ByUserId(userId)
                .IncludeUser()
                .IncludeOrders()
                .IncludeEmployee()
                .IncludeIntegrations()
                .IncludePaymentPlans()
                .FirstOrDefaultAsync();

            if (patient is null)
            {
                _logger.LogInformation($"Patient with [userId] = {userId} does not exist.");
                var exceptionParam = new AppException.ExceptionParameter(nameof(userId), userId);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist", exceptionParam);
            }

            return patient;
        }

        public async Task<Patient> GetByUserUniversalId(Guid universalId)
        {
            var patient = await _patientsRepository
                .All()
                .Active()
                .ByUserUniversalId(universalId)
                .IncludeUser()
                .FindAsync();

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.CreatePatientAsync(Patient)"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<Patient> CreatePatientAsync(Patient patient)
        {
            await _patientsRepository.AddAsync(patient);
            await _patientsRepository.SaveAsync();

            return patient;
        }

        /// <summary>
        /// <see cref="UpdatePatientOnBoardingStatusAsync(Patient, PatientOnBoardingStatus)"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public async Task<Patient> UpdatePatientOnBoardingStatusAsync(Patient patient, PatientOnBoardingStatus status)
        {
            patient.OnBoardingStatus = status;
            _patientsRepository.Edit(patient);
            await _patientsRepository.SaveAsync();

            _logger.LogInformation(
                $"OnBoarding Status of patient with [Id] = {patient.Id} was changed to [Status] = {status}");

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.AssignToEmployeesAsync(Patient, int[])"/>
        /// Save patient coaches and return newly assignment coaches
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="employeeIds"></param>
        /// <returns></returns>
        public async Task<IEnumerable<int>> AssignToEmployeesAsync(Patient patient, int[] employeeIds)
        {
            _logger.LogInformation($"Assigning employees [{String.Join(", ", employeeIds)}] to patient {patient.Id}");
            
            var allAssignedEmployeeIds = patient.Employees
                .Where(x => x.EmployeeId.HasValue)
                .Where(x => !x.DeletedAt.HasValue)
                .Select(x => x.EmployeeId!.Value!)
                .ToArray();

            var permanentlyAssignedEmployeeIds = patient.Employees
                .Where(x => x.EmployeeId.HasValue && x.IsLinkedAccount)
                .Select(x => x.EmployeeId!.Value!)
                .ToArray();

            var removedAssigmentEmployeeIds = allAssignedEmployeeIds
                .Where(employeeId => !employeeIds.Contains(employeeId))
                .Where(employeeId => !permanentlyAssignedEmployeeIds.Contains(employeeId))
                .Distinct()
                .ToArray();

            var newlyAssignmentsEmployeeIds = employeeIds
                .Where(employeeId => !allAssignedEmployeeIds.Contains(employeeId))
                .Distinct()
                .ToArray();

            await AssertEmployeesFromSameLocationAsync(patient, newlyAssignmentsEmployeeIds);

            _logger.LogInformation($"Unassigning employees [{String.Join(", ", removedAssigmentEmployeeIds)}] from patient {patient.Id}");
            foreach (var employeeId in removedAssigmentEmployeeIds)
            {
                await UnAssign(patient, employeeId);
            }

            _logger.LogInformation($"Assigning employees [{String.Join(", ", employeeIds)}] to patient {patient.Id}");
            foreach (var employeeId in newlyAssignmentsEmployeeIds)
            {
                await AssignToAsync(patient, employeeId);
            }

            return newlyAssignmentsEmployeeIds;
        }

        /// <summary>
        /// <see cref="IPatientsService.LinkToEmployeeAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="employeeId"></param>
        /// <returns></returns>
        public async Task<int> LinkToEmployeeAsync(Patient patient, int employeeId)
        {
            var linkedEmployee = patient.Employees.FirstOrDefault(x => x.IsLinkedAccount);

            if (!(linkedEmployee is null))
            {
                throw new AppException(HttpStatusCode.BadRequest,
                    $"Patient with id: {patient.Id} is already linked to employee account");
            }

            await LinkToAsync(patient, employeeId);

            return employeeId;
        }

        /// <summary>
        /// <see cref="IPatientsService.UpdatePatientOptionsAsync(Patient, PatientOptionsModel)"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        public async Task<Patient> UpdatePatientOptionsAsync(Patient patient, PatientOptionsModel options)
        {
            if (patient.Options.IsFellow == options.IsFellow && 
                patient.Options.IsCrossFitAssociated == options.IsCrossFitAssociated)
            {
                return patient;
            }
            
            patient.Options.IsFellow = options.IsFellow;
            patient.Options.IsCrossFitAssociated = options.IsCrossFitAssociated;

            _patientsRepository.Edit(patient);

            await _patientsRepository.SaveAsync();

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.LinkPatientWithIntegrationSystemAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        public async Task<Patient> LinkPatientWithIntegrationSystemAsync(Patient patient, string integrationId, IntegrationVendor vendor)
        {
            var patientDomain = PatientDomain.Create(patient);
            _logger.LogInformation($"LinkPatientWithIntegrationSystemAsync: linking patient {patient.Id} to {vendor} with {integrationId}");
            patientDomain.LinkWithIntegrationSystem(integrationId, vendor);

            _patientsRepository.Edit(patient);

            await _patientsRepository.SaveAsync();

            return patientDomain.Patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.GetByIdAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Patient> GetByIdAsync(int id)
        {
            var patient = await _patientsRepository
                .All()
                .Active()
                .ById(id)
                .IncludeUser()
                .IncludeEmployee()
                .IncludeLeadSource()
                .IncludeLocation()
                .IncludeOrders()
                .IncludeAllergies()
                .IncludeIntegrations()
                .FirstOrDefaultAsync();
            
            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist", exceptionParam);
            }

            patient.Subscriptions = await _subscriptionsRepository
                .All()
                .RelatedToPatient(id)
                .Include(x=> x.PaymentPrice)
                .ThenInclude(x=> x.Integrations)
                .ThenInclude(x=> x.Integration)
                .Include(x => x.PaymentPrice)
                .ThenInclude(x => x.PaymentCoupon)
                .ThenInclude(x=> x.Integrations)
                .ThenInclude(x=> x.Integration)
                .Include(x => x.PaymentPrice)
                .ThenInclude(x => x.PaymentPeriod)
                .ThenInclude(x => x.PaymentPlan)
                .Include(x => x.Integrations)
                .ThenInclude(x => x.Integration)
                .Include(x => x.PaymentPrice)
                .ThenInclude(x => x.PaymentPeriod)
                .ThenInclude(x => x.PaymentPeriodInclusions)
                .ThenInclude(x => x.Inclusion)
                .ToListAsync();

            patient.PatientSupplements = await _supplementsRepository
                .All()
                .RelatedToPatient(id)
                .ToListAsync();

            patient.PatientMedications = await _medicationsRepository
                .All()
                .RelatedToPatient(id)
                .ToListAsync();

            patient.QuestionnaireResults = await _questionnaireResultsRepository
                .All()
                .RelatedToPatient(id)
                .Include(o => o.Answers)
                .ToListAsync();


            patient.Appointments = await _appointmentsRepository
                .All()
                .RelatedToPatient(id)
                .Include(x => x.Employees)
                .ToListAsync();
            
            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.GetByFirstLastDobAsync"/>
        /// </summary>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="dob"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Patient> GetByFirstLastDobAsync(string firstName, string lastName, string dob)
        {
            var dobDateTime = DateTime.TryParse(dob, out var date) ? date : (DateTime?)null;

            AssertDOBIsValid(dobDateTime);

            var patients = await _patientsRepository
                .All()
                .Active()
                .IncludeUser()
                .Where(x => x.User.FirstName == firstName)
                .Where(x => x.User.LastName == lastName)
                .Where(x => x.User.Birthday == dobDateTime)
                .ToListAsync();

            AssertNoMoreThanOnePatient(patients);

            var patient = patients.FirstOrDefault();

            AssertPatientPresent(patient);

            return patient!;
        }

        /// <summary>
        /// <see cref="IPatientsService.GetByIntegrationIdAsync"/>
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="vendor"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Patient> GetByIntegrationIdAsync(string integrationId, IntegrationVendor vendor)
        {
            var patient = await _patientsRepository
                .All()
                .Active()
                .ByIntegrationId<Patient, WildHealth.Domain.Entities.Integrations.PatientIntegration>(integrationId, vendor, IntegrationPurposes.Patient.ExternalId)
                .IncludePaymentPrice()
                .IncludeIntegrations()
                .IncludeUser()
                .IncludeEmployee()
                .FirstOrDefaultAsync();

            if (patient is null)
            {
                throw new AppException(HttpStatusCode.NotFound, $"Patient with integration id: {integrationId} does not exist");
            }

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.LockPatientAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<Patient> LockPatientAsync(Patient patient)
        {
            patient.Lock();

            _patientsRepository.Edit(patient);

            await _patientsRepository.SaveAsync();

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.UnlockPatientAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<Patient> UnlockPatientAsync(Patient patient)
        {
            patient.Unlock();

            _patientsRepository.Edit(patient);

            await _patientsRepository.SaveAsync();

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.UpdateProviderAssignmentAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public async Task<Patient> UpdateProviderAssignmentAsync(Patient patient, Employee? provider)
        {
            if (patient?.Employees is null)
            {
                throw new AppException(HttpStatusCode.BadRequest, "Patient does not exist");
            }

            var currentProviders = patient
                .Employees
                .Where(x => x.Employee.Type == EmployeeType.Provider)
                .ToArray();

            if (provider is null)
            {
                return patient;
            }
            
            if (currentProviders.Any(x => x.EmployeeId == provider.Id))
            {
                return patient;
            }
            
            AssertEmployeeFromSameLocation(patient, provider);

            return await AssignToAsync(patient, provider.GetId());
        }

        /// <summary>
        /// <see cref="IPatientsService.UpdateAsync"/>
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        public async Task<Patient> UpdateAsync(Patient patient)
        {
            _patientsRepository.Edit(patient);

            await _patientsRepository.SaveAsync();

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.GetPatientsWithQuestionnaireResultsAsync()"/>
        /// </summary>
        /// <returns></returns>
        public async Task<ICollection<Patient>> GetPatientsWithQuestionnaireResultsAsync()
        {
            return await _patientsRepository
                .All()
                .IncludePaymentPlans()
                .IncludeUser()
                .WithCompletedUserRegistration(true)
                .IncludeQuestionnaireResults()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IPatientsService.GetPracticumPatientsAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="fellowId"></param>
        /// <returns></returns>
        public async Task<ICollection<Patient>> GetPracticumPatientsAsync(int practiceId, int? fellowId = null)
        {
            return await _patientsRepository
                .All()
                .RelatedToPractice(practiceId)
                .IncludeEmployee()
                .Where(patient => patient.FellowId == fellowId)
                .IncludeUser()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IPatientsService.PatientsAssignedToEmployeeSinceAsync"/>
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="since"></param>
        /// <returns></returns>
        public async Task<ICollection<Patient>> PatientsAssignedToEmployeeSinceAsync(int employeeId, DateTime since)
        {
            return await _patientsRepository
                .All()
                .AssignedToEmployeeSince(employeeId, since)
                .IncludeUser()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IPatientsService.GetPracticumPatientsByFellowCreationAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public async Task<ICollection<Patient>> GetPracticumPatientsByFellowCreationAsync(
            int practiceId,
            DateTime startDate, 
            DateTime endDate)
        {
            return await _patientsRepository
                .All()
                .RelatedToPractice(practiceId)
                .IncludeFellow()
                .Where(p => 
                    !p.DeletedAt.HasValue &&
                    p.Fellow.CreatedAt >= startDate &&
                    p.Fellow.CreatedAt < endDate)
                .IncludeUser()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IPatientsService.GetPracticumPatientsByRosterAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <param name="rosterId"></param>
        /// <returns></returns>
        public async Task<ICollection<Patient>> GetPracticumPatientsByRosterAsync(int practiceId, int rosterId)
        {
            return await _patientsRepository
                .All()
                .RelatedToPractice(practiceId)
                .IncludeFellow()
                .Where(p => !p.Fellow.DeletedAt.HasValue && p.Fellow.RosterId == rosterId)
                .IncludeUser()
                .ToArrayAsync();
        }

        /// <summary>
        /// <see cref="IPatientsService.GetPatientWithAppointments(int)"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Patient> GetPatientWithAppointments(int patientId)
        {
            var patient = await _patientsRepository
                .All()
                .IncludePaymentPlans()
                .Include(x=> x.User)
                .IncludeJustEmployee()
                .Include(x => x.Appointments)
                    .ThenInclude(x => x.Employees)
                    .ThenInclude(x => x.Employee)
                .Include(x => x.Appointments)
                    .ThenInclude(o => o.Configuration)
                .Include(x => x.Employees)
                    .ThenInclude(x => x.Employee)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == patientId);

            if (patient is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(patientId), patientId);
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist", exceptionParam);
            }

            return patient;
        }
        
        /// <summary>
        /// <see cref="IPatientsService.GetInsurancePatientsWithUpcomingAppointment"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Patient[]> GetInsurancePatientsWithUpcomingAppointment(int practiceId, DateTime from, DateTime to)
        {
            var patients = await _patientsRepository
                .All()
                .IncludeUser()
                .Include(x => x.Appointments)
                .ThenInclude(x => x.PatientProduct)
                .Where(x => x.User.PracticeId == practiceId && x.Appointments.Any(o => 
                    o.StartDate >= from 
                    && o.StartDate <= to 
                    && o.PatientProduct.PaymentFlow == PaymentFlow.Insurance
                    && o.Status == AppointmentStatus.Submitted))
                .ToArrayAsync();

            return patients;
        }

        /// <summary>
        /// Fetches and returns patient
        /// </summary>
        /// <param name="patientId"></param>
        /// <returns></returns>
        public async Task<Patient> FetchPatientAsync(int patientId)
        {
            var patient = await _patientsRepository.All()
                .Include(x => x.User)
                .Include(x => x.Agreements)
                .ThenInclude(x => x.Agreement)
                .Include(x => x.Agreements)
                .ThenInclude(x => x.Subscription)
                .ThenInclude(x => x.PaymentPrice)
                .ThenInclude(x => x.PaymentPeriod)
                .ThenInclude(x => x.PaymentPlan)
                .Include(x => x.Agreements)
                .ThenInclude(x => x.Subscription)
                .ThenInclude(x => x.PaymentPrice)
                .ThenInclude(x => x.PaymentCoupon)
                .FirstAsync(x => x.Id == patientId);

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.SetFellowshipNote"/>
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="note"></param>
        /// <returns></returns>
        public async Task<Patient> SetFellowshipNote(int patientId, string note)
        {
            var patient = await _patientsRepository
                .All()
                .IncludeEmployee()
                .IncludeUser()
                .FirstOrDefaultAsync(x => x.Id == patientId);

            if (patient is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist");
            }

            patient.SetFellowshipNote(note);

            _patientsRepository.Edit(patient);
            await _patientsRepository.SaveAsync();

            return patient;
        }

        /// <summary>
        /// <see cref="IPatientsService.GetUnassignedPracticePatientsAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<ICollection<Patient>> GetUnassignedPracticePatientsAsync(int practiceId)
        {
            return await _patientsRepository
                .All()
                .IncludeUser()
                .RelatedToPractice(practiceId)
                .ByPlanStatus(isPlanActive: true)
                .AssignedToEmployee(assignedTo: null)
                .ToArrayAsync();
        }

        public async Task<ICollection<Patient>> GetPatientsForNotificationAsync(string[] paymentPlans, int[] onlyPatientIds, DateTime? dateFrom = null, 
            DateTime? dateTo = null,  bool? hasCompletedAppointment = null, bool? hasActiveSubscription = null)
        {
            var result = _patientsRepository
                .All()
                .Active()
                .ByPlanStatus(hasActiveSubscription)
                .IncludeUser();

            if (onlyPatientIds.Any())
            {
                result = result.ByIds(onlyPatientIds);
            }
            else
            {
                if (hasCompletedAppointment.HasValue)
                {
                    result = result.ByCompletedAppointments(DateTime.UtcNow, hasCompletedAppointment.Value);
                }
                if (dateTo.HasValue)
                {
                    result = result.Where(x => x.User.CreatedAt <= dateTo.Value);
                }
                if (dateFrom.HasValue)
                {
                    result = result.Where(x => x.User.CreatedAt >= dateFrom.Value);
                }
                if (paymentPlans.Any())
                {
                    result = result.Where(x => paymentPlans.Contains(PatientDomain.Create(x).CurrentPlanName));
                }
            }
            
            return await result.ToArrayAsync();

        }

        /// <summary>
        /// Patients that are considered at risk because they do not have an ICC or ICC scheduled date is after a certain threshold
        /// No health coach appointments and greater than 14 days since start date
        /// Health coach appointment greater than 14 days since start date
        /// </summary>
        /// <returns></returns>
        public async Task<AtRiskIccPatientRaw[]> AtRiskIccDue()
        {
            var queryPath = "Queries/CustomSql/Sql/AtRiskIcc.sql";

            return (await _customSqlDataRunner.GetDataSet<AtRiskIccPatientRaw>(queryPath, new List<CustomSqlDataParameter>())).ToArray();
        }

        /// <summary>
        /// Gets PatientCohort information for a given patient
        /// </summary>
        /// <returns></returns>
        public async Task<PatientCohortModelRaw> GetPatientCohort(int patientId)
        {
            var queryPath = "Queries/CustomSql/Sql/PatientCohortQuery.sql";

            var parameters = new List<CustomSqlDataParameter>
            {
                new ()
                {
                    ParameterName = "@patientId",
                    ParameterValue = patientId.ToString()
                }
            };
            return (await _customSqlDataRunner.GetDataSet<PatientCohortModelRaw>(queryPath, parameters)).First();
        }

        public async Task<bool> IsPremium(int patientId)
        {
            return await _subscriptionsRepository.All().Active().AnyAsync(s =>
                s.PatientId == patientId &&
                PremiumPaymentPlan.Names.Contains(s.PaymentPrice.PaymentPeriod.PaymentPlan.Name));
        }

        public async Task<bool> HasLabs(int patientId)
        {
            return await _patientsRepository.All().AnyAsync(x => x.Id == patientId && x.LabsStatus != PatientLabsStatus.NA);
        }

        /// <summary>
        /// Patients that are considered at risk because they do not have an IMC or IMC scheduled date is after a certain threshold
        /// </summary>
        /// <returns></returns>
        public async Task<AtRiskImcPatientRaw[]> AtRiskImcDue()
        {
            var queryPath = "Queries/CustomSql/Sql/AtRiskImc.sql";

            return (await _customSqlDataRunner.GetDataSet<AtRiskImcPatientRaw>(queryPath, new List<CustomSqlDataParameter>())).ToArray();
        }

        #region private

        private async Task AssertEmployeesFromSameLocationAsync(Patient patient, int[] employeeIds)
        {
            var employees = await _employeeService.GetActiveAsync(
                ids: employeeIds,
                practiceId: patient.User.PracticeId,
                locationId: patient.LocationId);

            if (employeeIds.Count() != employees.Count())
            {
                throw new AppException(HttpStatusCode.BadRequest,
                    "Some employees has no permissions for this location.");
            }
        }

        private void AssertEmployeeFromSameLocation(Patient patient, Employee employee)
        {
            if (employee.Locations.All(x => patient.LocationId != x.LocationId))
            {
                throw new AppException(HttpStatusCode.BadRequest,
                    $"Employee with id: {employee.GetId()} has no permissions for patient with id: {patient.GetId()}.");
            }
        }

        private void AssertDOBIsValid(DateTime? dob)
        {
            if (dob is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Invalid date of birth format");
            }
        }

        private void AssertNoMoreThanOnePatient(List<Patient> patients)
        {
            if (patients.Count() > 1)
            {
                throw new AppException(HttpStatusCode.NotFound, "Multiple patients found with information provided");
            }
        }

        private void AssertPatientPresent(Patient? patient)
        {
            if (patient is null)
            {
                throw new AppException(HttpStatusCode.NotFound, "Patient does not exist");
            }
        }

        private async Task<Patient> AssignToAsync(Patient patient, int employeeId)
        {
            var assigment = new PatientEmployee(patient.GetId(), employeeId);

            await CreateTimelineEvent(patient, employeeId);
            
            await _patientsRepository.AddRelatedEntity(assigment);
            await _patientsRepository.SaveAsync();

            _logger.LogInformation(
                $"Patient with [Id] = {patient.Id} was assigned to employee with [Id] = {employeeId}");

            return patient;
        }

        private async Task CreateTimelineEvent(Patient patient, int employeeId)
        {
            var fullName = await _employeeRepository.All()
                .Where(e => e.Id == employeeId)
                .Select(e => new { e.User.FirstName, e.User.LastName })
                .FirstOrDefaultAsync();

            if(fullName is null) return;
            
            var timelineEvent = new HealthCoachOrProviderAssignedTimelineEvent(
                patient.GetId(),
                DateTime.UtcNow,
                new HealthCoachOrProviderAssignedTimelineEvent.Data($"{fullName.FirstName} {fullName.LastName}"));
            
            await _timelineRepository.AddAsync(timelineEvent);
            await _timelineRepository.SaveAsync();
        }

        private async Task<Patient> LinkToAsync(Patient patient, int employeeId)
        {
            var assigment = new PatientEmployee(patient.GetId(), employeeId, true);

            await _patientsRepository.AddRelatedEntity(assigment);
            await _patientsRepository.SaveAsync();

            _logger.LogInformation($"Patient with [Id] = {patient.Id} was linked to employee with [Id] = {employeeId}");

            return patient;
        }

        private async Task<Patient> UnAssign(Patient patient, int employeeId)
        {
            var employeeToUnAssign = patient.Employees.FirstOrDefault(pc => pc.EmployeeId == employeeId && !pc.DeletedAt.HasValue);
            if (employeeToUnAssign == null) return patient;
            _patientEmployeeRepository.Delete(employeeToUnAssign);
            await _patientsRepository.SaveAsync();

            _logger.LogInformation(
                $"Patient with [Id] = {patient.Id} was unassigned from employee with [Id] = {employeeId}");

            return patient;
        }

        public async Task<Patient[]>  GetPatientsWOMessagesOrConversation()
        {
            var result = await _patientsRepository
                .All()
                .IncludeUser()
                .ExcludeEmptyConversations()
                .ExcludeWithoutHealthCoach()
                .ToArrayAsync();

            return result;

        }

        public async Task<PatientStatusModel[]> GetMyPatientsWithFilters(int employeeId, MyPatientsFilterModel filter)
        {
            var allPatients = await GetAllMyPatientsNoFilter();
            
            var emp = await _employeeService.GetByIdAsync(filter.EmployeeId);
            
            var results = _filterHelper.HandlePatientFilter(allPatients, filter, emp);
            
            return results;
        }
        
        public async Task<PatientStatusModel[]> GetMyPatientsWithFiltersWithoutAssigment(MyPatientsFilterModel filter)
        {
            var allPatients = await GetAllMyPatientsNoFilter();
            
            var results = _filterHelper.HandlePatientFilterWithoutAssigment(allPatients, filter);
            
            return results;
        }

        private async Task<PatientStatusModel[]> GetAllMyPatientsNoFilterNoCache()
        {
            var queryPath = "Queries/CustomSql/Sql/MyPatientsQueryNoParams.sql";

            var myPatientsResultsRaw =
                await _customSqlDataRunner.GetDataSet<PatientRawSQLModel>(queryPath, new List<CustomSqlDataParameter>(), MyPatientsQueryTimeoutSeconds);
            
            _logger.LogInformation($"Received {myPatientsResultsRaw.Count()} results");

            _logger.LogInformation("Reducing...");
            
            var myPatientsResultsReduced = myPatientsResultsRaw.GroupBy(o => o.PatientId).Select(o =>
            {
                var mainModel = _mapper.Map<PatientStatusModel>(o.FirstOrDefault());

                mainModel.Tags = o.Where(o => !string.IsNullOrEmpty(o.TagName)).Select(a => new TagModel
                {
                    Name = a.TagName,
                    Description = a.TagDescription,
                    Sentiment = a.TagSentiment
                }).DistinctBy(t => t.Name).ToArray();

                mainModel.AssignedEmployees = o.Where(x => x.AssignedEmployee.HasValue)
                                              .Select(x => x.AssignedEmployee!.Value).ToArray();

                return mainModel;
            });
            
            _logger.LogInformation("Done.");

            return myPatientsResultsReduced.ToArray();

        }

        public async Task<PatientStatusModel[]> GetAllMyPatientsNoFilter()
        {
            return await GetAllMyPatientsNoFilterNoCache();
        }

        public async Task<Patient[]> GetAllWithUnreadMessagesSince(int minutes)
        {
            return await _patientsRepository
               .All()
               .IncludeConversationsFilterByUnreadInMinutes(minutes)
               .ToArrayAsync();
        }


        public async Task<Patient[]> GetAllWithDaysSinceSubscription(int days)
        {
            return await _patientsRepository
                .All()
                .IncludeUserWithDaysOfRegistration(days)
                .ToArrayAsync();
        }


        /// <summary>
        /// Get Patients with active
        /// </summary>
        /// <returns>patients</returns>
        public async Task<Patient[]> GetAllWithActiveSubscription()
        {

            return await _patientsRepository
                .All()
                .Active()
                .IncludeUser()
                .ByPlanStatus(true)
                .ToArrayAsync();

        }

        /// <summary>
        /// <see cref="IPatientsService.GetUniversalIdForPatientId(int)"/>
        /// </summary>
        public async Task<string> GetUniversalIdForPatientId(int patientId)
        {
            var patient = await GetByIdAsync(patientId);
            return patient.User.UserId();
        }
        
        #endregion
        
        
        #region Private

        private object? BoxNullableInt(int? value) => value.HasValue ? value.Value : null;
        
        private CustomSqlDataParameter[] BuildMyPatientsParameters(MyPatientsFilterModel filter)
        {
            var parameters = new List<CustomSqlDataParameter>();
            
            parameters.Add(new CustomSqlDataParameter()
            {
                ParameterName = "@employeeId",
                ParameterValue = filter.EmployeeId.ToString(),
                DbType = DbType.String
            });
            
            parameters.Add(new CustomSqlDataParameter()
            {
                ParameterName = "@includesTag",
                ParameterValue = filter.IncludesTags.FirstOrDefault(),
                DbType = DbType.String
            });
            
            parameters.Add(new CustomSqlDataParameter()
            {
                ParameterName = "@lastAppointmentGreaterThanDaysAgo",
                ParameterValue = BoxNullableInt(filter.LastAppointmentGreaterThanDaysAgo),
                DbType = DbType.Int32
            });
            
            parameters.Add(new CustomSqlDataParameter()
            {
                ParameterName = "@lastCoachingVisitGreaterThanDaysAgo",
                ParameterValue = BoxNullableInt(filter.LastCoachingVisitGreaterThanDaysAgo),
                DbType = DbType.Int32
            });
            
            parameters.Add(new CustomSqlDataParameter()
            {
                ParameterName = "@lastMessageSentGreaterThanDaysAgo",
                ParameterValue = BoxNullableInt(filter.LastMessageSentGreaterThanDaysAgo),
                DbType = DbType.Int32
            });
            
            parameters.Add(new CustomSqlDataParameter()
            {
                ParameterName = "@daysSinceIccWithoutImcScheduledFromToday",
                ParameterValue = BoxNullableInt(filter.DaysSinceIccWithoutImcScheduledFromToday),
                DbType = DbType.Int32
            });
            
            parameters.Add(new CustomSqlDataParameter()
            {
                ParameterName = "@planRenewalDateLessThanDaysFromToday",
                ParameterValue = BoxNullableInt(filter.PlanRenewalDateLessThanDaysFromToday),
                DbType = DbType.Int32
            });
            
            parameters.Add(new CustomSqlDataParameter()
            {
                ParameterName = "@daysSinceSignUpWithoutIccScheduledFromToday",
                ParameterValue = BoxNullableInt(filter.DaysSinceSignUpWithoutIccScheduledFromToday),
                DbType = DbType.Int32
            });

            return parameters.ToArray();
        }

        
        #endregion
    }
}