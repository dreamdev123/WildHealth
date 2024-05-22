using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Services.Practices;
using WildHealth.Application.Commands.Practices;
using MediatR;

namespace WildHealth.Application.CommandHandlers.Practices
{
    public class InvalidateCachePracticeCommandHandler : IRequestHandler<InvalidateCachePracticeCommand>
    {
        private readonly IPracticeService _practicesService;
        private readonly ILogger<InvalidateCachePracticeCommandHandler> _logger;

        public InvalidateCachePracticeCommandHandler(
            IPracticeService practicesService,
            ILogger<InvalidateCachePracticeCommandHandler> logger)
        {
            _practicesService = practicesService;
            _logger = logger;
        }

        public Task Handle(InvalidateCachePracticeCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"InvalidateCache of practice with id: {command.PracticeId} has been started.");

            _practicesService.InvalidateCache(command.PracticeId);

            return Task.FromResult(Unit.Value);
        }
    }
}