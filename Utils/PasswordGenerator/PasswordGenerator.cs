using System;
using System.Text;

namespace WildHealth.Application.Utils.PasswordGenerator
{
    public class PasswordGenerator : IPasswordGenerator
    {
        private const int Length = 10;
        private readonly Random _random = new Random();
        
        public string Generate()
        {
            var password = new StringBuilder();

            var nonAlphanumeric = true;
            var digit = true;
            var lowercase = true;
            var uppercase = true;
            
            while (password.Length < Length)
            {
                var c = (char)_random.Next(32, 126);

                password.Append(c);

                if (char.IsDigit(c))
                    digit = false;
                else if (char.IsLower(c))
                    lowercase = false;
                else if (char.IsUpper(c))
                    uppercase = false;
                else if (!char.IsLetterOrDigit(c))
                    nonAlphanumeric = false;
            }

            if (nonAlphanumeric)
                password.Append((char)_random.Next(33, 48));
            if (digit)
                password.Append((char)_random.Next(48, 58));
            if (lowercase)
                password.Append((char)_random.Next(97, 123));
            if (uppercase)
                password.Append((char)_random.Next(65, 91));

            return password.ToString();
        }
    }
}
