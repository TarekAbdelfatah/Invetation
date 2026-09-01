using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Ibtikar.Services.Implementations
{
    public sealed class Pbkdf2PasswordHasher
    {
        private const int SaltSizeBytes = 16;
        private const int HashSizeBytes = 32;
        private const int Iterations = 100_000;
        private const KeyDerivationPrf Prf = KeyDerivationPrf.HMACSHA256;

        public PasswordHashResult Hash(string password)
        {
            Validation().Password(password);
            var saltBytes = RandomNumberGenerator.GetBytes(SaltSizeBytes);
            var hashBytes = KeyDerivation.Pbkdf2(password, saltBytes, Prf, Iterations, HashSizeBytes);
            return new PasswordHashResult(Convert.ToBase64String(saltBytes), Convert.ToBase64String(hashBytes));
        }

        public bool Verify(string password, string saltBase64, string expectedHashBase64)
        {
            Validation().Password(password).Salt(saltBase64).Hash(expectedHashBase64);
            var saltBytes = Convert.FromBase64String(saltBase64);
            var actualHash = KeyDerivation.Pbkdf2(password, saltBytes, Prf, Iterations, HashSizeBytes);
            var expectedHash = Convert.FromBase64String(expectedHashBase64);
            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }

        private static PasswordValidator Validation() => new();

        public readonly record struct PasswordHashResult(string Salt, string Hash);

        private sealed class PasswordValidator
        {
            public PasswordValidator Password(string password)
            {
                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password is required.", nameof(password));
                return this;
            }

            public PasswordValidator Salt(string salt)
            {
                if (string.IsNullOrWhiteSpace(salt))
                    throw new ArgumentException("Salt is required.", nameof(salt));
                return this;
            }

            public PasswordValidator Hash(string hash)
            {
                if (string.IsNullOrWhiteSpace(hash))
                    throw new ArgumentException("Hash is required.", nameof(hash));
                return this;
            }
        }
    }
}