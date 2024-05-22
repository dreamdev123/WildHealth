using AutoMapper;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Practices;
using WildHealth.Common.Models.Practices;
using LicencingIntegrationModels = WildHealth.Licensing.Api.Models;
using WildHealth.Application.Services.Practices;

namespace WildHealth.Application.CommandHandlers.Practices
{
    public class GetPracticeCommandHandler : IRequestHandler<GetPracticeCommand, List<PracticeModel>>
    {
        private readonly IPracticeService _practiceService;
        private readonly IMapper _mapper;
        private readonly ILogger _logger;

        public GetPracticeCommandHandler(
            IPracticeService practiceService,
            IMapper mapper,
            ILogger<GetPracticeCommandHandler> logger)
        {
            _practiceService = practiceService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<PracticeModel>> Handle(GetPracticeCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Getting of all practice has been started.");

            var practices = await _practiceService.GetAllAsync();

            _logger.LogInformation($"Getting of all practice has been finished.");

            return _mapper.Map<List<PracticeModel>>(practices);
        }
    }
}
