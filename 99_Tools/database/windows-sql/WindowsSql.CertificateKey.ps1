# Certificate key identity comes from CERT_KEY_PROV_INFO, not the managed RSA wrapper or X509 KeyUsage.
# This boundary reads metadata and opens existing keys only; the caller owns the certificate throughout the call.
function Initialize-WindowsSqlCertificateKeyNative {
    if ('Dawnholder.WindowsSql.CertificateKeyNative' -as [type]) { return }
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.System32)]

namespace Dawnholder.WindowsSql
{
    public static class CertificateKeyNative
    {
        private const uint KeyProviderInfoProperty = 2;
        private const uint MachineKeySet = 0x20;
        private const uint CompareKey = 0x04;
        private const uint NoHealing = 0x08;
        private const uint Silent = 0x40;
        private const uint ExchangeKey = 1;
        private const uint CngKeySpec = 0xffffffff;
        private const int MaximumMetadataBytes = 65536;
        private const string MetadataError = "WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE";
        private const string AccessError = "WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED";
        private const string UnsupportedError = "WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED";

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyProviderInfo
        {
            internal IntPtr ContainerName;
            internal IntPtr ProviderName;
            internal uint ProviderType;
            internal uint Flags;
            internal uint ParameterCount;
            internal IntPtr Parameters;
            internal uint KeySpec;
        }

