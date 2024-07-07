namespace OpenIddict.Sandbox.UnoClient;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

internal static class OpenIddictClientCryptographicKeys
{
    internal static RsaSecurityKey GetRsaCngKey(string name, CngKeyUsages usages, CngProvider provider)
    {
        name = name ?? throw new ArgumentNullException(nameof(name));

        System.Security.Cryptography.CngKey key;

#pragma warning disable CA2000 // Dispose objects before losing scope
        if (CngKey.Exists(name, provider, CngKeyOpenOptions.UserKey))
        {
            key = CngKey.Open(name, provider, CngKeyOpenOptions.UserKey);
        }

        else
        {
            try
            {
                key = CngKey.Create(CngAlgorithm.Rsa, name, new CngKeyCreationParameters
                {
                    KeyCreationOptions = CngKeyCreationOptions.None,
                    KeyUsage = usages,
                    Parameters = { new CngProperty("Length", BitConverter.GetBytes(2048), CngPropertyOptions.None) },
                    Provider = provider
                });
            }

            // If multiple instances of the application were started at the same time, a race condition
            // might occur here. In this case, try to open the key that was created by the other instance.
            catch (CryptographicException) when (CngKey.Exists(name, provider, CngKeyOpenOptions.UserKey))
            {
                key = CngKey.Open(name, provider, CngKeyOpenOptions.UserKey);
            }
        }

        return new RsaSecurityKey(new RSACng(key));
#pragma warning restore CA2000 // Dispose objects before losing scope
    }
}

#if IOS || MACCATALYST

public static class CngKey
{
    public static bool Exists(string name, CngProvider provider, CngKeyOpenOptions options)
    {
        return false;
    }

    public static System.Security.Cryptography.CngKey Open(string name, CngProvider provider, CngKeyOpenOptions options)
    {
        //System.Security.Cryptography.CgnKey.Import();
        return null;
    }

    public static System.Security.Cryptography.CngKey Create(CngAlgorithm algorithm, string name, CngKeyCreationParameters parameters)
    {
        //System.Security.Cryptography.CgnKey.Import();
        return null;
    }
}
#endif
