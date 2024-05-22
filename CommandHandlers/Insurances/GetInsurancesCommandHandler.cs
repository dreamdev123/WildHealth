using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Insurances;
using WildHealth.Application.Services.Insurances;
using WildHealth.Domain.Entities.Insurances;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Insurances;

public class GetInsurancesCommandHandler : IRequestHandler<GetInsurancesCommand, Insurance[]>
{
    private readonly IInsuranceService _insuranceService;
    
    public GetInsurancesCommandHandler(IInsuranceService insuranceService)
    {
        _insuranceService = insuranceService;
    }

    public async Task<Insurance[]> Handle(GetInsurancesCommand command, CancellationToken cancellationToken)
    {
        var stateId = command.StateId;
        var age = command.Age;
        
        if (stateId.HasValue)
        {
            return await _insuranceService.GetByStateAsync(stateId.Value, age); 
        }

        return await _insuranceService.GetAllAsync();
    }
}