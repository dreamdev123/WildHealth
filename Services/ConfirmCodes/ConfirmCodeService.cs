using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WildHealth.Shared.Exceptions;
using WildHealth.Common.Options;
using WildHealth.Domain.Entities.Users;
using WildHealth.Domain.Enums.User;
using WildHealth.Infrastructure.Data.Queries;
using WildHealth.Shared.Data.Repository;

namespace WildHealth.Application.Services.ConfirmCodes
{
    /// <summary>
    /// <see cref="IConfirmCodeService"/>
    /// </summary>
    public class ConfirmCodeService : IConfirmCodeService
    {
        private const int DefaultLength = 8;
        private const string NumericPattern = "0123456789";
        private const string AlphabeticPattern = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private const string AlphanumericPattern = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        
        private readonly IGeneralRepository<ConfirmCode> _confirmCodeRepository;
        private readonly ConfirmCodeOptions _options;
        private readonly ILogger _logger;

        public ConfirmCodeService(
            IGeneralRepository<ConfirmCode> confirmCodeRepository,
            IOptions<ConfirmCodeOptions> options, 
            ILogger<ConfirmCodeService> logger)
        {
            _confirmCodeRepository = confirmCodeRepository;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// <see cref="IConfirmCodeService.GenerateAsync"/>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="type"></param>
        /// <param name="length"></param>
        /// <param name="option"></param>
        /// <param name="unlimited"></param>
        /// <returns></returns>
        public async Task<ConfirmCode> GenerateAsync(
            User user, 
            ConfirmCodeType type,
            ConfirmCodeOption option = ConfirmCodeOption.Guid,
            int? length = null,
            bool unlimited = false)
        {
            var expiredAt = DeterminateExpiration(type);
            var code = GenerateCode(option, length);
            
            var confirmCode = new ConfirmCode(
                user: user, 
                code: code, 
                type: type, 
                expireAt: expiredAt,
                unlimited: unlimited
            );
            
            await _confirmCodeRepository.AddAsync(confirmCode);
            await _confirmCodeRepository.SaveAsync();

            _logger.LogInformation($"Confirm code with [Id] = {confirmCode.Id} generated.");
            
            return confirmCode;
        }

        /// <summary>
        /// <see cref="IConfirmCodeService.ConfirmAsync(string, ConfirmCodeType)"/>
        /// </summary>
        /// <param name="code"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        /// <exception cref="AppException"></exception>
        public async Task<ConfirmCode> ConfirmAsync(string code, ConfirmCodeType type)
        {
            var confirmCode = await _confirmCodeRepository
                .Get(x => x.Code == code && x.Type == type)
                .IncludeUser()
                .FirstOrDefaultAsync();

            if (confirmCode is null)
            {
                _logger.LogInformation($"Confirm code with [Code] = {code} and [Type] = {type} does not exist.");
                throw new AppException(HttpStatusCode.BadRequest, "Wrong confirm code") { LogAsError = false };
            }

            return await ConfirmAsync(confirmCode);
        }

        /// <summary>
        /// <see cref="IConfirmCodeService.ConfirmAsync(User, string, ConfirmCodeType)"/>
        /// </summary>
        /// <param name="user"></param>
        /// <param name="code"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<ConfirmCode> ConfirmAsync(User user, string code, ConfirmCodeType type)
        {
            var confirmCode = await _confirmCodeRepository
                .Get(x => x.Code == code && x.Type == type && x.UserId == user.GetId())
                .IncludeUser()
                .FirstOrDefaultAsync();

            if (confirmCode is null)
            {
                _logger.LogInformation($"Confirm code with [Code] = {code} and [Type] = {type} does not exist.");
                throw new AppException(HttpStatusCode.BadRequest, "Wrong confirm code") { LogAsError = false };
            }

            return await ConfirmAsync(confirmCode);
        }

        #region private

        private async Task<ConfirmCode> ConfirmAsync(ConfirmCode confirmCode)
        {
            if (!confirmCode.IsActive(DateTime.UtcNow))
            {
                _logger.LogInformation($"Confirm code with [Id] = {confirmCode.Id} expired.");
                throw new AppException(HttpStatusCode.BadRequest, "Confirm code has expired.") { LogAsError = false };
            }

            if (!confirmCode.Unlimited)
            {
                _confirmCodeRepository.Delete(confirmCode);
                await _confirmCodeRepository.SaveAsync();
            }
            
            _logger.LogInformation($"Confirm code with [Id] = {confirmCode.Id} applied.");

            return confirmCode;
        }
        
        private DateTime DeterminateExpiration(ConfirmCodeType type)
        {
            return type switch
            {
                ConfirmCodeType.RestorePassword => DateTime.UtcNow.AddMinutes(_options.RestorePasswordExpirationInMinutes),
                ConfirmCodeType.ActivateIdentity => DateTime.UtcNow.AddMinutes(_options.ActivateIdentityExpirationInMinutes),
                ConfirmCodeType.SetUpPassword => DateTime.UtcNow.AddMinutes(_options.SetUpPasswordExpirationInMinutes),
                ConfirmCodeType.RefreshToken => DateTime.UtcNow.AddMinutes(_options.RefreshTokenExpirationInMinutes),
                ConfirmCodeType.CheckoutSession => DateTime.UtcNow.AddMinutes(_options.CheckoutSessionExpirationInMinutes),
                _ => throw new ArgumentException(nameof(ConfirmCodeType))
            };
        }

        private string GenerateCode(ConfirmCodeOption option, int? length = null)
        {
            if (option == ConfirmCodeOption.Guid)
            {
                return length is null
                    ? Guid.NewGuid().ToString()
                    : Guid.NewGuid().ToString().Substring(length.Value);
            }
            
            var code = new char[length ?? DefaultLength];
            var random = new Random();
            var pattern = option switch
            {
                ConfirmCodeOption.Numeric => NumericPattern,
                ConfirmCodeOption.Alphabetic => AlphabeticPattern,
                ConfirmCodeOption.Alphanumeric => AlphanumericPattern,
                ConfirmCodeOption.Guid => throw new ArgumentException("Unsupported confirm code option"),
                _ => throw new ArgumentException("Unsupported confirm code option")
            };
            
            for (var x = 0; x < code.Length; x++)
            {
                code[x] = pattern[random.Next(pattern.Length)];
            }

            return new string(code);
        }

        #endregion
    }
}
