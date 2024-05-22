using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Practices;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Application.Commands.Practices;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Practices
{
    public class DeactivatePracticeCommandHandler : IRequestHandler<DeactivatePracticeCommand, Practice>
    {
        private readonly IPracticeService _practicesService;
        private readonly ILogger<DeactivatePracticeCommandHandler> _logger;

        public DeactivatePracticeCommandHandler(
            IPracticeService practicesService,
            ILogger<DeactivatePracticeCommandHandler> logger)
        {
            _practicesService = practicesService;
            _logger = logger;
        }

        public async Task<Practice> Handle(DeactivatePracticeCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Deactivation of practice with id: {command.PracticeId} has been started.");

            var practice = await _practicesService.GetAsync(command.PracticeId);

            if (!practice.IsActive)
            {
                _logger.LogWarning($"Deactivation of practice with id: {command.PracticeId} skipped. Practice is not active already.");

                return practice;
            }

            practice.Deactivate();

            await _practicesService.UpdateAsync(practice);

            _logger.LogInformation($"Deactivation of practice with id: {command.PracticeId} has been finished.");

            return practice;
        }
    }
}