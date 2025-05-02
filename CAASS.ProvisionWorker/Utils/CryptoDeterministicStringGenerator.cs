using System.Security.Cryptography;
using System.Text;

namespace CAASS.ProvisionWorker.Utils;

public static class CryptoDeterministicStringGenerator
{
    
    private static readonly char[] Charset = 
        "abcdefghijkmnpqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ123456789!@#$%=-+*".ToCharArray();

    public static string Generate(string seed, int length)
    {
        if (length < 1) throw new ArgumentException("Length must be greater than 0.", nameof(length));

        var result = new StringBuilder(length);
        byte[] key = Encoding.UTF8.GetBytes(seed);
        int counter = 0;

        while (result.Length < length)
        {
            byte[] counterBytes = BitConverter.GetBytes(counter++);
            
            if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes); // Ensure consistent byte order
            
            using var hmac = new HMACSHA256(key);
            byte[] hash = hmac.ComputeHash(counterBytes);
            
            // Use bytes from hash to select characters from charset
            foreach (byte b in hash)
            {
                if (result.Length >= length) break;
                
                result.Append(Charset[b % Charset.Length]);
            }
        }
        
        return result.ToString();
    }
}