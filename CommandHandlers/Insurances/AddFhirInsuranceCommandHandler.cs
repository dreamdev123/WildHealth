using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Insurances;
using WildHealth.Common.Models.Insurance;
using WildHealth.Domain.Entities.Insurances;
using MediatR;
using WildHealth.Application.Extensions;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class AddFhirInsuranceCommandHandler : IRequestHandler<AddFhirInsuranceCommand, Insurance>
{
    private readonly IInsuranceService _insuranceService;
    private readonly IMediator _mediator;
    private readonly ILogger<AddFhirInsuranceCommandHandler> _logger;

    public AddFhirInsuranceCommandHandler(
        IInsuranceService insuranceService,
        IMediator mediator,
        ILogger<AddFhirInsuranceCommandHandler> logger)
    {
        _insuranceService = insuranceService;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<Insurance> Handle(AddFhirInsuranceCommand command, CancellationToken cancellationToken)
    {
        var insuranceModel = command.Insurance;
        
        var states = command.States;
        
        _logger.LogInformation($"Adding fhir insurance organization with name = {insuranceModel.Name} has: started");
        
        var insurance = await _insuranceService.GetByIdAsync(insuranceModel.Id);

        if (insurance is null)
        {
           insurance = await CreateNewInsuranceAsync(insuranceModel);
        }

        if (!states.IsNullOrEmpty())
        {
            await _mediator.Send(new AddInsuranceStatesCommand(insurance.GetId(), states), cancellationToken);
        }

        _logger.LogInformation($"Adding fhir insurance organization with fhir id = {insurance.Id} has: finished");

        return insurance;
    }
    
    #region private

    private async Task<Insurance> CreateNewInsuranceAsync(InsuranceModel insuranceModel)
    {
        var insurance = new Insurance(name: insuranceModel.Name);

        await _insuranceService.CreateAsync(insurance);

        return insurance;
    }

    #endregion
}