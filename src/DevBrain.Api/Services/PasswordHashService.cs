using System.Security.Cryptography;
using System.Text;

namespace DevBrain.Api.Services;

public class PasswordHashService : IPasswordHashService
{
    public string HashPassword(string password)
    {
        // Generate random salt
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash password with PBKDF2 (100,000 iterations - standard as of 2026)
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
        {
            byte[] hash = pbkdf2.GetBytes(20);

            // Combine salt and hash for storage
            byte[] hashWithSalt = new byte[36];
            Buffer.BlockCopy(salt, 0, hashWithSalt, 0, 16);
            Buffer.BlockCopy(hash, 0, hashWithSalt, 16, 20);

            // Return as Base64 for storage
            return Convert.ToBase64String(hashWithSalt);
        }
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            byte[] hashWithSalt = Convert.FromBase64String(hash);

            // Extract salt
            byte[] salt = new byte[16];
            Buffer.BlockCopy(hashWithSalt, 0, salt, 0, 16);

            // Compute hash with stored salt
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
            {
                byte[] computedHash = pbkdf2.GetBytes(20);

                // Compare hashes
                for (int i = 0; i < 20; i++)
                {
                    if (hashWithSalt[i + 16] != computedHash[i])
                        return false;
                }

                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
