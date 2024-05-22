using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.CommandHandlers.Insurances.Flows;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Functional.Flow;
using WildHealth.Application.Materialization;
using WildHealth.Application.Services.Coverages;
using WildHealth.Application.Services.InsuranceConfigurations;
using WildHealth.Common.Models.Common;
using WildHealth.Common.Models.Insurance;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.InsuranceConfigurations;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Domain.Entities.Patients;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Domain.Enums.Integrations;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Models.Eligibility.X12;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class VerifyCoverageCommandHandler : IRequestHandler<VerifyCoverageCommand, InsuranceVerification?>
{
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly ICoveragesService _coveragesService;
    private readonly IInsuranceConfigsService _insuranceConfigsService;
    private readonly IMediator _mediator;
    private readonly MaterializeFlow _materializeFlow;
    private readonly ILogger<VerifyCoverageCommandHandler> _logger;
    private readonly IMapper _mapper;

    public VerifyCoverageCommandHandler(
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        ICoveragesService coveragesService,
        IInsuranceConfigsService insuranceConfigsService,
        IMediator mediator,
        MaterializeFlow materializeFlow,
        IMapper mapper,
        ILogger<VerifyCoverageCommandHandler> logger)
    {
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _coveragesService = coveragesService;
        _insuranceConfigsService = insuranceConfigsService;
        _mediator = mediator;
        _materializeFlow = materializeFlow;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<InsuranceVerification?> Handle(VerifyCoverageCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Verification of coverage id = {command.CoverageId} has: started");
        
        var coverage = await _coveragesService.GetAsync(command.CoverageId);

        var practiceId = coverage.User.PracticeId;
        
        var patient = coverage.User.Patient;

        var insuranceConfig = await GetConfig(practiceId: practiceId, insuranceId: coverage.InsuranceId);

        if (!insuranceConfig.SupportsEligibilityLookup)
        {
            throw new AppException(HttpStatusCode.BadRequest,
                $"Insurance does not support RTE lookup for insurance id = {coverage.InsuranceId} and practice id = {practiceId}");
        }
        
        var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(practiceId: practiceId);
        
        var pmPatientId = GetPmPatientId(user: coverage.User, vendor: pmService.Vendor);

        var eligibility = await pmService.GetEligibilityAsync(
            pmPatientId: pmPatientId,
            policyNumber: coverage.MemberId,
            serviceTypes: new[] { InsuranceConfigurationConstants.Rte.ServiceTypes.PlanCoverage },
            return271: true,
            practiceId: practiceId);

        var result = eligibility?.Result;

        var isEligible = IsEligible(result);
                                  
        if (isEligible)
        {
            _logger.LogInformation($"Verification of coverage id = {command.CoverageId} has: found eligible coverage");

            var flow = new ActivateCoverageFlow(coverage);
        
            await flow.Materialize(_materializeFlow);
            
            coverage = await UpdateCoverage(
                patient: patient,
                coverage: coverage,
                response: result);
        }
        else
        {
            _logger.LogInformation($"Verification of coverage id = {command.CoverageId} has: found ineligible coverage");

            var flow = new DeactivateCoverageFlow(coverage);
        
            await flow.Materialize(_materializeFlow);
        }
        
        var verification = await _mediator.Send(new CreateInsuranceVerificationCommand(
            patientId: patient.GetId(),
            isVerified: isEligible,
            copay: GetCopay(result),
            coverageId: coverage.GetId(),
            errorCode: GetErrorCode(result),
            raw271: eligibility?.Raw271), 
            cancellationToken);
        
        _logger.LogInformation($"Verification of coverage id = {command.CoverageId} has: finished");

        return verification;
    }

    #region private

    private string GetPmPatientId(User user, IntegrationVendor vendor)
    {
        var pmPatientId = user.GetIntegrationId(vendor, IntegrationPurposes.User.Customer);

        if (pmPatientId is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"pmPatientId not found for user id = {user.GetId()}");
        }

        return pmPatientId;
    }
    
    private async Task<Coverage> UpdateCoverage(
        Coverage coverage,
        Patient patient,
        EligibilityTransactionModel? response)
    {
        var subscriber = response?.Subscriber;
        var dependent = response?.Dependent;

        var relationship = dependent?.InsuredBenefit?.RelationshipCode 
                                     ?? subscriber?.InsuredBenefit?.RelationshipCode;

        PolicyHolderModel? policyHolder = null;
        
        if (!string.IsNullOrEmpty(relationship) && subscriber is not null)
        {
            policyHolder = new PolicyHolderModel
            {
                FirstName = subscriber.FirstName,
                LastName = subscriber.LastName,
                Relationship = relationship,
                DateOfBirth = subscriber.Birthday,
                StreetAddress1 = subscriber.Address,
                City = subscriber.City,
                State = subscriber.State,
                ZipCode = subscriber.ZipCode
            };
        }

        var command = UpdateCoverageCommand.OnBehalf(
            id: coverage.GetId(),
            patientId: patient.GetId(),
            insuranceId: coverage.InsuranceId,
            memberId: subscriber?.MemberId ?? coverage.MemberId,
            isPrimary: coverage.Priority == CoveragePriority.Primary,
            attachments: Array.Empty<AttachmentModel>(),
            policyHolder: policyHolder ?? _mapper.Map<PolicyHolderModel>(coverage.PolicyHolder));

        return await _mediator.Send(command);
    }

    private async Task<InsuranceConfig> GetConfig(int practiceId, int insuranceId)
    {
        var insuranceConfig = (await _insuranceConfigsService.GetAsync(
            practiceId: practiceId,
            insuranceId: insuranceId)).FirstOrDefault();

        if (insuranceConfig is null)
        {
            throw new AppException(HttpStatusCode.NotFound,
                $"Insurance configuration not found for insurance id = {insuranceId} and practice id = {practiceId}");
        }

        return insuranceConfig;
    }

    decimal? GetCopay(EligibilityTransactionModel? result) => result?
        .Benefits
        .FirstOrDefault(o => o.ServiceTypeCode == InsuranceConfigurationConstants.Rte.ServiceTypes.PhysicianVisitWell 
                             && o.EligibilityCode == InsuranceConfigurationConstants.Rte.BenefitEligibilityCodes.Copay)?
        .Amount;
    
    bool IsEligible(EligibilityTransactionModel? result) => result is not null
                                                            && result.Subscriber.Validation is null 
                                                            && !result.Benefits.Any(o => o.ServiceTypeCode == InsuranceConfigurationConstants.Rte.ServiceTypes.PlanCoverage
                                                                                   && o.EligibilityCode == InsuranceConfigurationConstants.Rte.BenefitEligibilityCodes.Inactive);

    private string? GetErrorCode(EligibilityTransactionModel? result)
    {
        var inactiveCoverageCode = result?.Benefits.FirstOrDefault(o => o.ServiceTypeCode == InsuranceConfigurationConstants.Rte.ServiceTypes.PlanCoverage
                                  && o.EligibilityCode == InsuranceConfigurationConstants.Rte.BenefitEligibilityCodes.Inactive)?.EligibilityCode; 
        
        return result?.Subscriber?.Validation?.ReasonCode ?? inactiveCoverageCode;
    }

    #endregion
}