using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Inputs;
using WildHealth.Application.Services.Inputs;
using Microsoft.Extensions.Logging;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Inputs
{
    public class ResetMicrobiomeInputsCommandHandler : IRequestHandler<ResetMicrobiomeInputsCommand>
    {
        private readonly IInputsService _inputsService;
        private readonly ILogger _logger;

        public ResetMicrobiomeInputsCommandHandler(
            IInputsService inputsService,
            ILogger<ResetMicrobiomeInputsCommandHandler> logger)
        {
            _inputsService = inputsService;
            _logger = logger;
        }

        public async Task Handle(ResetMicrobiomeInputsCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Resetting microbiome inputs for patient with id: {command.PatientId} has been started.");
            
            var inputs = await _inputsService.GetMicrobiomeInputsAsync(command.PatientId);

            foreach (var input in inputs)
            {
                input.Reset();
            }

            await _inputsService.UpdateMicrobiomeInputsAsync(inputs);
            
            _logger.LogInformation($"Resetting microbiome inputs for patient with id: {command.PatientId} has been finished.");
        }
    }
}