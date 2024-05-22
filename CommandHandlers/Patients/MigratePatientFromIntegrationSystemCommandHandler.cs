using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Patients;
using WildHealth.Application.Commands.Users;
using WildHealth.Application.Services.Patients;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Shared.Enums;
using WildHealth.Common.Models.Users;
using WildHealth.Domain.Entities.Payments;
using WildHealth.Application.Services.Agreements;
using WildHealth.Application.Services.Subscriptions;
using WildHealth.Application.Services.PaymentPlans;
using WildHealth.Application.Events.Patients;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Common.Models.Patients;
using WildHealth.Application.Services.Auth;
using WildHealth.Application.Services.Locations;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Integration.Models.Patients;
using WildHealth.Integration.Models.Subscriptions;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;
using WildHealth.Application.Utils.PatientCreator;
using MediatR;
using WildHealth.Application.CommandHandlers.Subscriptions.Flows;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Domain.Enums.Integrations;

namespace WildHealth.Application.CommandHandlers.Patients
{
    public class MigratePatientFromIntegrationSystemCommandHandler : IRequestHandler<MigratePatientFromIntegrationSystemCommand, MigratePatientFromIntegrationSystemResultModel>
    {
        private const string DefaultUserNote = "patient_migrated_from_integration_system";
        
        private readonly IAuthService _authService;
        private readonly IPatientCreator _patientCreator;
        private readonly IPatientsService _patientsService;
        private readonly ILocationsService _locationsService;
        private readonly IAgreementsService _agreementsService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentPlansService _paymentPlansService;
        private readonly ITransactionManager _transactionManager;
        private readonly IIntegrationServiceFactory _integrationServiceFactory;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        private readonly MaterializeFlow _materialization;

        public MigratePatientFromIntegrationSystemCommandHandler(
            IAuthService authService,
            IPatientCreator patientCreator,
            IPatientsService patientsService,
            ILocationsService locationsService,
            IAgreementsService agreementsService,
            ISubscriptionService subscriptionService,
            IPaymentPlansService paymentPlansService,
            ITransactionManager transactionManager,
            IIntegrationServiceFactory integrationServiceFactory,
            IMediator mediator,
            ILogger<MigratePatientFromIntegrationSystemCommandHandler> logger, 
            MaterializeFlow materialization)
        {
            _authService = authService;
            _patientCreator = patientCreator;
            _patientsService = patientsService;
            _locationsService = locationsService;
            _agreementsService = agreementsService;
            _subscriptionService = subscriptionService;
            _paymentPlansService = paymentPlansService;
            _transactionManager = transactionManager;
            _integrationServiceFactory = integrationServiceFactory;
            _mediator = mediator;
            _logger = logger;
            _materialization = materialization;
            _transactionManager = transactionManager;
        }

        public async Task<MigratePatientFromIntegrationSystemResultModel> Handle(MigratePatientFromIntegrationSystemCommand command, CancellationToken cancellationToken)
        {
            var result = new MigratePatientFromIntegrationSystemResultModel();

            var integrationService = await _integrationServiceFactory.CreateAsync(command.PracticeId);

            var originPatients = await integrationService.MatchPatientsAsync(
                firstName: command.FirstName,
                lastName: command.LastName,
                middleName: command.MiddleName,
                email: command.Email);

            originPatients = Filter(
                originPatients: originPatients,
                email: command.Email,
                firstName: command.FirstName,
                lastName: command.LastName,
                middleName: command.MiddleName);
            
            if (!originPatients.Any())
            {
                _logger.LogWarning($"Migrating patient from Integration System skipped, patient with [Email] = {command.Email} does not exist in Integration System.");
                return result;
            }

            var location = await _locationsService.GetDefaultLocationAsync(command.PracticeId);
            
            PatientIntegrationModel? currentPatient = null;

            foreach (var originPatient in originPatients)
            {
                await using var transaction = _transactionManager.BeginTransaction();

                try
                {
                    currentPatient = originPatient;
                
                    await AssertPatientDoesNotExistAsync(originPatient.Id, originPatient.Email);
            
                    var patient = await CreatePatientAsync(originPatient, location, cancellationToken);

                    await _patientsService.UpdatePatientOnBoardingStatusAsync(patient, command.Status);

                    await _patientsService.LinkPatientWithIntegrationSystemAsync(patient, originPatient.Id, integrationService.IntegrationVendor);

                    var (subscription, membership) = await CreateSubscriptionAsync(
                        integrationService: integrationService,
                        patient: patient,
                        createdAt: originPatient.CreatedAt,
                        paymentPlanId: command.PaymentPlanId,
                        paymentPeriodId: command.PaymentPeriodId,
                        paymentPriceId: command.PaymentPriceId,
                        practiceId: command.PracticeId);
                
                    if (membership != null)
                    {
                        await new MarkSubscriptionAsPaidFlow(
                            subscription, 
                            membership.Id, 
                            integrationService.IntegrationVendor).Materialize(_materialization);
                    }

                    if (command.ConfirmAgreements)
                    {
                        await _agreementsService.CreateUnsignedConfirmationsAsync(patient, subscription!);
                    }
                    
                    await transaction.CommitAsync(cancellationToken);
            
                    result.SuccessfulPatients.Add(new MigratedPatientFromIntegrationSystemModel
                    {
                        Id = originPatient.Id,
                        Email = originPatient.Email,
                        Comment = "Success"
                    });

                    var migratedEvent = new PatientMigratedFromIntegrationSystemEvent(
                        patient: patient,
                        subscription: subscription!,
                        dpc: command.IsDPC);

                    await _mediator.Publish(migratedEvent, cancellationToken);
                }
                catch (Exception ex)
                {
                    result.FailedPatients.Add(new MigratedPatientFromIntegrationSystemModel
                    {
                        Id = currentPatient?.Id,
                        Email = currentPatient?.Email,
                        Comment = ex.Message
                    });
            
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError($"Error during import from Integration System patient with [Id] = {currentPatient}: {ex.Message}");
                }
            }

            return result;
        }

