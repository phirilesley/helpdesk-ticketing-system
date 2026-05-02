using System.Security.Cryptography;
using System.Text;
using HelpDeskSystem.Application.Interfaces;

namespace HelpDeskSystem.Application.Services;

public class MfaService : IMfaService
{
    private const int TimeStepSeconds = 30;
    private const int Digits = 6;
    private static readonly char[] Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public string GenerateSharedSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    public bool VerifyCode(string sharedSecret, string code, DateTime? nowUtc = null)
    {
        if (string.IsNullOrWhiteSpace(sharedSecret) || string.IsNullOrWhiteSpace(code))
            return false;

        var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        if (normalizedCode.Length != Digits)
            return false;

        var key = Base32Decode(sharedSecret);
        if (key.Length == 0)
            return false;

        var now = nowUtc ?? DateTime.UtcNow;
        var unix = new DateTimeOffset(now).ToUnixTimeSeconds();
        var counter = unix / TimeStepSeconds;

        for (var offset = -1; offset <= 1; offset++)
        {
            var candidate = ComputeTotp(key, counter + offset);
            if (candidate == normalizedCode)
                return true;
        }

        return false;
    }

    private static string ComputeTotp(byte[] key, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
                         | (hash[offset + 1] << 16)
                         | (hash[offset + 2] << 8)
                         | hash[offset + 3];
        var otp = binaryCode % (int)Math.Pow(10, Digits);
        return otp.ToString(new string('0', Digits));
    }

    private static string Base32Encode(byte[] data)
    {
        if (data.Length == 0)
            return string.Empty;

        var output = new StringBuilder((data.Length + 4) / 5 * 8);
        var bitBuffer = 0;
        var bitBufferLength = 0;

        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitBufferLength += 8;

            while (bitBufferLength >= 5)
            {
                var index = (bitBuffer >> (bitBufferLength - 5)) & 0x1F;
                output.Append(Base32Alphabet[index]);
                bitBufferLength -= 5;
            }
        }

        if (bitBufferLength > 0)
        {
            var index = (bitBuffer << (5 - bitBufferLength)) & 0x1F;
            output.Append(Base32Alphabet[index]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string encoded)
    {
        var clean = encoded.Trim().TrimEnd('=').ToUpperInvariant();
        if (clean.Length == 0)
            return Array.Empty<byte>();

        var bitBuffer = 0;
        var bitBufferLength = 0;
        var bytes = new List<byte>(clean.Length * 5 / 8);

        foreach (var c in clean)
        {
            var index = Array.IndexOf(Base32Alphabet, c);
            if (index < 0)
                return Array.Empty<byte>();

            bitBuffer = (bitBuffer << 5) | index;
            bitBufferLength += 5;

            if (bitBufferLength >= 8)
            {
                bytes.Add((byte)((bitBuffer >> (bitBufferLength - 8)) & 0xFF));
                bitBufferLength -= 8;
            }
        }

        return bytes.ToArray();
    }
}
