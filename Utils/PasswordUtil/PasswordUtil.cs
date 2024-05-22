using System.Linq;

namespace WildHealth.Application.Utils.PasswordUtil
{
    public class PasswordUtil : IPasswordUtil
    {
        public bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] salt)
        {
            if (password is null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            if (passwordHash is null || passwordHash.Length != 64)
            {
                return false;
            }

            if (salt is null || salt.Length != 128)
            {
                return false;
            }
            
            using var hmac = new System.Security.Cryptography.HMACSHA512(salt);
            
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            
            return !computedHash.Where((t, i) => t != passwordHash[i]).Any();
        }

        public (byte[] passwordHash, byte[] passwordSalt) CreatePasswordHash(string password)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA512();
            var passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            var passwordSalt = hmac.Key;
            return (passwordHash, passwordSalt);
        }
    }
}