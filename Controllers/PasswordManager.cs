using System;
using System.Security.Cryptography;
using System.Text;

public class PasswordManager
{
    public static string GenerateSalt()
    {
        byte[] saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    public static string Hash(string value)
    {
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
            StringBuilder builder = new();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
    public static string GeneratePasswordHash(string password, string salt)
    {
        byte[] saltBytes = Convert.FromBase64String(salt);
        string combined = password + Convert.ToBase64String(saltBytes);
        return Hash(combined);
    }

    public static bool Verify(string candidatePassword, string salt, string passwordHash)
    {
        var candidateHash = GeneratePasswordHash(candidatePassword, salt);
        return candidateHash == passwordHash;
    }
}