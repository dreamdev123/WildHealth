using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Insurances;
using WildHealth.Domain.Entities.Insurances;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class RemoveInsuranceStatesCommandHandler : IRequestHandler<RemoveInsuranceStatesCommand, Insurance>
{
    private readonly IInsuranceService _insuranceService;
    private readonly IInsuranceStateService _insuranceStateService;
    private readonly ILogger<AddInsuranceStatesCommandHandler> _logger;

    public RemoveInsuranceStatesCommandHandler(
        IInsuranceService insuranceService,
        IInsuranceStateService insuranceStateService,
        ILogger<AddInsuranceStatesCommandHandler> logger)
    {
        _insuranceService = insuranceService;
        _insuranceStateService = insuranceStateService;
        _logger = logger;
    }

    public async Task<Insurance> Handle(RemoveInsuranceStatesCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Removing insurance states to insurance with id: {command.InsuranceId} has been started.");
        
        var insurance = await _insuranceService.GetByIdAsync(command.InsuranceId);

        if (insurance is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Insurance with id: {command.InsuranceId} not found.");
        }

        var states = command.States;
        
        foreach (var state in states)
        {
            try
            {
                var insuranceState = insurance.States.First(x => x.StateId == state.Id);

                await _insuranceStateService.DeleteAsync(insuranceState.GetId());
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to remove state (id: {state.Id}) to insurance (id: {insurance.GetId()}): {ex.Message}");
            }
        }
        
        insurance = await _insuranceService.GetByIdAsync(command.InsuranceId);
        
        _logger.LogInformation($"Removing insurance states to insurance with id: {command.InsuranceId} has been finished.");

        return insurance!;
    }
}