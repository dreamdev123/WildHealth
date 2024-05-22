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
using WildHealth.Domain.Entities.Payments;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class AddInsuranceStatesCommandHandler : IRequestHandler<AddInsuranceStatesCommand, Insurance>
{
    private readonly IInsuranceService _insuranceService;
    private readonly IInsuranceStateService _insuranceStateService;
    private readonly IGeneralRepository<PaymentPlan> _paymentPlansRepository;
    private readonly IGeneralRepository<PaymentPlanInsuranceState> _paymentPlanInsuranceStatesRepository;
    private readonly ILogger<AddInsuranceStatesCommandHandler> _logger;
    private readonly ITransactionManager _transactionManager;

    public AddInsuranceStatesCommandHandler(
        IInsuranceService insuranceService,
        IInsuranceStateService insuranceStateService,
        IGeneralRepository<PaymentPlan> paymentPlansRepository,
        IGeneralRepository<PaymentPlanInsuranceState> paymentPlanInsuranceStatesRepository,
        ILogger<AddInsuranceStatesCommandHandler> logger,
        ITransactionManager transactionManager)
    {
        _insuranceService = insuranceService;
        _paymentPlansRepository = paymentPlansRepository;
        _paymentPlanInsuranceStatesRepository = paymentPlanInsuranceStatesRepository;
        _insuranceStateService = insuranceStateService;
        _logger = logger;
        _transactionManager = transactionManager;
    }

    public async Task<Insurance> Handle(AddInsuranceStatesCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Adding insurance states to insurance with id: {command.InsuranceId} has started.");
        
        var insurance = await _insuranceService.GetByIdAsync(command.InsuranceId);

        var paymentPlans = _paymentPlansRepository
            .All()
            .Include(o => o.PaymentPlanInsuranceStates)
            .Where(o => o.PaymentPlanInsuranceStates.Any()).ToList();
            
        if (insurance is null)
        {
            throw new AppException(HttpStatusCode.NotFound, $"Insurance with id: {command.InsuranceId} not found.");
        }

        var states = command.States;
        
        foreach (var state in states)
        {
            await using var transaction = _transactionManager.BeginTransaction();
            try
            {
                if (insurance.States.All(x => x.StateId != state.Id))
                {
                    var insuranceState = new InsuranceState(insurance.GetId(), state.Id);
                    
                    await _insuranceStateService.CreateAsync(insuranceState);
                }

                // Also want to add this state to all paymentPlans that currently have at least one state configured to accept insurance
                var existingPlansForState = await _paymentPlanInsuranceStatesRepository
                    .All()
                    .Where(o => o.StateId == state.Id)
                    .Select(o => o.PaymentPlan)
                    .ToArrayAsync();

                var missingPlansForState = paymentPlans.Except(existingPlansForState);

                foreach (var missingPlanForState in missingPlansForState)
                {
                    await _paymentPlanInsuranceStatesRepository.AddAsync(new PaymentPlanInsuranceState(
                        paymentPlanId: missingPlanForState.GetId(),
                        stateId: state.Id
                    ));

                    await _paymentPlanInsuranceStatesRepository.SaveAsync();
                }
                

                await transaction.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                
                _logger.LogError($"Failed to add state (id: {state.Id}) to insurance (id: {insurance.GetId()}): {ex.Message}");
            }
        }
        
        insurance = await _insuranceService.GetByIdAsync(command.InsuranceId);
        
        _logger.LogInformation($"Adding insurance states to insurance with id: {command.InsuranceId} has finished.");

        return insurance!;
    }
}