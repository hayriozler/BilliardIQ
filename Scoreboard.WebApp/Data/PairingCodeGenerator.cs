using System.Security.Cryptography;

namespace Scoreboard.WebApp.Data;

public static class PairingCodeGenerator
{
    private const string _alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public static string Generate(int length = 8)
    {
        if (length < 6 || length > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Pairing code length must be between 6 and 10 characters.");
        }

        var buffer = new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = _alphabet[RandomNumberGenerator.GetInt32(_alphabet.Length)];
        }

        return new string(buffer);
    }
}
