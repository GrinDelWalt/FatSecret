using System.Security.Cryptography;
using System.Text;

namespace FatSecret.Service.Authentication
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);
    }

    public class PasswordHasher : IPasswordHasher
    {
        private const int SaltSize = 32; // 256 bits
        private const int HashSize = 32; // 256 bits
        private const int Iterations = 10000;

        public string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password cannot be null or empty", nameof(password));

            // ?????????? ????
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // ???????? ?????? ? ?????
            byte[] hash = HashPasswordWithSalt(password, salt);

            // ?????????? ???? ? ???
            byte[] result = new byte[SaltSize + HashSize];
            Array.Copy(salt, 0, result, 0, SaltSize);
            Array.Copy(hash, 0, result, SaltSize, HashSize);

            return Convert.ToBase64String(result);
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                byte[] combined = Convert.FromBase64String(hashedPassword);
                
                if (combined.Length != SaltSize + HashSize)
                    return false;

                // ????????? ????
                byte[] salt = new byte[SaltSize];
                Array.Copy(combined, 0, salt, 0, SaltSize);

                // ????????? ???
                byte[] storedHash = new byte[HashSize];
                Array.Copy(combined, SaltSize, storedHash, 0, HashSize);

                // ???????? ????????? ?????? ? ??????????? ?????
                byte[] computedHash = HashPasswordWithSalt(password, salt);

                // ?????????? ????
                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false;
            }
        }

        private byte[] HashPasswordWithSalt(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }
    }
}
