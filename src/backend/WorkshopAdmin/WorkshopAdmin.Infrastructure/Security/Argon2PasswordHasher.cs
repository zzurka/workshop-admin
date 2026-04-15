namespace WorkshopAdmin.Infrastructure.Security;

using Konscious.Security.Cryptography;
using System.Security.Cryptography;
using System.Text;
using WorkshopAdmin.Application.Common.Interfaces;

public class Argon2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int DegreeOfParallelism = 4;
    private const int MemorySize = 65536; // 64 MB
    private const int Iterations = 3;

    public string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = ComputeHash(password, salt);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hashString)
    {
        string[] parts = hashString.Split(':');
        if (parts.Length != 2) return false;

        byte[] salt = Convert.FromBase64String(parts[0]);
        byte[] expectedHash = Convert.FromBase64String(parts[1]);
        byte[] actualHash = ComputeHash(password, salt);

        return CryptographicOperations.FixedTimeEquals(expectedHash, actualHash);
    }

    private static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = DegreeOfParallelism,
            MemorySize = MemorySize,
            Iterations = Iterations
        };

        return argon2.GetBytes(HashSize);
    }
}
