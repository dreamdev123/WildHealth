using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Businesses;
using WildHealth.Application.Services.Practices;
using WildHealth.Common.Models.Businesses;
using WildHealth.Licensing.Api.Services;
using WildHealth.Shared.Exceptions;
using WildHealth.Shared.Utils.AuthTicket;
using LicencingIntegrationModels = WildHealth.Licensing.Api.Models;


namespace WildHealth.Application.CommandHandlers.Business
{
    public class UpdateBusinessCommandHandler : IRequestHandler<UpdateBusinessCommand, BusinessModel>
    {
        private readonly IPracticeService _practiceService;
        private readonly IWildHealthLicensingApiService _licensingApiService;
        private readonly IMapper _mapper;
        private readonly IAuthTicket _authTicket;
        private readonly ILogger<UpdateBusinessCommandHandler> _logger;

        public UpdateBusinessCommandHandler(
            IPracticeService practiceService,
            IWildHealthLicensingApiService licensingApiService,
            IMapper mapper,
            IAuthTicket authTicket,
            ILogger<UpdateBusinessCommandHandler> logger)
        {
            _practiceService = practiceService;
            _licensingApiService = licensingApiService;
            _mapper = mapper;
            _authTicket = authTicket;
            _logger = logger;
        }

        public async Task<BusinessModel> Handle(UpdateBusinessCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating of practice with id: {command.Id} has been started.");

            await AssertBusinessPermissionsAsync(command.Id);

            var result = await UpdateBusinessAsync(command);

            _logger.LogInformation($"Updating of business with id: {command.Id} has been finished.");

            return result;
        }

        #region private

        private async Task<BusinessModel> UpdateBusinessAsync(UpdateBusinessCommand command)
        {
            var model = new LicencingIntegrationModels.Business.UpdateBusinessModel
            {
                Id = command.Id,
                Name = command.Name,
                PhoneNumber = command.PhoneNumber,
                TaxIdNumber = command.TaxIdNumber,
                Address = _mapper.Map<LicencingIntegrationModels._Base.AddressModel>(command.Address),
                BillingAddress = _mapper.Map<LicencingIntegrationModels._Base.AddressModel>(command.BillingAddress)
            };

            var result = await _licensingApiService.UpdateBusiness(model);

            return _mapper.Map<BusinessModel>(result);
        }

        private async Task AssertBusinessPermissionsAsync(int businessId)
        {
            var practice = await _practiceService.GetAsync(_authTicket.GetPracticeId());

            if (practice.BusinessId != businessId)
            {
                throw new AppException(HttpStatusCode.Forbidden, "You have no permission for this business.");
            }
        }
        #endregion
    }
}
