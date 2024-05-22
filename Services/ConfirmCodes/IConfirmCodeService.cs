using System.Threading.Tasks;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;

namespace WildHealth.Application.Services.ConfirmCodes
{
    /// <summary>
    /// Provides method for working with confirm codes
    /// </summary>
    public interface IConfirmCodeService
    {
        /// <summary>
        /// Generates and returns confirm code
        /// </summary>
        /// <param name="user"></param>
        /// <param name="type"></param>
        /// <param name="option"></param>
        /// <param name="length"></param>
        /// <param name="unlimited"></param>
        /// <returns></returns>
        Task<ConfirmCode> GenerateAsync(
            User user, 
            ConfirmCodeType type,
            ConfirmCodeOption option = ConfirmCodeOption.Guid,
            int? length = null,
            bool unlimited = false);
        
        /// <summary>
        /// Confirms code and mark it as used
        /// </summary>
        /// <param name="code"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Task<ConfirmCode> ConfirmAsync(string code, ConfirmCodeType type);
        
        /// <summary>
        /// Confirms code and mark it as used
        /// </summary>
        /// <param name="user"></param>
        /// <param name="code"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        Task<ConfirmCode> ConfirmAsync(User user, string code, ConfirmCodeType type);
    }
}