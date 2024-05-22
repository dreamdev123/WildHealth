using System;
using System.Net;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WildHealth.Application.Utils.PermissionsGuard;
using WildHealth.Domain.Entities.Integrations;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Entities.Locations;
using WildHealth.Domain.Entities.Users;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;
using WildHealth.Shared.Data.Queries;
using WildHealth.Domain.Enums.Location;
using WildHealth.Shared.Enums;

namespace WildHealth.Application.Services.Locations
{
    public class LocationsService : ILocationsService
    {
        private readonly IGeneralRepository<Location> _locationRepository;
        private readonly IPermissionsGuard _permissionsGuard;

        public LocationsService(
            IGeneralRepository<Location> locationRepository, 
            IPermissionsGuard permissionsGuard)
        {
            _locationRepository = locationRepository;
            _permissionsGuard = permissionsGuard;
        }

        /// <summary>
        /// <see cref="ILocationsService.GetDefaultLocationAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<Location> GetDefaultLocationAsync(int practiceId)
        {
            var location = await _locationRepository
                .All()
                .ByType(LocationType.HeadOffice)
                .RelatedToPractice(practiceId)
                .IncludeIntegrations<Location, LocationIntegration>()
                .FirstOrDefaultAsync();

            if (location is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(practiceId), practiceId);
                throw new AppException(HttpStatusCode.NotFound, "Default Location for practice does not exist.", exceptionParam);
            }

            return location;
        }

        /// <summary>
        /// <see cref="ILocationsService.GetFellowshipLocationAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<Location> GetFellowshipLocationAsync(int practiceId)
        {
            var location = await _locationRepository
                .All()
                .ByType(LocationType.HeadOffice)    // As part of the move to a Single Pod
                .RelatedToPractice(practiceId)
                .FirstOrDefaultAsync();

            if (location is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(practiceId), practiceId);
                throw new AppException(HttpStatusCode.NotFound, "Fellowship Location for practice does not exist.", exceptionParam);
            }

            return location;
        }

        /// <summary>
        /// <see cref="ILocationsService.GetByIdAsync"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<Location> GetByIdAsync(int id, int practiceId)
        {
            var location = await _locationRepository
                .All()
                .ById(id)
                .RelatedToPractice(practiceId)
                .IncludeEmployees()
                .IncludeIntegrations<Location, LocationIntegration>()
                .FirstOrDefaultAsync();

            if (location is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Location does not exist.", exceptionParam);
            }

            return location;
        }

        /// <summary>
        /// <see cref="ILocationsService.GetByIdsAsync"/>
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<Location[]> GetByIdsAsync(ICollection<int> ids, int practiceId)
        {
            var location = await _locationRepository
                .All()
                .ByIds(ids.ToArray())
                .RelatedToPractice(practiceId)
                .IncludeEmployees()
                .ToArrayAsync();

            return location;
        }

        /// <summary>
        /// <see cref="ILocationsService.GetAllAsync"/>
        /// </summary>
        /// <param name="practiceId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Location>> GetAllAsync(int practiceId)
        {
             var location = await _locationRepository
                .All()
                .RelatedToPractice(practiceId)
                .IncludeEmployees()
                .ToArrayAsync();

            return location;
        }

        /// <summary>
        /// <see cref="ILocationsService.GetOwnedLocationIdsAsync"/>
        /// </summary>
        /// <param name="identity"></param>
        /// <returns></returns>
        public async Task<int[]> GetOwnedLocationIdsAsync(UserIdentity identity)
        {
            switch(identity.Type)
            {
                case UserType.Patient: return new[] { identity.User.Patient.LocationId };
                case UserType.Unspecified: return (await GetAllAsync(identity.User.PracticeId)).Select(x => x.GetId()).ToArray();
                case UserType.Employee:
                    if (_permissionsGuard.IsHighestRole(identity.User.Employee.RoleId))
                    {
                        var allLocations = await GetAllAsync(identity.User.PracticeId);
                        
                        return allLocations.Select(x => x.GetId()).ToArray();
                    } 
                    
                    return identity.User.Employee.Locations.Select(c => c.LocationId).ToArray();
                
                default:
                    return Array.Empty<int>();
            }
        }

        /// <summary>
        /// <see cref="ILocationsService.CreateAsync"/>
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public async Task<Location> CreateAsync(Location location)
        {
            await _locationRepository.AddAsync(location);

            await _locationRepository.SaveAsync();

            return location;
        }

        /// <summary>
        /// <see cref="ILocationsService.UpdateAsync"/>
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public async Task<Location> UpdateAsync(Location location)
        {
            _locationRepository.Edit(location);

            await _locationRepository.SaveAsync();

            return location;
        }
    }
}
