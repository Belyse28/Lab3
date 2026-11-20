using System.Security.Cryptography;

namespace RentalPropertyManagement.Data;

/// <summary>Salted PBKDF2-SHA256 password hashing. Passwords are never stored in plain text.</summary>
public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return Iterations + "." + Convert.ToBase64String(salt) + "." + Convert.ToBase64String(key);
    }

    public static bool Verify(string password, string stored)
    {
        try
        {
            string[] parts = stored.Split('.');
            if (parts.Length != 3) return false;
            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expected = Convert.FromBase64String(parts[2]);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch
        {
            return false;
        }
    }
}
