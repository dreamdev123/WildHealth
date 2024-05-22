using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Events.Patients;
using WildHealth.Application.Services.Agreements;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Utils.PatientCreator;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Common.Models.Patients;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Domain.Enums.Patient;
using WildHealth.Integration.Models.Patients;
using WildHealth.Integration.Models.Subscriptions;
using WildHealth.Shared.Enums;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;
using Newtonsoft.Json;
using CsvHelper;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Extensions;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Constants;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class BulkPatientsMigrateFromIntegrationSystemCommandHandler : IRequestHandler<BulkPatientsMigrateFromIntegrationSystemCommand, IEnumerable<BulkPatientsMigrateResultModel>>
    {
        private const string OriginalWildHealthPlan = "ORIGINAL_WILD_HEALTH_PLAN";
        private const string DefaultUserNote = "patient_migrated_from_integration_system";

        private readonly IPatientCreator _patientCreator;
        private readonly IPatientsService _patientsService;
        private readonly ILocationsService _locationsService;
        private readonly IAgreementsService _agreementsService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly ITransactionManager _transactionManager;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly IEmployeeService _employeeService;
        private readonly IMediator _mediator;
        private readonly MaterializeFlow _materialization;
        ILogger<BulkPatientsMigrateFromIntegrationSystemCommandHandler> _logger;

        public BulkPatientsMigrateFromIntegrationSystemCommandHandler(
            IPatientCreator patientCreator,
            IPatientsService patientsService,
            ILocationsService locationsService,
            IAgreementsService agreementsService,
            ISubscriptionService subscriptionService,
            IPaymentPlansService paymentPlansService,
            IEmployeeService employeeService,
            ITransactionManager transactionManager,
            IIntegrationServiceFactory integrationServiceFactory,
            IMediator mediator,
            ILogger<BulkPatientsMigrateFromIntegrationSystemCommandHandler> logger, 
            MaterializeFlow materialization)
        {
            _patientCreator = patientCreator;
            _patientsService = patientsService;
            _locationsService = locationsService;
            _agreementsService = agreementsService;
            _subscriptionService = subscriptionService;
            _paymentPlansService = paymentPlansService;
            _transactionManager = transactionManager;
            _integrationServiceFactory = integrationServiceFactory;
            _employeeService = employeeService;
            _mediator = mediator;
            _transactionManager = transactionManager;
            _mediator = mediator;
            _logger = logger;
            _materialization = materialization;
        }

        public async Task<IEnumerable<BulkPatientsMigrateResultModel>> Handle(BulkPatientsMigrateFromIntegrationSystemCommand request, CancellationToken cancellationToken)
        {
            var integrationService = await _integrationServiceFactory.CreateAsync(request.PracticeId);
            var location = await _locationsService.GetDefaultLocationAsync(request.PracticeId);
            var plansMap = CreatePlansMap(request.PlanMapJson);
            var records = await ParseCsvAsync(request.File);
            var (patients, _) = await _patientsService.SelectPatientsAsync(
                practiceId: request.PracticeId,
                locationIds: new [] { request.LocationId });
            var paymentPlans = await _paymentPlansService.GetAllAsync(request.PracticeId);
            var paymentPrices = paymentPlans
                .SelectMany(c => c.PaymentPeriods)
                .SelectMany(c => c.Prices)
                .ToArray();

            var result = new List<BulkPatientsMigrateResultModel>();

            foreach (var csvItem in records)
            {
                var originPatients = (await integrationService.GetPatientsAsync(csvItem.Email))?.ToArray();
                var (isValid, comment) = Validate(
                    originPatients: originPatients, 
                    clarityPatients: patients.ToArray(), 
                    paymentPrices: paymentPrices, 
                    plansMap: plansMap,
                    usePlanFromIntegrationSystem: request.UsePlanFromIntegrationSystem,
                    csvModel: csvItem,
                    integrationVendor: integrationService.IntegrationVendor);

                if (isValid && request.SaveMode)
                {
                    var originPatient = originPatients!.First();
                    var paymentPrice = request.UsePlanFromIntegrationSystem
                        ? GetPaymentPriceFromIntegrationSystem(paymentPrices, GetLastSubscription(originPatient.Subscriptions), integrationService.IntegrationVendor)
                        : GetPaymentPriceFromMap(paymentPrices, plansMap[csvItem.Plan]);

                    var (saveResult, saveComment) = await SavePatientAsync(
                        integrationService: integrationService,
                        originPatient: originPatient, 
                        paymentPrice: paymentPrice, 
                        location: location, 
                        status: request.Status,
                        sendConfirmationEmail: request.SendConfirmationEmail,
                        confirmAgreements: request.ConfirmAgreements,
                        dpc: csvItem.DPC,
                        integrationVendor: integrationService.IntegrationVendor,
                        cancellationToken: cancellationToken);
                    
                    result.Add(new BulkPatientsMigrateResultModel(csvItem, saveResult, saveComment));

                    continue;
                }

                result.Add(new BulkPatientsMigrateResultModel(csvItem, GetUpdateStatus(isValid, request.SaveMode), comment));
            }

            return result;
        }

        #region PlanMap

        /// <summary>
        /// Create payment plans as concat default plans with JSON plans from request
        /// </summary>
        /// <param name="planMapJson"></param>
        /// <returns></returns>
        private Dictionary<string, int> CreatePlansMap(string planMapJson)
        {
            var defaultPlanMap = new Dictionary<string, int>
            {
                { "Clarity Helix", 0 },
                { "Clarity Optimization 12 months", 9 },
                { "Clarity Optimization 12 months (FULL)", 10 },
                { "Clarity Optimization 12 months (FULL) - 10%", 513 },
                { "Clarity Optimization 4 months (FULL)", 12 },
                { "Clarity Optimization 4 months (FULL) - 10%", 511 },
                { "Clarity Optimization Fellowship 4 months", 14 },
                { "Clarity Precision Care 12 months", 1 },
                { "Clarity Precision Care 12 months - 10%", 505 },
                { "Clarity Precision Care 12 months (FULL)", 2 },
                { "Clarity Precision Care 12 months (FULL) -10%", 507 },
                { "Clarity Precision Care 4 months", 3 },
                { "Clarity Precision Care 4 months (FULL)", 4 },
                { "Clarity Precision Health Coaching 12 months - 10%", 509 },
                { "Clarity Precision Health Coaching 12 months (FULL)", 6 },
                { "Clarity Precision Health Coaching 12 months (FULL) - 10%", 510 },
                { "Clarity Precision Health Coaching 4 months (FULL)", 8 },
                { "Founder PMOC", 0 },
                { "Founder Signup $1995", 0 },
                { "Founders Precision 12 months", 0 },
                { "Founders Precision 4 months", 0 },
                { "Free Trial", 13 },
                { "Neurofeedback Only", 0 },
                { "PMOC", 0 },
                { "Precision Primary Care", 0 },
                { "Precision Primary Care + Precision Coaching", 0 },
                { "Precision Primary Care Virtual", 0 },
                { "Wild Health Initial Signup $295 Option", 0 },
                { "Wild Health Initial Signup $795 Option", 0 },
            };

            try
            {
                var planMaps = JsonConvert.DeserializeObject<Dictionary<string, int>>(planMapJson)!;

                var result = defaultPlanMap;

                foreach (var item in planMaps)
                {
                    if (defaultPlanMap.ContainsKey(item.Key))
                    {
                        result[item.Key] = item.Value;
                        continue;
                    }

                    result.Add(item.Key, item.Value);
                }

                return result;
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Create plans map has failed with [Error]: {e.ToString()}");
                return defaultPlanMap;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate CSV record. Returns possibility to use with comment
        /// </summary>
        /// <param name="originPatients"></param>
        /// <param name="clarityPatients"></param>
        /// <param name="paymentPrices"></param>
        /// <param name="plansMap"></param>
        /// <param name="usePlanFromIntegrationSystem"></param>
        /// <param name="csvModel"></param>
        /// <param name="integrationVendor"></param>
        /// <returns></returns>
        private (bool, string) Validate(
            PatientIntegrationModel[]? originPatients,
            Patient[] clarityPatients,
            IEnumerable<PaymentPrice> paymentPrices,
            Dictionary<string, int> plansMap,
            bool usePlanFromIntegrationSystem,
            BulkPatientsMigrateCsvModel csvModel,
            IntegrationVendor integrationVendor)
        {
            if (originPatients.Empty())
            {
                return (false, "There are no similar patients in the integration system");
            }

            if (originPatients!.Count() > 1)
            {
                return (false, "There are 2 or more patients with a similar email in the integration system");
            }

            var originPatient = originPatients!.First();

            if (clarityPatients.Any(c => c.User.Email == csvModel.Email))
            {
                return (false, "Patient with similar email already exists in Clarity");
            }

            if (clarityPatients.Any(c => c.GetIntegrationId(integrationVendor) == originPatient.Id))
            {
                return (false, "Patient with similar IntegrsationId is already exist in Clarity");
            }

            var originSubscription = GetLastSubscription(originPatient.Subscriptions);
            if (originSubscription is null)
            {
                return (false, "Patient does not have an active membership in Integration system");
            }

            if (!usePlanFromIntegrationSystem)
            {
                var isPlanExist = plansMap.TryGetValue(csvModel.Plan, out int paymentPriceId);
                if (!isPlanExist)
                {
                    return (false, "No mapped plan in Clarity");
                }

                var paymentPrice = paymentPrices.FirstOrDefault(c => c.Id == paymentPriceId);

                if (paymentPrice is null)
                {
                    return (false, "PaymentPrice is absent in Clarity");
                }

                if (!paymentPrice.IsNotIntegratedPlan() && paymentPrice.GetIntegrationId(integrationVendor) != originSubscription.PlanId)
                {
                    return (false, "Payment plan is difference in Integration System");
                }
            }

            return (true, "");
        }

        #endregion

        #region Save process

        /// <summary>
        /// Facade for create new Patient
        /// </summary>
        /// <param name="integrationService"></param>
        /// <param name="originPatient"></param>
        /// <param name="paymentPrice"></param>
        /// <param name="location"></param>
        /// <param name="status"></param>
        /// <param name="sendConfirmationEmail"></param>
        /// <param name="confirmAgreements"></param>
        /// <param name="integrationVendor"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="dpc"></param>
        /// <returns></returns>
        private async Task<(string, string)> SavePatientAsync(
            IWildHealthIntegrationService integrationService,
            PatientIntegrationModel originPatient,
            PaymentPrice paymentPrice,
            Location location,
            PatientOnBoardingStatus status,
            bool sendConfirmationEmail,
            bool confirmAgreements,
            bool dpc,
            IntegrationVendor integrationVendor,
            CancellationToken cancellationToken)
        {
            await using var transaction = _transactionManager.BeginTransaction();

            try
            {
                var patient = await CreatePatientAsync(originPatient, location, cancellationToken);

                await _patientsService.UpdatePatientOnBoardingStatusAsync(patient, status);
                
                await _patientsService.LinkPatientWithIntegrationSystemAsync(patient, originPatient.Id, integrationVendor);

                var employee = await _employeeService.GetByIntegrationIdAsync(
                    integrationId: originPatient.PractitionerId, 
                    vendor: IntegrationVendor.Hint,
                    purpose: IntegrationPurposes.User.Customer,
                    practiceId: patient.User.PracticeId,
                    locationId: patient.LocationId
                );

                await _patientsService.UpdateProviderAssignmentAsync(patient, employee);

                var (subscription, membership) = await CreateSubscriptionAsync(
                    integrationService: integrationService,
                    patient: patient,
                    createdAt: originPatient.CreatedAt,
                    paymentPlanId: paymentPrice.PaymentPeriod.PaymentPlan.GetId(),
                    paymentPeriodId: paymentPrice.PaymentPeriod.GetId(),
                    paymentPriceId: paymentPrice.GetId(),
                    practiceId: location.PracticeId);

                if (membership != null)
                {
                    await new MarkSubscriptionAsPaidFlow(subscription, membership.Id, integrationService.IntegrationVendor).Materialize(_materialization);
                }

                if (confirmAgreements)
                {
                    await _agreementsService.CreateUnsignedConfirmationsAsync(patient, subscription!);
                }
                
                await transaction.CommitAsync(cancellationToken);

                if (sendConfirmationEmail)
                {
                    await _mediator.Publish(new PatientMigratedFromIntegrationSystemEvent(patient, subscription!, dpc), cancellationToken);
                }

                return (GetUpdateStatus(success: true, saveMode: true), "");
            }
            catch (Exception e)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning($"Save patient has failed with [Error]: {e.ToString()}");
                return (GetUpdateStatus(success: false, saveMode: true), e.Message);
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }

        /// <summary>
        /// Create patient in database
        /// </summary>
        /// <param name="originPatient"></param>
        /// <param name="location"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Patient> CreatePatientAsync(
            PatientIntegrationModel originPatient,
            Location location,
            CancellationToken cancellationToken)
        {
            var address = new AddressModel
            {
                City = originPatient.City,
                Country = originPatient.Country,
                State = originPatient.State,
                StreetAddress1 = originPatient.Address1,
                StreetAddress2 = originPatient.Address2,
                ZipCode = originPatient.ZipCode
            };

            var createUserCommand = new CreateUserCommand(
                firstName: originPatient.FirstName,
                lastName: originPatient.LastName,
                email: originPatient.Email,
                phoneNumber: originPatient.Phone,
                password: Guid.NewGuid().ToString().Substring(0, 12),
                birthDate: originPatient.Birthday,
                gender: originPatient.Sex,
                userType: UserType.Patient,
                practiceId: location.PracticeId,
                billingAddress: address,
                shippingAddress: address,
                isVerified: false,
                isRegistrationCompleted: true,
                note: DefaultUserNote
            );

            var user = await _mediator.Send(createUserCommand, cancellationToken);

            var patient = await _patientCreator.Create(user, null, location);

            await _patientsService.CreatePatientAsync(patient);

            return patient;
        }

        /// <summary>
        /// Create Clarity subscription
        /// </summary>
        /// <param name="integrationService"></param>
        /// <param name="patient"></param>
        /// <param name="createdAt"></param>
        /// <param name="paymentPlanId"></param>
        /// <param name="paymentPeriodId"></param>
        /// <param name="paymentPriceId"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        private async Task<(Subscription?, SubscriptionIntegrationModel?)> CreateSubscriptionAsync(
            IWildHealthIntegrationService integrationService,
            Patient patient,
            DateTime createdAt,
            int paymentPlanId,
            int paymentPeriodId,
            int paymentPriceId,
            int practiceId)
        {
            var paymentPlan = await _paymentPlansService.GetPlanAsync(paymentPlanId, paymentPeriodId, practiceId);
            var paymentPeriod = paymentPlan.PaymentPeriods.First(x => x.Id == paymentPeriodId);
            var paymentPrice = paymentPeriod.Prices.First(x => x.Id == paymentPriceId);

            var originSubscription = await GetLastSubscriptionAsync(integrationService, patient);

            var startDate = GetSubscriptionStartDate(originSubscription, createdAt);
            var endDate = GetSubscriptionEndDate(originSubscription, paymentPeriod.PeriodInMonths);

            var subscription = await _subscriptionService.CreatePastSubscriptionAsync(
                patient: patient,
                paymentPrice: paymentPrice,
                startDate: startDate,
                endDate: endDate
            );

            return (subscription, originSubscription);
        }

        /// <summary>
        /// Returns last membership
        /// </summary>
        /// <param name="integrationService"></param>
        /// <param name="patient"></param>
        /// <returns></returns>
        private async Task<SubscriptionIntegrationModel?> GetLastSubscriptionAsync(
            IWildHealthIntegrationService integrationService,
            Patient patient)
        {
            var subscriptions = await integrationService.GetPatientSubscriptionsAsync(patient);
            
            return GetLastSubscription(subscriptions);
        }
        
        private SubscriptionIntegrationModel? GetLastSubscription(ICollection<SubscriptionIntegrationModel> subscriptions)
        {
            var activeSubscription = subscriptions.FirstOrDefault(x => x.Status == "active");
            if (activeSubscription != null)
            {
                return activeSubscription;
            }

            var lastSubscription = subscriptions
                .OrderByDescending(x => x.StartDate.AddMonths(x.PeriodInMonths ?? 0))
                .FirstOrDefault();

            return lastSubscription;
        }

        #endregion

        #region Helpers

        private PaymentPrice GetPaymentPriceFromMap(IEnumerable<PaymentPrice> prices, int planId)
        {
            return prices.First(c => c.Id == planId);
        }

        private PaymentPrice GetPaymentPriceFromIntegrationSystem(IEnumerable<PaymentPrice> prices, SubscriptionIntegrationModel? subscription, IntegrationVendor vendor)
        {
            var paymentPrice = prices.FirstOrDefault(c => c.GetIntegrationId(vendor) == subscription?.PlanId);

            return paymentPrice ?? prices.First(c => c.PaymentPeriod.PaymentPlan.Name == OriginalWildHealthPlan);
        }

        /// <summary>
        /// Calculates and returns subscription End date based on membership
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="periodInMonths"></param>
        /// <returns></returns>
        private DateTime GetSubscriptionEndDate(SubscriptionIntegrationModel? subscription, int periodInMonths)
        {
            if (subscription is null)
            {
                return DateTime.UtcNow.AddDays(-1);
            }

            if (subscription.EndDate.HasValue)
            {
                return subscription.EndDate.Value;
            }

            return subscription.StartDate.AddMonths(periodInMonths);
        }

        /// <summary>
        /// Calculates and returns subscription start date based on membership
        /// </summary>
        /// <param name="membership"></param>
        /// <param name="defaultDate"></param>
        /// <returns></returns>
        private DateTime GetSubscriptionStartDate(SubscriptionIntegrationModel? membership, DateTime defaultDate)
        {
            return membership?.StartDate ?? defaultDate;
        }

        private string GetUpdateStatus(bool success, bool saveMode)
        {
            return saveMode
                ? (success ? "Created" : "Failed")
                : (success ? "Ready to create" : "Can not be created");
        }

        #endregion

        #region CSV

        /// <summary>
        /// Get bytes array from CSV file
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task<byte[]> GetBytesAsync(IFormFile file)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Parse CSV file to model
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private async Task<IEnumerable<BulkPatientsMigrateCsvModel>> ParseCsvAsync(IFormFile file)
        {
            var bytes = await GetBytesAsync(file);
            await using var stream = new MemoryStream(bytes);
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            return csv.GetRecords<BulkPatientsMigrateCsvModel>().ToList();
        }

        #endregion
    }
}
