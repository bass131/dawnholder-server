using System.Security.Cryptography;
using System.Text;

namespace Dawnholder.Server.GameServer.Accounts;

internal static class AccountIdResolver
{
    static readonly Guid AccountNamespace = new("d05083ec-6067-4d21-9ce9-271da508bbfd");

    internal static Guid Resolve(string normalizedName)
    {
        // UUID v5 hashes RFC network-order bytes; Guid's default first three fields are little-endian.
        byte[] namespaceBytes = AccountNamespace.ToByteArray(bigEndian: true);
        byte[] nameBytes = Encoding.UTF8.GetBytes(normalizedName);
        byte[] input = new byte[namespaceBytes.Length + nameBytes.Length];
        namespaceBytes.CopyTo(input, 0);
        nameBytes.CopyTo(input, namespaceBytes.Length);

        // SHA-1 is used only for deterministic UUID numbering, not authentication or security.
        byte[] hash = SHA1.HashData(input);
        hash[6] = (byte)((hash[6] & 0x0f) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3f) | 0x80);
        return new Guid(hash.AsSpan(0, 16), bigEndian: true);
    }
}
