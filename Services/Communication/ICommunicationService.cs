using System.Threading.Tasks;
using WildHealth.Domain.Entities.Users;

namespace WildHealth.Application.Services.Communication
{
    public interface ICommunicationService
    {
        /// <summary>
        /// Send verification SMS to user
        /// </summary>
        /// <param name="user"></param>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        Task SendVerificationSmsAsync(User user, string verificationCode);
    }
}
