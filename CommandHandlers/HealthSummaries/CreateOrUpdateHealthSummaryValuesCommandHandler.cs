using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using WildHealth.Application.Commands.HealthSummaries;
using WildHealth.Application.Services.HealthSummaries;
using WildHealth.Domain.Models.Extensions;

namespace WildHealth.Application.CommandHandlers.HealthSummaries;

public class CreateOrUpdateHealthSummaryValuesCommandHandler: IRequestHandler<CreateOrUpdateHealthSummaryValuesCommand>
{
    private readonly IHealthSummaryService _healthSummaryService;
    private readonly IMediator _mediator;

    public CreateOrUpdateHealthSummaryValuesCommandHandler(
        IHealthSummaryService healthSummaryService, 
        IMediator mediator)
    {
        _healthSummaryService = healthSummaryService;
        _mediator = mediator;
    }

    public async Task Handle(CreateOrUpdateHealthSummaryValuesCommand request, CancellationToken cancellationToken)
    {
        var tryColonoscopy = await GetValuesByKeyAndUpdateOrDelete(request, "DATE_OF_LAST_SCREENING").ToTry();

        if(!tryColonoscopy.IsSuccess())
        {
            await CreateByKeyValue(request, "DATE_OF_LAST_SCREENING");
        }
       
        var tryPsa = await GetValuesByKeyAndUpdateOrDelete(request, "DATE_OF_PSA_OR_PROSTATE_EXAM").ToTry();

        if(!tryPsa.IsSuccess())
        {
            await CreateByKeyValue(request, "DATE_OF_PSA_OR_PROSTATE_EXAM");
        }
    }
    
    public async Task<Unit> GetValuesByKeyAndUpdateOrDelete(CreateOrUpdateHealthSummaryValuesCommand command, string key)
    {
        await _healthSummaryService.GetByKeyAsync(command.PatientId, key);
            
        var colonoscopy = command.Values.FirstOrDefault(x => x.Key == key);

        if (colonoscopy != null && !string.IsNullOrEmpty(colonoscopy.Value))
        {
            await _mediator.Send(new UpdateHealthSummaryCommand(command.PatientId, colonoscopy.Key, "", colonoscopy.Value));
        }
        else
        {
            await _healthSummaryService.DeleteAsync(command.PatientId, key);
        }
        
        return Unit.Value;
    }
    
    public async Task CreateByKeyValue(CreateOrUpdateHealthSummaryValuesCommand command, string key)
    {
        var colonoscopy = command.Values.FirstOrDefault(x => x.Key == key);

        if (colonoscopy != null && !string.IsNullOrEmpty(colonoscopy.Value))
        {
            await _mediator.Send(new CreateHealthSummaryCommand(command.PatientId, colonoscopy.Key, "", colonoscopy.Value));
        }
    }
}