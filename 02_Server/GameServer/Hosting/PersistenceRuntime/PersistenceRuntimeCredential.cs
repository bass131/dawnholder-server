using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using Dawnholder.Server.GameServer.Persistence;
using Dawnholder.Server.GameServer.Persistence.Sql;

namespace Dawnholder.Server.GameServer.Hosting.PersistenceRuntime;

/// <summary>stdin 첫 LF JSON만 소비한다. 뒤의 operator newline은 기존 종료 입력에 남긴다.</summary>
internal sealed class PersistenceRuntimeCredential : IDisposable
{
    readonly string _userName;
    readonly SecureString _password;

    PersistenceRuntimeCredential(string userName, SecureString password)
    {
        _userName = userName;
        _password = password;
    }

    internal static async Task<PersistenceRuntimeCredential> ReadAsync(Stream input, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[2048];
        try
        {
            int length = 0;
            while (length < buffer.Length)
            {
                int read = await input.ReadAsync(buffer.AsMemory(length, 1), cancellationToken).ConfigureAwait(false);
                if (read == 0 || buffer[length] is 0 or 13)
                    throw new InvalidDataException("Runtime credential transport is invalid.");
                if (buffer[length] == 10)
                    break;
                length++;
            }
            if (length == 0 || length == buffer.Length)
                throw new InvalidDataException("Runtime credential transport is invalid.");
            using JsonDocument document = JsonDocument.Parse(buffer.AsMemory(0, length));
            JsonElement root = document.RootElement;
            SqlPersistenceSnapshot.Keys(root, "version", "userName", "password");
            string? userName = root.GetProperty("userName").GetString();
            string? password = root.GetProperty("password").GetString();
            if (root.GetProperty("version").GetInt32() != 1 || string.IsNullOrWhiteSpace(userName) ||
                userName.Length > 128 || userName.Any(char.IsControl) || password is null || password.Length is < 1 or > 128)
                throw new InvalidDataException("Runtime credential transport is invalid.");
            SecureString secret = new();
            try
            {
                foreach (char character in password)
                    secret.AppendChar(character);
                secret.MakeReadOnly();
                return new PersistenceRuntimeCredential(userName, secret);
            }
            catch
            {
                secret.Dispose();
                throw;
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(buffer);
        }
    }

    internal SqlPersistenceConnectionFactory CreateFactory(PersistenceOptions options) => new(options, _userName, _password);
    public void Dispose() => _password.Dispose();
}
