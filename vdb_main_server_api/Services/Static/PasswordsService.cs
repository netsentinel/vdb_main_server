using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace main_server_api.Services.Static;


public static class PasswordsService
{
    private const int SaltSizeBytes = 512 / 8;

    //public PasswordsService(IServiceScopeFactory DbContextScopeFactory, PasswordsCryptographyServiceSettingsProvider SettingsProvider)
    //{
    //	this.DbContextScopeFactory = DbContextScopeFactory;
    //	this.SettingsProvider = SettingsProvider;
    //}

    private static byte[] ConcatBytes(byte[] firstSeq, byte[] secondSeq)
    {
        var result = new byte[firstSeq.Length + secondSeq.Length];
        firstSeq.CopyTo(result, 0);
        secondSeq.CopyTo(result, firstSeq.Length);

        return result;
    }
    private static byte[] GenerateSalt()
    {
        return RandomNumberGenerator.GetBytes(SaltSizeBytes);
    }
    private static byte[] HashPassword(string passwordPlainText, byte[] salt)
    {
        return SHA512.HashData(ConcatBytes(Encoding.UTF8.GetBytes(passwordPlainText), salt));
    }

    public static byte[] HashPassword(string passwordPlainText, out byte[] generatedSalt)
    {
        generatedSalt = GenerateSalt();
        return HashPassword(passwordPlainText, generatedSalt);
    }
    public static bool ConfirmPassword(string passwordPlainText, byte[] hashedPassword, byte[] salt)
    {
        return HashPassword(passwordPlainText, salt).SequenceEqual(hashedPassword);
    }
}
