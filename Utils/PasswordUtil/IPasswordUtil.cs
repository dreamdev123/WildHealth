namespace WildHealth.Application.Utils.PasswordUtil
{
    public interface IPasswordUtil
    {
        bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt);

        (byte[] passwordHash, byte[] passwordSalt) CreatePasswordHash(string password);
    }
}