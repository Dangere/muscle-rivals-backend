using System.Security.Cryptography;
using System.Text;
namespace MuscleRivalsBackend.Utilities;


/// <summary>
///     Hashing class used to hash passwords and tokens
/// </summary>
public static class Hashing
{
    // OWASP recommended iterations for PBKDF2-SHA256
    private const int Iterations = 600000;
    public static string GenerateSalt()
    {
        byte[] salt = new byte[16];
        using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);

        }
        return Convert.ToBase64String(salt);
    }

    public static string HashPassword(string str, string salt)
    {

        byte[] saltBytes = Convert.FromBase64String(salt);
        byte[] stringBytes = Encoding.UTF8.GetBytes(str);

        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
        stringBytes,
        saltBytes,
        Iterations,
        HashAlgorithmName.SHA256,
        outputLength: 32
        );

        return Convert.ToBase64String(hashBytes);
    }

    public static string HashToken(string str, string? salt)
    {

        byte[] saltBytes;
        byte[] stringBytes = Encoding.UTF8.GetBytes(str);

        if (salt != null)
            saltBytes = Convert.FromBase64String(salt);
        else
            saltBytes = new byte[16];


        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
        stringBytes,
        saltBytes,
        Iterations / 30,
        HashAlgorithmName.SHA256,
        outputLength: 32
        );

        return Convert.ToBase64String(hashBytes);
    }

    public static bool ValidatePassword(string password, string hashedPassword, string salt)
    {
        string generatedHash = HashPassword(password, salt);
        return generatedHash.Equals(hashedPassword);
    }
}