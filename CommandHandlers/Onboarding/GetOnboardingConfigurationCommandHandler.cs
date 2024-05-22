using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Onboarding;
using WildHealth.Application.Services.Onboarding;
using WildHealth.Common.Models.Onboarding;

namespace WildHealth.Application.CommandHandlers.Onboarding
{
    public class GetOnboardingConfigurationCommandHandler : IRequestHandler<GetOnboardingConfigurationCommand, OnboardingConfigurationModel>
    {
        private readonly ILogger _logger;
        private readonly IOnboardingService _onboardingService;

        public GetOnboardingConfigurationCommandHandler(
            ILogger<GetOnboardingConfigurationCommandHandler> logger,
            IOnboardingService onboardingService)
        {
            _logger = logger;
            _onboardingService = onboardingService;
        }

        public async Task<OnboardingConfigurationModel> Handle(GetOnboardingConfigurationCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Loading onboarding configuration for [PracticeId]: {command.PracticeId} has started.");
            
            var result = await _onboardingService.GetOnboardingConfiguration(command.PracticeId);
            
            _logger.LogInformation($"Loading onboarding configuration for [PracticeId]: {command.PracticeId} has finished.");

            return result;
        }
    }
}