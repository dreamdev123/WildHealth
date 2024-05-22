using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace WildHealth.Application.Utils.TokenGenerator
{
    /// <summary>
    /// Auth token generator
    /// </summary>
    public interface ITokenGenerator
    {
        /// <summary>
        /// Generates and returns token based on user claims and expiration date
        /// </summary>
        /// <param name="now"></param>
        /// <param name="claims"></param>
        /// <returns></returns>
        (string token, DateTime expires) Generate(DateTime now, IEnumerable<Claim> claims);


        /// <summary>
        /// Generates and returns token based on user claims and expiration date
        /// </summary>
        /// <param name="encodedToken"></param>
        /// <returns></returns>

        JwtSecurityToken Decrypt(string encodedToken);
    }
}
