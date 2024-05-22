using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WildHealth.Domain.Entities.Banners;
using WildHealth.Common.Models.Banners;
using WildHealth.Shared.Data.Repository;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Queries;
using System.Net;
using WildHealth.Shared.Exceptions;
using WildHealth.Domain.Enums;
using WildHealth.Application.Services.Employees;
using WildHealth.Application.Services.Patients;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Services.Banners
{
    /// <summary>
    /// <see cref="IBannersService"/>
    /// </summary>
    public class BannersService : IBannersService
    {
        private readonly IGeneralRepository<User> _usersRepository;
        private readonly IGeneralRepository<Banner> _bannerRepository;
        private readonly IGeneralRepository<BannerStatus> _bannerStatusRepository;
        private readonly IEmployeeService _employeeService;
        private readonly IPatientsService _patientService;
        private readonly ILogger<BannersService> _logger;

        public BannersService(
            IGeneralRepository<User> usersRepository,
            IGeneralRepository<Banner> bannerRepository,
            IGeneralRepository<BannerStatus> bannerStatusRepository,
            IEmployeeService employeeService,
            IPatientsService patientService,
            ILogger<BannersService> logger)
        {
            _usersRepository = usersRepository;
            _bannerRepository = bannerRepository;
            _bannerStatusRepository = bannerStatusRepository;
            _employeeService = employeeService;
            _patientService = patientService;
            _logger = logger;
        }

        /// <summary>
        /// <see cref="IBannersService.GetBannerStatusByIdAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<BannerStatus> GetBannerStatusByIdAsync(int id)
        {
            var bannerStatus = await _bannerStatusRepository
                .All()
                .ById(id)
                .Include(x => x.Banner)
                .FirstOrDefaultAsync();

            if (bannerStatus is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, $"Unable to find bannerStatus for [BannerStatusId] = {id}", exceptionParam);
            }

            return bannerStatus;
        }

        /// <summary>
        /// <see cref="IBannersService.GetBannerStatusesByUserIdAsync"/>
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<Banner[]> GetBannersByUserIdAsync(int userId)
        {
            var user = await _usersRepository
                .All()
                .Include(o => o.Patient)
                .Include(o => o.Employee)
                .Where(o => o.Id == userId)
                .FirstOrDefaultAsync();

            if (user is null)
            {
                return Enumerable.Empty<Banner>().ToArray();
            }

            var acceptedTypes =
                user.Employee != null ? new[] {BannerDisplayType.All, BannerDisplayType.Employee} :
                user.Patient != null ? new[] {BannerDisplayType.All, BannerDisplayType.Patient} :
                new[] {BannerDisplayType.All};
            
            return await _bannerRepository
                .All()
                .Include(x => x.BannerStatuses).ThenInclude(o => o.User)
                .Where(o => o.IsActive)
                .Where(o => !o.BannerStatuses.Any(a => a.UserId == userId && a.IsAcknowledged && a.User.CreatedAt <= o.CreatedAt))
                .Where(o => acceptedTypes.Contains(o.Type))
                .OrderBy(c=> c.Priority)
                .AsNoTracking()
                .ToArrayAsync();
        }

        public async Task<Banner[]> GetDisplayableBannersByUserIdAsync(int userId)
        {
            var banners = await GetBannersByUserIdAsync(userId);

            if (banners.Length > 0)
            { 
                return new [] { banners.First() };
            }

            return Enumerable.Empty<Banner>().ToArray();
        }

        /// <summary>
        /// Acknowledge the banner for the given user
        /// </summary>
        /// <param name="bannerId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<BannerStatus> AcknowledgeBannerStatus(int bannerId, int userId)
        {
            var bannerStatus = await _bannerStatusRepository
                .All()
                .Where(o => o.UserId == userId && o.BannerId == bannerId)
                .FirstOrDefaultAsync();

            if (bannerStatus is not null)
            {
                bannerStatus.IsAcknowledged = true;
                
                _bannerStatusRepository.Edit(bannerStatus);

                await _bannerStatusRepository.SaveAsync();

                return bannerStatus;
            }

            bannerStatus = new BannerStatus(
                bannerId: bannerId,
                userId: userId,
                isAcknowledged: true);

            await _bannerStatusRepository.AddAsync(bannerStatus);

            await _bannerStatusRepository.SaveAsync();

            return bannerStatus;
        }

        /// <summary>
        /// <see cref="IBannersService.CreateBannerAsync"/>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<Banner> CreateBannerAsync(BannerModel model)
        {
            var newBanner = new Banner
            {
                Name = model.Name,
                Copy = model.Copy,
                Type = model.Type,
                IsActive = true
            };
            await _bannerRepository.AddAsync(newBanner);

            await _bannerRepository.SaveAsync();

            return newBanner;
        }

        /// <summary>
        /// <see cref="IBannersService.CreateBannerAsync"/>
        /// </summary>
        /// <returns></returns>
        public async Task<Banner[]> GetAllBannersAsync()
        {
            var banners = await _bannerRepository
                .All()
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToArrayAsync();

            return banners;
        }

        /// <summary>
        /// <see cref="IBannersService.DeleteAsync(int)"/>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<Banner> DeleteAsync(int id)
        {
            var banner = await _bannerRepository
                .All()
                .ById(id)
                .FirstOrDefaultAsync();
            
            if (banner is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(id), id);
                throw new AppException(HttpStatusCode.NotFound, "Banner does not exist", exceptionParam);
            }

            try
            {
                _bannerRepository.Delete(banner);

                await _bannerRepository.SaveAsync();
            }
            catch (Exception e)
            {
                _logger.LogError($"Unable to delete banner with [Id] = {id}, [err] - {e.ToString()}");
                banner.IsActive = false;
                
                _bannerRepository.Edit(banner);
                
                await _bannerRepository.SaveAsync();
            }

            return banner;
        }

        /// <summary>
        /// <see cref="IBannersService.UpdateAsync"/>
        /// </summary>
        /// <param name="bannerModel"></param>
        /// <returns></returns>
        public async Task<Banner> UpdateAsync(BannerModel bannerModel)
        {
            var banner = await _bannerRepository
                .All()
                .ById(bannerModel.Id)
                .FirstOrDefaultAsync();
            
            if (banner is null)
            {
                var exceptionParam = new AppException.ExceptionParameter(nameof(bannerModel.Id), bannerModel.Id);
                throw new AppException(HttpStatusCode.NotFound, "Banner does not exist", exceptionParam);
            }
               
            banner.Name = bannerModel.Name;
            banner.Copy = bannerModel.Copy;
            banner.Type = bannerModel.Type;
            banner.IsActive = bannerModel.IsActive;
            banner.Priority = bannerModel.Priority;

            _bannerRepository.Edit(banner);

            await _bannerRepository.SaveAsync();

            return banner;
        }
    }
}
