using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Practices;
using WildHealth.Domain.Entities.Practices;
using WildHealth.Application.Commands.Practices;
using MediatR;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Practices
{
    public class ActivatePracticeCommandHandler : IRequestHandler<ActivatePracticeCommand, Practice>
    {
        private readonly IPracticeService _practicesService;
        private readonly ISettingsManager _settingsManager;
        private readonly ILogger<ActivatePracticeCommandHandler> _logger;

        public ActivatePracticeCommandHandler(
            IPracticeService practicesService,
            ISettingsManager settingsManager,
            ILogger<ActivatePracticeCommandHandler> logger)
        {
            _practicesService = practicesService;
            _settingsManager = settingsManager;
            _logger = logger;
        }

        public async Task<Practice> Handle(ActivatePracticeCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Activation of practice with id: {command.PracticeId} has been started.");

            var practice = await _practicesService.GetAsync(command.PracticeId);

            _settingsManager.ClearSettingsCache(command.PracticeId);

            if (practice.IsActive)
            {
                _logger.LogWarning($"Activation of practice with id: {command.PracticeId} skipped. Practice is already active.");

                return practice;
            }

            practice.Activate();

            await _practicesService.UpdateAsync(practice);

            _logger.LogInformation($"Activation of practice with id: {command.PracticeId} has been finished.");

            return practice;
        }
    }
}