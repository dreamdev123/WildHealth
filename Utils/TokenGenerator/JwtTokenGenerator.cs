using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WildHealth.Common.Options;
using WildHealth.Shared.Utils.CryptographyUtil;


namespace WildHealth.Application.Utils.TokenGenerator
{
    /// <summary>
    /// JWT token generator
    /// </summary>
    public class JwtTokenGenerator : ITokenGenerator
    {
        private readonly AuthTokenOptions _options;

        private readonly ICryptographyUtil _cryptographyUtil;

        public JwtTokenGenerator(IOptions<AuthTokenOptions> options, ICryptographyUtil cryptographyUtil)
        {
            _options = options.Value;
            _cryptographyUtil = cryptographyUtil;
        }

        /// <summary>
        /// <see cref="ITokenGenerator.Generate"/>
        /// </summary>
        /// <param name="now"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        public (string token, DateTime expires) Generate(DateTime now, IEnumerable<Claim> claims)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_options.Secret);
            var expires = now.AddMinutes(_options.ExpirationInMinutes);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                Expires = expires,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return (tokenHandler.WriteToken(token), expires);
        }


        public JwtSecurityToken Decrypt(string encodedToken)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var decryptedToken = _cryptographyUtil.Decrypt(encodedToken, _options.Secret);
            return tokenHandler.ReadJwtToken(decryptedToken);
        }

    }
}
