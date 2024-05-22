using System.Collections.Generic;
using System.Threading.Tasks;
using WildHealth.Domain.Entities.Banners;
using WildHealth.Common.Models.Banners;

namespace WildHealth.Application.Services.Banners
{
    /// <summary>
    /// Provides method for working with banners
    /// </summary>
    public interface IBannersService
    {
        /// <summary>
        /// Returns bannerStatus by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<BannerStatus> GetBannerStatusByIdAsync(int id);

        /// <summary>
        /// Get banners for selected user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<Banner[]> GetBannersByUserIdAsync(int userId);

        /// <summary>
        /// Get displayable banners for selected user
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<Banner[]> GetDisplayableBannersByUserIdAsync(int userId);

        /// <summary>
        /// Creates Banner
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<Banner> CreateBannerAsync(BannerModel model);

        /// <summary>
        /// Acknowledge the banner for the given user
        /// </summary>
        /// <param name="bannerId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        Task<BannerStatus> AcknowledgeBannerStatus(int bannerId, int userId);

        /// <summary>
        /// Get All Banners
        /// </summary>
        /// <returns></returns>
        Task<Banner[]> GetAllBannersAsync();

        /// <summary>
        /// Deletes banner
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Banner> DeleteAsync(int id);

        /// <summary>
        /// Returns an updated banner
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        Task<Banner> UpdateAsync(BannerModel model);
    }
}
