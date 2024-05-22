using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Integrations;
using WildHealth.Application.Services.Users;
using WildHealth.Common.Models.Insurance;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Patients;
using WildHealth.Application.Services.States;
using WildHealth.Domain.Entities.Address;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Services.Coverages;
using WildHealth.Application.Services.Insurances;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Enums.Insurance;
using MediatR;
using WildHealth.Domain.Models.Extensions;
using WildHealth.OpenPM.Clients.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances
{
    public class CreateCoverageCommandHandler : IRequestHandler<CreateCoverageCommand, Coverage>
    {
        private const string SelfRelation = "SELF";
        
        private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
        private readonly IIntegrationsService _integrationsService;
        private readonly ITransactionManager _transactionManager;
        private readonly ICoveragesService _coveragesService;
        private readonly IInsuranceService _insuranceService;
        private readonly IPatientsService _patientsService;
        private readonly IUsersService _usersService;
        private readonly ILocationsService _locationsService;
        private readonly IStatesService _statesService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;

        public CreateCoverageCommandHandler(
            IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
            IIntegrationsService integrationsService, 
            ITransactionManager transactionManager,
            ICoveragesService coveragesService,
            IInsuranceService insuranceService,
            IPatientsService patientsService,
            IUsersService usersService,
            ILocationsService locationsService,
            IStatesService statesService,
            IMediator mediator,
            ILogger<CreateCoverageCommandHandler> logger)
        {
            _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
            _integrationsService = integrationsService;
            _transactionManager = transactionManager;
            _coveragesService = coveragesService;
            _insuranceService = insuranceService;
            _patientsService = patientsService;
            _usersService = usersService;
            _locationsService = locationsService;
            _statesService = statesService;
            _mediator = mediator;
            _logger = logger;
        }

        public async Task<Coverage> Handle(CreateCoverageCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Creating coverage for user with [Id] = {command.UserId} started.");

            var user = await GetUserAsync(command);

            var insurance = await _insuranceService.GetByIdAsync(command.InsuranceId);
            
            var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(user.PracticeId);

            var organizationId = insurance!.GetIntegrationId(pmService.Vendor);

            var coverage = new Coverage(
                user: user,
                insurance: insurance,
                memberId: command.MemberId,
                priority: command.IsPrimary
                    ? CoveragePriority.Primary
                    : CoveragePriority.Secondary,
                policyHolder: command.PolicyHolder is null
                    ? null
                    : new PolicyHolder
                    {
                        FirstName = command.PolicyHolder.FirstName,
                        LastName = command.PolicyHolder.LastName,
                        Relationship = command.PolicyHolder.Relationship,
                        DateOfBirth = command.PolicyHolder.DateOfBirth,
                    }
            );
          
            var patientId = user.GetIntegration(IntegrationVendor.OpenPm, IntegrationPurposes.User.Customer)?.Value;

            var pmCoverageId = string.Empty;
            
            if (string.IsNullOrEmpty(patientId))
            {
                patientId = await CreatePatientInOpenPmAsync(
                    user: user,
                    pmService: pmService);
            }

            await _transactionManager.Run(async () =>
            {
                var policyHolderId = string.Empty;

                if (command.PolicyHolder is not null)
                {
                    policyHolderId = await CreatePolicyHolderAsync(
                        user: user, 
                        policyHolder: command.PolicyHolder, 
                        patientId: patientId,
                        pmService: pmService);
                }

                try
                {
                    pmCoverageId = await CreateCoverageAsync(
                        user: user,
                        patientId: patientId,
                        policyHolderId: policyHolderId,
                        organizationId: organizationId,
                        command: command,
                        pmService: pmService
                    );
                    
                    if (command.IsPrimary)
                    {
                        await DowngradeExistingCoverageAsync(user.GetId());
                    }

                    await CreateOpenPmAccountAsync(
                        patientId: patientId,
                        coverageId: pmCoverageId,
                        user: user,
                        pmService: pmService
                    );
                }
                catch (OpenPMException ex)
                {
                    _logger.LogError($"Problem creating coverage in [PmVendor] = {pmService.Vendor} - {ex}");
                    
                    // Ok continuing on, insurance will not be in OpenPM but patient can still sign up
                    // https://wildhealth.atlassian.net/browse/CLAR-5798
                }

                coverage = await _coveragesService.CreateAsync(coverage);

                if (!string.IsNullOrEmpty(pmCoverageId))
                {
                    await CreateIntegration(coverage, pmCoverageId, pmService);
                }

                _logger.LogInformation($"Creating coverage for user with [Id] = {command.UserId} finished.");
            });
            
            if (command.Attachments is not null && command.Attachments.Any())
            {
                var uploadAttachmentsCommand = UploadInsuranceCommand.ByUser(
                    userId: user.GetId(),
                    coverageId: coverage.GetId().ToString(),
                    attachments: command.Attachments
                );

                await _mediator.Send(uploadAttachmentsCommand, cancellationToken);
            }
            
            // If patient creating coverage during the checkout flow his patient record is not created yet.
            // In this case, we want to skip this logic because it works only for registered patients without insurance.
            if (user.IsRegistrationCompleted)
            {
                var patient = await _patientsService.GetByUserIdAsync(user.GetId());

                if (CanSwitchToInsurance(patient))
                {
                    var turnOnInsuranceCommand = new TurnOnInsuranceCommand(patient.GetId());

                    await _mediator.Send(turnOnInsuranceCommand, cancellationToken);
                }
                else
                {
                    _logger.LogInformation($"Skipped turning on insurance for user with [Id] = {command.UserId}");
                }
            }

            return coverage;
        }

        #region private

        /// <summary>
        /// Downgrade existing coverages to secondary priority
        /// </summary>
        /// <param name="userId"></param>
        private async Task DowngradeExistingCoverageAsync(int userId)
        {
            var existingCoverages = await _coveragesService.GetPrimaryAsync(userId);

            foreach (var coverage in existingCoverages)
            {
                coverage.Priority = CoveragePriority.Secondary;
                await _coveragesService.UpdateAsync(coverage);
            }
        }
        
        /// <summary>
        /// Fetches and returns user 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        private async Task<User> GetUserAsync(CreateCoverageCommand command)
        {
            if (command.UserId.HasValue)
            {
                var specification = UserSpecifications.UserWithIntegrations;

                return await _usersService.GetAsync(command.UserId.Value, specification);
            }

            if (command.PatientId.HasValue)
            {
                var specification = PatientSpecifications.PatientWithIntegrations;

                var patient = await _patientsService.GetByIdAsync(command.PatientId.Value, specification);

                return patient.User;
            }

            throw new AppException(HttpStatusCode.BadRequest, "Patient is should be Greater Than than 0");
        }
        
        /// <summary>
        /// Creates patient in Open PM and stores integration
        /// </summary>
        /// <param name="user"></param>
        /// <param name="pmService"></param>
        private async Task<string> CreatePatientInOpenPmAsync(User user, IPracticeManagementIntegrationService pmService)
        {
            var location = await _locationsService.GetDefaultLocationAsync(user.PracticeId);
            var fhirLocationId = location.GetIntegrationId(IntegrationVendor.OpenPm);
            
            var openPmState = await GetOpenPmState(user);

            var patientId = await pmService.CreatePatientAsync(
                firstName: user.FirstName,
                lastName: user.LastName,
                birthday: user.Birthday ?? DateTime.Now,
                gender: user.Gender,
                phoneNumber: user.PhoneNumber,
                email: user.Email ?? string.Empty,
                streetAddress1: user.BillingAddress?.StreetAddress1 ?? string.Empty,
                streetAddress2: user.BillingAddress?.StreetAddress2 ?? string.Empty,
                city: user.BillingAddress?.City ?? string.Empty,
                state: openPmState,
                zipCode: user.BillingAddress?.ZipCode ?? string.Empty,
                fhirLocationId: fhirLocationId,
                practiceId: user.PracticeId
            );

            var patientIntegration = new UserIntegration(
                vendor: IntegrationVendor.OpenPm,
                purpose: IntegrationPurposes.User.Customer,
                value: patientId,
                user: user
            );

            await _integrationsService.CreateAsync(patientIntegration);

            return patientId;
        }
        
        private async Task<string> GetOpenPmState(User user)
        {
            var userState = user.BillingAddress?.State;

            if (string.IsNullOrEmpty(userState))
            {
                return string.Empty;
            }

            if (userState.Length == 2)
            {
                return userState;
            }

            var state = await GetStateEntity(userState);

            return state?.Abbreviation ?? string.Empty;
        }

        private async Task<State?> GetStateEntity(string stateString)
        {
            try
            {
                return await _statesService.GetByName(stateString);
            }
            catch (AppException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        /// <summary>
        /// Creates policy holder in Open PM and returns external id
        /// </summary>
        /// <param name="user"></param>
        /// <param name="policyHolder"></param>
        /// <param name="patientId"></param>
        /// <param name="pmService"></param>
        /// <returns></returns>
        private async Task<string> CreatePolicyHolderAsync(User user, PolicyHolderModel policyHolder, string patientId, IPracticeManagementIntegrationService pmService)
        {
            var policyHolderId = await pmService.CreatePolicyHolderAsync(
                firstName: policyHolder.FirstName,
                lastName: policyHolder.LastName,
                patientId: patientId,
                practiceId: user.PracticeId,
                relationship: policyHolder.Relationship
            );
            
            return policyHolderId;
        }

        /// <summary>
        /// Creates coverage in Open PM and returns integration Id
        /// </summary>
        /// <param name="user"></param>
        /// <param name="patientId"></param>
        /// <param name="policyHolderId"></param>
        /// <param name="organizationId"></param>
        /// <param name="command"></param>
        /// <param name="pmService"></param>
        /// <returns></returns>
        private async Task<string> CreateCoverageAsync(
            User user, 
            string patientId, 
            string policyHolderId,
            string organizationId,
            CreateCoverageCommand command, 
            IPracticeManagementIntegrationService pmService)
        {
            var coverageId = await pmService.CreateCoverageAsync(
                patientId: patientId,
                memberId: command.MemberId,
                policyHolderId: policyHolderId, 
                policyHolderRelationshipCode: command.PolicyHolder?.Relationship ?? SelfRelation,
                organizationId: organizationId,
                practiceId: user.PracticeId
            );
            
            return coverageId;
        }

        /// <summary>
        /// Creates Open PM account
        /// </summary>
        /// <param name="patientId"></param>
        /// <param name="coverageId"></param>
        /// <param name="user"></param>
        /// <param name="pmService"></param>
        private async Task CreateOpenPmAccountAsync(
            string patientId,
            string coverageId,
            User user, 
            IPracticeManagementIntegrationService pmService)
        {
            await pmService.CreateAccountAsync(
                patientId: patientId,
                fullName: user.GetFullname(),
                coverageIds: new[] { coverageId },
                practiceId: user.PracticeId
            );
        }

        /// <summary>
        /// Returns if patient can be switched to insurance
        /// </summary>
        /// <param name="patient"></param>
        /// <returns></returns>
        private bool CanSwitchToInsurance(Patient patient)
        {
            if (patient.CurrentSubscription is null)
            {
                return false;
            }

            if (patient.CurrentSubscription.PaymentPrice.IsInsurance())
            {
                return false;
            }

            return true;
        }

        private async Task CreateIntegration(Coverage coverage, string pmCoverageId, IPracticeManagementIntegrationService pmService)
        {
            var integration = new CoverageIntegration(
                vendor: pmService.Vendor,
                purpose: IntegrationPurposes.User.Coverage,
                value: pmCoverageId,
                coverage: coverage);

            await _integrationsService.CreateAsync(integration);
        }

        #endregion
    }
}