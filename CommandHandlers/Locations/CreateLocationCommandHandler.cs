using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Locations;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Application.Services.Locations;
using WildHealth.Application.Services.Practices;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Licensing.Api.Models.Locations;
using WildHealth.Licensing.Api.Services;
using MediatR;
using WildHealth.Domain.Enums.Location;

namespace WildHealth.Application.CommandHandlers.Locations
{
    public class CreateLocationCommandHandler : IRequestHandler<CreateLocationCommand, Location>
    {
        private readonly ILocationsService _locationsService;
        private readonly IPracticeService _practicesService;
        private readonly ITransactionManager _transactionManager;
        private readonly IWildHealthLicensingApiService _licensingService;

        public CreateLocationCommandHandler(
            ILocationsService locationsService,
            IPracticeService practicesService, 
            ITransactionManager transactionManager, 
            IWildHealthLicensingApiService licensingService)
        {
            _locationsService = locationsService;
            _practicesService = practicesService;
            _transactionManager = transactionManager;
            _licensingService = licensingService;
        }

        public async Task<Location> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
        {
            var practice = await _practicesService.GetAsync(command.PracticeId);

            var location = new Location(practice)
            {
                Name = command.Name,
                Description = command.Description,
                Country = command.Country,
                City = command.City,
                State = command.State,
                ZipCode = command.ZipCode,
                StreetAddress1 = command.StreetAddress1,
                StreetAddress2 = command.StreetAddress2,
                Type = LocationType.Default
            };

            await _transactionManager.Run(async () =>
            {
                await _locationsService.CreateAsync(location);

                var originId = await CreateLocationInOriginAsync(location);
            
                location.SetOriginId(originId);
            
                await _locationsService.UpdateAsync(location);
            });
            
            return location;
        }

        #region private

        /// <summary>
        /// Creates location in origin system
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private async Task<int> CreateLocationInOriginAsync(Location location)
        {
            var result = await _licensingService.CreateLocation(new CreateLocationModel
            {
                PracticeId = location.PracticeId,
                Name = location.Name
            });

            return result.Id;
        }

        #endregion
    }
}
