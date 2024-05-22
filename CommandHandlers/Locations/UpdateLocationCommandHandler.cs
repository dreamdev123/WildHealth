using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using WildHealth.Application.Commands.Locations;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Application.Services.Locations;
using WildHealth.Shared.Utils.AuthTicket;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Shared.Data.Managers.TransactionManager;
using WildHealth.Shared.Exceptions;
using WildHealth.Licensing.Api.Services;
using WildHealth.Licensing.Api.Models.Locations;
using MediatR;
using WildHealth.Application.Services.Practices;
using WildHealth.Settings;

namespace WildHealth.Application.CommandHandlers.Locations
{
    public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, Location>
    {
        private readonly ILocationsService _locationsService;
        private readonly IPermissionsGuard _permissionsGuard;
        private readonly ITransactionManager _transactionManager;
        private readonly IWildHealthLicensingApiService _licensingService;
        private readonly ISettingsManager _settingsManager;
        private readonly IPracticeService _practiceService;
        private readonly IAuthTicket _authTicket;

        public UpdateLocationCommandHandler(
            ILocationsService locationsService,
            IPermissionsGuard permissionsGuard,
            ITransactionManager transactionManager,
            IWildHealthLicensingApiService licensingService,
            IAuthTicket authTicket,
            ISettingsManager settingsManager,
            IPracticeService practiceService)
        {
            _locationsService = locationsService;
            _permissionsGuard = permissionsGuard;
            _transactionManager = transactionManager;
            _licensingService = licensingService;
            _authTicket = authTicket;
            _settingsManager = settingsManager;
            _practiceService = practiceService;
        }

        public async Task<Location> Handle(UpdateLocationCommand command, CancellationToken cancellationToken)
        {
            var location = await _locationsService.GetByIdAsync(command.Id, _authTicket.GetPracticeId());

            _permissionsGuard.AssertPermissions(location);
            
            await _transactionManager.Run(async () =>
            {
                location.Name = command.Name;
                location.Description = command.Description;
                location.Country = command.Country;
                location.City = command.City;
                location.State = command.State;
                location.ZipCode = command.ZipCode;
                location.StreetAddress1 = command.StreetAddress1;
                location.StreetAddress2 = command.StreetAddress2;

                UpdateCareCoordinator(command, location);
                
                await UpdateLocationInOriginAsync(location);
                
                await _locationsService.UpdateAsync(location);
            });

            return location;
        }
        
        #region private

        /// <summary>
        /// Updates care coordinator
        /// </summary>
        /// <param name="command"></param>
        /// <param name="location"></param>
        /// <exception cref="AppException"></exception>
        private void UpdateCareCoordinator(UpdateLocationCommand command, Location location)
        {
            var currentCareCoordinator = location.Employees.FirstOrDefault(i => i.IsCareCoordinator);
            
            currentCareCoordinator?.SetIsCareCoordinator(false);

            if (command.CareCoordinatorId is null)
            {
                return;
            }
            
            var employee = location.Employees.FirstOrDefault(i => i.EmployeeId == command.CareCoordinatorId);

            if (employee is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(command.CareCoordinatorId), command.CareCoordinatorId);
                throw new AppException(HttpStatusCode.BadRequest, "Care Coordinator does not exist.", exceptionParam);
            }
            
            employee.SetIsCareCoordinator();
        }

        
        /// <summary>
        /// Updates location in origin system
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private Task UpdateLocationInOriginAsync(Location location)
        {
            if (location.OriginId is null)
            {
                return Task.CompletedTask;
            }

            _settingsManager.ClearSettingsCache(location.PracticeId);
            _practiceService.InvalidateCache(location.PracticeId);

            return _licensingService.UpdateLcationName(new UpdateLocationNameModel
            {
                Id = location.OriginId.Value,
                Name = location.Name
            });
        }
        
        #endregion
    }
}