        #region private

        /// <summary>
        /// Creates patient in the system based on origin patient data
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
                UserType.Patient,
                practiceId: location.PracticeId,
                billingAddress: address,
                shippingAddress: address,
                isVerified: false,
                isRegistrationCompleted: true,
                note: DefaultUserNote
            );

            var user = await _mediator.Send(createUserCommand, cancellationToken);

            var patient = await _patientCreator.Create(user, null, location);

            return await _patientsService.CreatePatientAsync(patient);
        }

        /// <summary>
        /// Creates patient in the system based on origin patient data
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
            var membership = await GetLastSubscriptionAsync(integrationService, patient);
            var startDate = GetSubscriptionStartDate(membership, createdAt);
            var endDate = GetSubscriptionEndDate(membership, paymentPeriod.PeriodInMonths, startDate);
            var subscription = await _subscriptionService.CreatePastSubscriptionAsync(
                patient: patient,
                paymentPrice: paymentPrice,
                startDate: startDate,
                endDate: endDate);

            return (subscription, membership);
        }

        /// <summary>
        /// Asserts patient does not exist in the system
        /// </summary>
        /// <param name="integrationId"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task AssertPatientDoesNotExistAsync(string integrationId, string email)
        {
            try
            {
                var existingPatient = await _patientsService.GetByIntegrationIdAsync(integrationId, IntegrationVendor.Hint);
                if (existingPatient != null)
                {
                    throw new AppException(HttpStatusCode.BadRequest, $"Patient with INTEGRATION ID: {integrationId} already exists in the system.");
                }

                if (await _authService.CheckIfEmailExistsAsync(email))
                {
                    throw new AppException(HttpStatusCode.BadRequest, $"User with email: {email} already exists.");
                }
            }
            catch (AppException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // If App Exception was risen with status code: 404 - skip it and continue as expected result
            }
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
            var activeMembership = subscriptions.FirstOrDefault(x => x.Status == "active");
            if (activeMembership != null)
            {
                return activeMembership;
            }

            var lastMembership = subscriptions
                .OrderByDescending(x => x.EndDate)
                .FirstOrDefault();

            return lastMembership?.EndDate != null 
                ? lastMembership 
                : null;
        }
        
        /// <summary>
        /// Calculates and returns subscription End date based on membership
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="periodInMonths"></param>
        /// <param name="startDate"></param>
        /// <returns></returns>
        private static DateTime GetSubscriptionEndDate(SubscriptionIntegrationModel? subscription, int periodInMonths, DateTime startDate)
        {
            if (subscription is null)
            {
                var endDate = DateTime.UtcNow.AddDays(-1);

                if (endDate.Date < startDate.Date)
                {
                    endDate = startDate;
                }

                return endDate;
            }
            
            return subscription.EndDate ?? subscription.StartDate.AddMonths(periodInMonths);
        }

        /// <summary>
        /// Calculates and returns subscription start date based on membership
        /// </summary>
        /// <param name="subscription"></param>
        /// <param name="defaultDate"></param>
        /// <returns></returns>
        private static DateTime GetSubscriptionStartDate(SubscriptionIntegrationModel? subscription, DateTime defaultDate)
        {
            return subscription?.StartDate ?? defaultDate;
        }

        /// <summary>
        /// Filters patients from Integration system
        /// </summary>
        /// <param name="originPatients"></param>
        /// <param name="email"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="middleName"></param>
        /// <returns></returns>
        private static PatientIntegrationModel[] Filter(
            IEnumerable<PatientIntegrationModel> originPatients, 
            string email, 
            string firstName, 
            string lastName,
            string middleName)
        {
            return originPatients
                .Where(x => x.Email?.Equals(email, StringComparison.OrdinalIgnoreCase) ?? string.IsNullOrEmpty(email))
                .Where(x => x.FirstName?.Equals(firstName, StringComparison.OrdinalIgnoreCase) ?? string.IsNullOrEmpty(firstName))
                .Where(x => x.LastName?.Equals(lastName, StringComparison.OrdinalIgnoreCase) ?? string.IsNullOrEmpty(lastName))
                .Where(x => x.MiddleName?.Equals(middleName, StringComparison.OrdinalIgnoreCase) ?? string.IsNullOrEmpty(middleName))
                .ToArray();
        }

        #endregion
    }
}