        [DllImport("crypt32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CertGetCertificateContextProperty(
            IntPtr certificate, uint property, IntPtr data, ref uint size);

        [DllImport("crypt32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptAcquireCertificatePrivateKey(
            IntPtr certificate, uint flags, IntPtr reserved, out IntPtr provider,
            out uint keySpec, [MarshalAs(UnmanagedType.Bool)] out bool callerFree);

        [DllImport("advapi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CryptReleaseContext(IntPtr provider, uint flags);

        [DllImport("ncrypt.dll", ExactSpelling = true)]
        private static extern int NCryptFreeObject(IntPtr key);

        // The native property packs strings into the returned buffer. Never follow a pointer outside that buffer.
        private static string ReadName(IntPtr buffer, uint size, IntPtr text)
        {
            long offset = text.ToInt64() - buffer.ToInt64();
            int structureSize = Marshal.SizeOf<KeyProviderInfo>();
            if (offset < structureSize || offset >= size || (offset & 1) != 0)
                throw new InvalidOperationException(MetadataError);

            int available = checked((int)(size - offset) / 2);
            for (int length = 0; length < available; length++)
            {
                if (Marshal.ReadInt16(text, length * 2) == 0)
                    return Marshal.PtrToStringUni(text, length);
            }
            throw new InvalidOperationException(MetadataError);
        }

        private static Exception MetadataReadFailure()
        {
            int error = Marshal.GetLastWin32Error();
            // ERROR_ACCESS_DENIED / NTE_PERM describe access, not an unsupported key attribute.
            return new InvalidOperationException(error == 5 || error == unchecked((int)0x80090010)
                ? AccessError : MetadataError);
        }

        public static CspParameters ReadProviderParameters(X509Certificate2 certificate)
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                IntPtr context = certificate.Handle;
                uint size = 0;
                if (!CertGetCertificateContextProperty(context, KeyProviderInfoProperty, IntPtr.Zero, ref size))
                    throw MetadataReadFailure();
                if (size < Marshal.SizeOf<KeyProviderInfo>() || size > MaximumMetadataBytes)
                    throw new InvalidOperationException(MetadataError);

                uint capacity = size;
                buffer = Marshal.AllocHGlobal(checked((int)capacity));
                if (!CertGetCertificateContextProperty(context, KeyProviderInfoProperty, buffer, ref size))
                    throw MetadataReadFailure();
                if (size < Marshal.SizeOf<KeyProviderInfo>() || size > capacity)
                    throw new InvalidOperationException(MetadataError);

                KeyProviderInfo info = Marshal.PtrToStructure<KeyProviderInfo>(buffer);
                if (info.ParameterCount != 0 || info.ProviderType > int.MaxValue || info.KeySpec > int.MaxValue)
                    throw new InvalidOperationException(UnsupportedError);

                return new CspParameters
                {
                    ProviderName = ReadName(buffer, size, info.ProviderName),
                    KeyContainerName = ReadName(buffer, size, info.ContainerName),
                    ProviderType = (int)info.ProviderType,
                    KeyNumber = (int)info.KeySpec,
                    Flags = (info.Flags & MachineKeySet) != 0
                        ? CspProviderFlags.UseMachineKeyStore : CspProviderFlags.NoFlags
                };
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(buffer);
                // Handle is borrowed. Its owning certificate must outlive both native property reads.
                GC.KeepAlive(certificate);
            }
        }

        public static void AssertKeyBinding(X509Certificate2 certificate)
        {
            IntPtr provider = IntPtr.Zero;
            uint keySpec = 0;
            bool callerFree = false;
            try
            {
                // No cache / USE_PROV_INFO / CNG flags: compare this certificate's public key through CAPI.
                // NO_HEALING prevents repairing a missing association or searching for a different container.
                bool acquired = CryptAcquireCertificatePrivateKey(
                    certificate.Handle, CompareKey | NoHealing | Silent, IntPtr.Zero,
                    out provider, out keySpec, out callerFree);
                if (!acquired || provider == IntPtr.Zero)
                    throw new InvalidOperationException(AccessError);
                if (keySpec != ExchangeKey)
                    throw new InvalidOperationException(UnsupportedError);
                // A borrowed/cached result cannot prove that COMPARE_KEY actually ran.
                if (!callerFree)
                    throw new InvalidOperationException(MetadataError);
            }
            finally
            {
                if (callerFree && provider != IntPtr.Zero)
                {
                    if (keySpec == CngKeySpec)
                        NCryptFreeObject(provider);
                    else
                        CryptReleaseContext(provider, 0);
                }
                GC.KeepAlive(certificate);
            }
        }
    }
}
'@ -ErrorAction Stop
}

function Get-WindowsSqlCertificateKeyFailureCode {
    param([Exception]$Exception, [string]$DefaultCode)
    # PowerShell wraps method exceptions. Only our closed public codes cross this boundary.
    $allowed = @('WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE',
        'WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED', 'WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED')
    $current = $Exception
    while ($null -ne $current) {
        if ($current.Message -cin $allowed) { return $current.Message }
        # Access denied, NTE_PERM, BAD_KEYSET, NO_KEY, KEYSET_NOT_DEF and SILENT_CONTEXT are access failures.
        if ($current -is [UnauthorizedAccessException] -or
            $current.HResult -in @([int]0x80070005, [int]0x80090010, [int]0x80090016,
                [int]0x8009000D, [int]0x80090019, [int]0x80090022)) {
            return 'WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED'
        }
        $current = $current.InnerException
    }
    return $DefaultCode
}

function Get-WindowsSqlCertificateKeyFile {
    param($Certificate)
    try {
        Initialize-WindowsSqlCertificateKeyNative
        $parameters = [Dawnholder.WindowsSql.CertificateKeyNative]::ReadProviderParameters($Certificate)
    }
    catch {
        throw (Get-WindowsSqlCertificateKeyFailureCode -Exception $_.Exception `
                -DefaultCode 'WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE')
    }
    if ($null -eq $parameters -or [string]::IsNullOrWhiteSpace($parameters.KeyContainerName) -or
        $parameters.KeyContainerName.Length -gt 260 -or $parameters.KeyContainerName.Contains([char]0)) {
        throw 'WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE'
    }
    # PROV_RSA_SCHANNEL (12) and AT_KEYEXCHANGE (1) are native provider/KeySpec values, not X509 KeyUsage bits.
    $providerName = 'Microsoft RSA SChannel Cryptographic Provider'
    if ($parameters.ProviderName -cne $providerName -or $parameters.ProviderType -ne 12 -or
        $parameters.KeyNumber -ne [int][Security.Cryptography.KeyNumber]::Exchange -or
        ($parameters.Flags -band [Security.Cryptography.CspProviderFlags]::UseMachineKeyStore) -eq 0) {
        throw 'WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED'
    }

    $key = $null
    try {
        try {
            # These flags forbid both container creation and key generation; never set PersistKeyInCsp to false.
            $parameters.Flags = [Security.Cryptography.CspProviderFlags]::UseExistingKey -bor
            [Security.Cryptography.CspProviderFlags]::UseMachineKeyStore -bor
            [Security.Cryptography.CspProviderFlags]::NoPrompt
            $key = [Security.Cryptography.RSACryptoServiceProvider]::new($parameters)
            if ($null -eq $key) { throw 'WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED' }
            [Dawnholder.WindowsSql.CertificateKeyNative]::AssertKeyBinding($Certificate)
        }
        catch {
            throw (Get-WindowsSqlCertificateKeyFailureCode -Exception $_.Exception `
                    -DefaultCode 'WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED')
        }
        try {
            # Explicit getters preserve exceptions which PowerShell's property access can silently turn into null.
            $info = $key.get_CspKeyContainerInfo()
            if ($null -eq $info) { throw 'WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE' }
            $actualProvider = $info.get_ProviderName()
            $actualProviderType = $info.get_ProviderType()
            $container = $info.get_KeyContainerName()
            $keyNumber = $info.get_KeyNumber()
            $machineKey = $info.get_MachineKeyStore()
            $exportable = $info.get_Exportable()
            $uniqueName = $info.get_UniqueKeyContainerName()
            $keySize = $key.get_KeySize()
            $publicOnly = $key.get_PublicOnly()
        }
        catch {
            throw (Get-WindowsSqlCertificateKeyFailureCode -Exception $_.Exception `
                    -DefaultCode 'WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE')
        }
        if ($actualProvider -cne $providerName -or $actualProviderType -ne 12 -or
            $container -cne $parameters.KeyContainerName -or
            $keyNumber -ne [Security.Cryptography.KeyNumber]::Exchange -or
            $machineKey -ne $true -or $exportable -ne $false -or $publicOnly -ne $false -or $keySize -lt 2048) {
            throw 'WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED'
        }
        if ([string]::IsNullOrWhiteSpace($uniqueName)) {
            throw 'WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE'
        }
        # A unique container name is one filename, never a relative path, stream or MachineKeys itself.
        if ($uniqueName.Length -gt 255 -or $uniqueName.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or
            $uniqueName -in @('.', '..') -or $uniqueName.EndsWith('.') -or $uniqueName.EndsWith(' ')) {
            throw 'WINDOWS_SQL_PATH_INVALID'
        }
        $keyRoot = Join-Path ([Environment]::GetFolderPath('CommonApplicationData')) 'Microsoft\Crypto\RSA\MachineKeys'
        $keyFile = Get-WindowsSqlFullPath -Path (Join-Path $keyRoot $uniqueName)
        if ([IO.Path]::GetDirectoryName($keyFile) -ine $keyRoot) { throw 'WINDOWS_SQL_PATH_INVALID' }
        try { $attributes = [IO.File]::GetAttributes($keyFile) }
        catch [UnauthorizedAccessException] { throw 'WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED' }
        catch { throw 'WINDOWS_SQL_PATH_INVALID' }
        if (($attributes -band [IO.FileAttributes]::Directory) -ne 0) { throw 'WINDOWS_SQL_PATH_INVALID' }
        [void](Assert-WindowsSqlLocalPath -Path $keyFile)
        return $keyFile
    }
    finally { if ($null -ne $key) { $key.Dispose() } }
}

function Read-WindowsSqlCertificateKeyAcl {
    param([string]$KeyFile)
    try {
        $acl = Get-Acl -LiteralPath $KeyFile -ErrorAction Stop
        if ($null -eq $acl) { throw 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_ACCESS_FAILED' }
        return $acl
    }
    catch { throw 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_ACCESS_FAILED' }
}

function Assert-WindowsSqlCertificateServiceAcl {
    param($Acl, [string]$ServiceSid)
    try {
        $rules = @($Acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]) | Where-Object {
                $_.IdentityReference.Value -ceq $ServiceSid
            })
    }
    catch { throw 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_ACCESS_FAILED' }
    if (@($rules | Where-Object AccessControlType -EQ 'Deny').Count -ne 0 -or
        @($rules | Where-Object {
                $_.AccessControlType -eq 'Allow' -and
                ($_.FileSystemRights -band [Security.AccessControl.FileSystemRights]::Read) -eq
                [Security.AccessControl.FileSystemRights]::Read
            }).Count -ne 1) { throw 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_INVALID' }
}
