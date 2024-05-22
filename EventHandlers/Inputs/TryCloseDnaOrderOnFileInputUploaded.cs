using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Orders;
using WildHealth.Application.Events.Inputs;
using WildHealth.Application.Services.Orders.Dna;
using WildHealth.Domain.Enums.Inputs;
using MediatR;

namespace WildHealth.Application.EventHandlers.Inputs
{
    public class TryCloseDnaOrderOnFileInputUploaded :  INotificationHandler<FileInputsUploadedEvent>
    {
        private readonly IDnaOrdersService _dnaOrdersService;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;
        
        public TryCloseDnaOrderOnFileInputUploaded(
            IDnaOrdersService dnaOrdersService,
            IMediator mediator,
            ILogger<TryCloseDnaOrderOnFileInputUploaded> logger)
        {
            _dnaOrdersService = dnaOrdersService;
            _mediator = mediator;
            _logger = logger;
        }
        
        public async Task Handle(FileInputsUploadedEvent @event, CancellationToken cancellationToken)
        {
            if (@event.InputType != FileInputType.DnaReport)
            {
                return;
            }
            
            _logger.LogInformation($"Closing of DNA order after manual results upload with patientId: {@event.PatientId} has been started.");

            var orders = await _dnaOrdersService.GetAsync(@event.PatientId);

            var pendingOrder = orders.FirstOrDefault(x => !x.IsResulted());

            if (pendingOrder is null)
            {
                _logger.LogInformation($"Closing of DNA order after manual results upload with patientId: {@event.PatientId} has been skipped.");
                
                return;
            }

            await _mediator.Send(new CloseDnaOrderCommand(pendingOrder.GetId(), true), cancellationToken);

            _logger.LogInformation($"Closing of DNA order after manual results upload with patientId: {@event.PatientId} has been finished.");
        }
    }
}