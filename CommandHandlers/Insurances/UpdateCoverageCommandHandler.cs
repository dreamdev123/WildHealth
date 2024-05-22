using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Shared.Exceptions;
using WildHealth.Application.Services.Coverages;
using WildHealth.Domain.Entities.Insurances;
using CoverageIntegrationModel = WildHealth.Fhir.Models.Coverages.CoverageModel;
using MediatR;
using WildHealth.Application.Services.Comments;
using WildHealth.Application.Services.Users;
using WildHealth.Domain.Constants;
using WildHealth.Domain.Entities.Comments;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.Comments;
using WildHealth.Domain.Enums.Insurance;
using WildHealth.Infrastructure.Data.Specifications;
using WildHealth.Integration.Factories.IntegrationServiceFactory;
using WildHealth.Integration.Services;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class UpdateCoverageCommandHandler : IRequestHandler<UpdateCoverageCommand, Coverage>
{
    private readonly ICoveragesService _coveragesService;
    private readonly IUsersService _usersService;
    private readonly ICommentsService _commentsService;
    private readonly IPracticeManagementIntegrationServiceFactory _practiceManagementIntegrationServiceFactory;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;

    public UpdateCoverageCommandHandler(
        ICoveragesService coveragesService,
        IUsersService usersService,
        ICommentsService commentsService,
        IPracticeManagementIntegrationServiceFactory practiceManagementIntegrationServiceFactory,
        IMediator mediator,
        IMapper mapper,
        ILogger<CreateCoverageCommandHandler> logger)
    {
        _coveragesService = coveragesService;
        _usersService = usersService;
        _commentsService = commentsService;
        _practiceManagementIntegrationServiceFactory = practiceManagementIntegrationServiceFactory;
        _mediator = mediator; 
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Coverage> Handle(UpdateCoverageCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Updating coverage for user with [Id] = {command.UserId} started.");

        try
        {
            var coverage = await _coveragesService.GetAsync(command.Id);
            
            if (coverage is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(command.UserId), command.UserId);
                
                throw new AppException(HttpStatusCode.NotFound, "[Insurance] Coverage for user does not exist.", exceptionParam);
            }
            
            var coverageChanged = CoverageChanged(command, coverage);

            if (coverageChanged)
            {
                await _mediator.Send(new DeactivateCoverageCommand(command.Id), cancellationToken);

                await _commentsService.CreateAsync(new Comment(
                    description: "Deactivating coverage due to a change in insurance or member id.",
                    commentableUniversalId: coverage.UniversalId,
                    leftByType: LeftByType.Clarity));
                
                var createNewCoverageCommand = ToCreateCoverageCommand(command);

                return await _mediator.Send(createNewCoverageCommand, cancellationToken);
            }
            
            var policyHolderChanged = PolicyHolderChanged(command, coverage.PolicyHolder);

            if (policyHolderChanged)
            {
                var user = await _usersService.GetAsync(
                    id: coverage.UserId, 
                    specification: UserSpecifications.UserWithIntegrations);
        
                var pmService = await _practiceManagementIntegrationServiceFactory.CreateAsync(user.PracticeId);

                var patientPmRef = user.GetIntegrationId(pmService.Vendor, IntegrationPurposes.User.Customer);
                
                coverage.PolicyHolder = _mapper.Map<PolicyHolder>(command.PolicyHolder);

                coverage = await UpdateCoverage(command, coverage);
            
                await UpdateCoverageInPm(
                    coverage: coverage,
                    user: user,
                    patientPmRef: patientPmRef,
                    policyHolderChanged: policyHolderChanged,
                    pmService: pmService);

                return coverage;

            }

            var priorityChanged = PriorityChanged(command, coverage);

            if (priorityChanged)
            {
                return await UpdateCoverage(command, coverage);
            }

            _logger.LogInformation($"Updating coverage for user with [Id] = {command.UserId} finished.");

            return coverage;
        }
        catch (Exception e)
        {
            _logger.LogInformation($"Updating coverage for user with [Id] = {command.UserId} failed. {e}");

            throw;
        }
    }

    #region private
    
    private CreateCoverageCommand ToCreateCoverageCommand(UpdateCoverageCommand command)
    {
        if (command.UserId.HasValue)
        {
            return CreateCoverageCommand.ByUser(
                userId: command.UserId.Value,
                insuranceId: command.InsuranceId,
                memberId: command.MemberId,
                isPrimary: command.IsPrimary,
                attachments: command.Attachments,
                policyHolder: command.PolicyHolder
            );
        }
        
        if (command.PatientId.HasValue)
        {
            return CreateCoverageCommand.OnBehalf(
                patientId: command.PatientId.Value,
                insuranceId: command.InsuranceId,
                memberId: command.MemberId,
                isPrimary: command.IsPrimary,
                attachments: command.Attachments,
                policyHolder: command.PolicyHolder
            );
        }
        
        throw new AppException(HttpStatusCode.BadRequest, "Patient is should be Greater Than than 0");
    }

    private async Task<Coverage> UpdateCoverage(UpdateCoverageCommand command, Coverage coverage)
    {
        coverage.Priority = command.IsPrimary
            ? CoveragePriority.Primary
            : CoveragePriority.Secondary;

        return await _coveragesService.UpdateAsync(coverage);
    }

    private async Task UpdateCoverageInPm(
        Coverage coverage,
        User user,
        string patientPmRef,
        bool policyHolderChanged,
        IPracticeManagementIntegrationService pmService)
    {
        var pmCoverage = await pmService.UpdateCoverageAsync(
            clarityCoverage: coverage, 
            patientPmRef: patientPmRef,
            practiceId: user.PracticeId);

        if (policyHolderChanged)
        {
            await pmService.UpdatePolicyHolderAsync(
                pmCoverage: pmCoverage,
                clarityCoverage: coverage,
                practiceId: user.PracticeId);
        }
    }

    private bool CoverageChanged(UpdateCoverageCommand command, Coverage coverage)
    {
        if (command.InsuranceId != coverage.InsuranceId)
        {
            return true;
        }

        if (command.MemberId != coverage.MemberId)
        {
            return true;
        }

        return false;
    }

    private bool PolicyHolderChanged(UpdateCoverageCommand command, PolicyHolder policyHolder)
    {
        if (command.PolicyHolder == null && policyHolder == null)
        {
            return false;
        }

        if (command.PolicyHolder == null && policyHolder != null)
        {
            return true;
        }

        if (command.PolicyHolder != null && policyHolder == null)
        {
            return true;
        }

        return
            command.PolicyHolder?.FirstName != policyHolder?.FirstName ||
            command.PolicyHolder?.LastName != policyHolder?.LastName ||
            command.PolicyHolder?.DateOfBirth != policyHolder?.DateOfBirth ||
            command.PolicyHolder?.Relationship != policyHolder?.Relationship ||
            command.PolicyHolder?.StreetAddress1 != policyHolder?.StreetAddress1 ||
            command.PolicyHolder?.StreetAddress2 != policyHolder?.StreetAddress2 ||
            command.PolicyHolder?.City != policyHolder?.City ||
            command.PolicyHolder?.State != policyHolder?.State ||
            command.PolicyHolder?.ZipCode != policyHolder?.ZipCode;
    }

    private bool PriorityChanged(UpdateCoverageCommand command, Coverage coverage)
    {
        switch (command.IsPrimary)
        {
            case true when coverage.Priority == CoveragePriority.Primary:
            case false when coverage.Priority != CoveragePriority.Primary:
                return false;
            default:
                return true;
        }
    }

    #endregion
}