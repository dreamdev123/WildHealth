using AutoMapper;
using MediatR;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using WildHealth.Application.Commands.Practices;
using WildHealth.Common.Models.Practices;
using WildHealth.Licensing.Api.Services;
using LicencingIntegrationModels = WildHealth.Licensing.Api.Models;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Application.Services.Practices;
using WildHealth.Domain.Entities.Practices;

namespace WildHealth.Application.CommandHandlers.Practices
{
    public class UpdatePracticeCommandHandler : IRequestHandler<UpdatePracticeCommand, PracticeModel>
    {
        private readonly IWildHealthLicensingApiService _licensingApiService;
        private readonly IPracticeService _practiceService;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdatePracticeCommandHandler> _logger;
        private readonly IPermissionsGuard _permissionsGuard;

        public UpdatePracticeCommandHandler(
            IPracticeService practiceService,
            IWildHealthLicensingApiService licensingApiService,
            IMapper mapper,
            ILogger<UpdatePracticeCommandHandler> logger,
            IPermissionsGuard permissionsGuard)
        {
            _licensingApiService = licensingApiService;
            _practiceService = practiceService;
            _mapper = mapper;
            _logger = logger;
            _permissionsGuard = permissionsGuard;
        }

        public async Task<PracticeModel> Handle(UpdatePracticeCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating of practice with id: {command.Id} has been started.");

            var practice = await _practiceService.GetAsync(command.Id);

            _permissionsGuard.AssertPracticePermissions(practice);

            var result = await UpdateLicensingPracticeAsync(command);

            await UpdatePracticeAsync(command, practice);

            _logger.LogInformation($"Updating of practice with id: {command.Id} has been finished.");

            return result;
        }

        #region private 

        private async Task<PracticeModel> UpdateLicensingPracticeAsync(UpdatePracticeCommand command)
        {
            var model = new LicencingIntegrationModels.Practices.UpdatePracticeModel
            {
                Id = command.Id,
                Name = command.Name,
                Email = command.Email,
                PhoneNumber = command.PhoneNumber,
                PreferredUrl = command.PreferredUrl,
                HideAddressOnSignUp = command.HideAddressOnSignUp,
                Address = _mapper.Map<LicencingIntegrationModels._Base.AddressModel>(command.Address)
            };

            var result = await _licensingApiService.UpdatePractice(model);

            return _mapper.Map<PracticeModel>(result);
        }

        private async Task UpdatePracticeAsync(UpdatePracticeCommand command, Practice practice)
        {
            practice.Name = command.Name;

            await _practiceService.UpdateAsync(practice);
        }

        #endregion
    }
}
