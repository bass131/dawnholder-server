[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    # Optional absolute windows-sql folder of an isolated same-shaped tree (before/after product runs).
    [string]$ProductRoot = ''
)
# Independent verification of the certificate key boundary: the CERT_KEY_PROV_INFO decision, the CSP reopen, the
# key file path and the service ACL, through the real key functions, the real create entry and the real Assert gate.
# - The product's native class Dawnholder.WindowsSql.CertificateKeyNative and RSACryptoServiceProvider bind to
#   stand-ins and Add-Type refuses that class. Provider info, CSP container answers and key binding results are case
#   facts at the class's public contract (CspParameters, closed-code exceptions). Buffer parsing, handle release and
#   CAPI/CNG interop inside the class are not verified here.
# - CspKeyContainerInfo echoes the stored CspParameters for provider, type, container, KeyNumber and machine flag, as
#   the runtime does; Exportable, UniqueKeyContainerName, KeySize and PublicOnly come from the case container.
# - Certificates are public-only in-memory certificates signed by a key disposed at once. The machine key folder is a
#   suite folder returned by the Environment stand-in. Key files and their ACLs are real files below this suite.
# - Protected type names stay inside strings: the parser caches type literal names, so a literal parsed before the
#   stand-ins are registered would keep the real type.
# Nothing here opens a certificate store, a key container, the registry, a service, SQL, the firewall or WSL.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-certificate-key-verification'

# The -ProductRoot parameter is this same script-scope variable; empty means the checked-out windows-sql folder.
if (-not $ProductRoot) { $ProductRoot = Join-Path $script:ToolRoot 'windows-sql' }
elseif (-not [IO.Path]::IsPathRooted($ProductRoot)) { throw 'ProductRoot must be an explicit absolute path.' }
$script:ProductRoot = [IO.Path]::GetFullPath($ProductRoot)
if (-not [IO.File]::Exists((Join-Path $script:ProductRoot 'New-WindowsSqlCertificate.ps1'))) {
    throw ('ProductRoot has no New-WindowsSqlCertificate.ps1: ' + $script:ProductRoot)
}
$script:FixtureMachine = 'LAPTOP-FIXTURE'
$script:FixtureInstance = 'DHFIXTURE'
$script:ServiceAccount = 'NT Service\MSSQL$DHFIXTURE'
$script:ServiceSid = 'S-1-5-80-1111111111-2222222222-3333333333-4111111111-555555555'
$script:TlsServerName = 'localhost'
$script:SChannel = 'Microsoft RSA SChannel Cryptographic Provider'
$script:Sentinel = 'VERIFY_SENTINEL_7C2E'
if ($env:COMPUTERNAME -ceq $script:FixtureMachine) {
    throw 'This host has the fixture machine name; refusing to run.'
}
$env:LOCALAPPDATA = Join-Path $script:SuiteRoot 'profile'
[void][IO.Directory]::CreateDirectory((Join-Path $env:LOCALAPPDATA 'Dawnholder'))
$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
try { $script:ExecutorSid = $currentIdentity.User.Value } finally { $currentIdentity.Dispose() }
$script:Codes = @{
    Metadata = 'WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE'
    Access = 'WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED'
    Unsupported = 'WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED'
    AclAccess = 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_ACCESS_FAILED'
    AclInvalid = 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_INVALID'
    PathInvalid = 'WINDOWS_SQL_PATH_INVALID'
    PathReparse = 'WINDOWS_SQL_PATH_REPARSE'
}

# =====================================================================================================
# 0. Stand-in types. State reachable from product code lives in VerifyWorld: inside a product script, $script:
#    names that script's scope, not this file's.
# =====================================================================================================
class VerifyWorld {
    static [Collections.Generic.List[object]]$Events = [Collections.Generic.List[object]]::new()
    static [string]$Run = 'setup'
    static [string]$SuiteRoot = ''
    static [string]$KeyArea = ''
    static [hashtable]$Certificates = @{}
    static [hashtable]$Containers = @{}
    static [hashtable]$Store = @{}
    static [string]$PendingCreation = ''
    static [Collections.Generic.List[object]]$Keys = [Collections.Generic.List[object]]::new()
    static [Collections.Generic.List[object]]$Handed = [Collections.Generic.List[object]]::new()
    static [hashtable]$Acl = @{ ReadDeny = @(); ReadNull = @(); Set = 'Real'; Reads = 0 }

    static [void] Add([string]$kind, [string]$detail) {
        [VerifyWorld]::Events.Add([pscustomobject]@{ Run = [VerifyWorld]::Run; Kind = $kind; Detail = $detail })
    }

    static [void] Refuse([string]$kind, [string]$detail) {
        [VerifyWorld]::Add($kind, $detail)
        throw ('VERIFY_REFUSED_' + $kind + ': ' + $detail)
    }

    # Raw text that must never cross the product's closed-code boundary.
    static [string] Sentinel() { return 'VERIFY_SENTINEL_7C2E' }

    # CAPI failures carry an HResult whose setter is protected; the stand-in sets it as the runtime does.
    static [Exception] CryptoError([int]$hresult, [string]$message) {
        $exception = [Security.Cryptography.CryptographicException]::new($message)
        [Exception].GetProperty('HResult').SetValue($exception, $hresult)
        return $exception
    }

    static [bool] InSuite([string]$path) {
        if ([string]::IsNullOrEmpty($path) -or $path -match '^[A-Za-z][A-Za-z0-9]+:') { return $false }
        $root = [VerifyWorld]::SuiteRoot.TrimEnd('\') + '\'
        return [IO.Path]::GetFullPath($path).StartsWith($root, [StringComparison]::OrdinalIgnoreCase)
    }

    static [bool] InKeyArea([string]$path) {
        $area = [VerifyWorld]::KeyArea.TrimEnd('\')
        $full = [IO.Path]::GetFullPath($path).TrimEnd('\')
        if ($area -eq '') { return $false }
        return $full -ieq $area -or $full.StartsWith($area + '\', [StringComparison]::OrdinalIgnoreCase)
    }

    # The native class receives the caller's certificate; record whether it is alive and the object handed out.
    static [hashtable] CertificateFacts([object]$certificate, [string]$kind) {
        $base = $certificate
        if ($certificate -is [psobject]) { $base = $certificate.psobject.BaseObject }
        $thumbprint = [string]$base.Thumbprint
        $facts = [VerifyWorld]::Certificates[$thumbprint]
        $alive = $base.Handle -ne [IntPtr]::Zero
        $same = $null -ne $facts -and [object]::ReferenceEquals($base, $facts.Current)
        [VerifyWorld]::Add($kind, $thumbprint + '|alive=' + $alive + '|same=' + $same)
        if ($null -eq $facts) { [VerifyWorld]::Refuse('UnrepresentedCertificate', $thumbprint) }
        return $facts
    }
}

class VerifyEnvironment {
    static [string]$MachineName = ''
    static [string]$CommonApplicationData = ''
    static [string]$NewLine = "`r`n"

    static [string] GetFolderPath([object]$folder) {
        [VerifyWorld]::Add('GetFolderPath', [string]$folder)
        if ([string]$folder -cne 'CommonApplicationData') {
            [VerifyWorld]::Refuse('UnrepresentedFolder', [string]$folder)
        }
        return [VerifyEnvironment]::CommonApplicationData
    }
}

class VerifyPrincipal {
    static [bool]$Administrator = $true
    hidden [object]$Identity

    VerifyPrincipal([object]$identity) { $this.Identity = $identity }

    [bool] IsInRole([object]$role) {
        if ([string]$role -cne 'Administrator') { [VerifyWorld]::Refuse('UnrepresentedRole', [string]$role) }
        return [VerifyPrincipal]::Administrator
    }
}

class VerifyAccount {
    static [hashtable]$Sids = @{}
    [string]$Value

    VerifyAccount([string]$name) { $this.Value = $name }

    [object] Translate([type]$target) {
        [VerifyWorld]::Add('Translate', $this.Value)
        if ($target.FullName -cne 'System.Security.Principal.SecurityIdentifier' -or
            -not [VerifyAccount]::Sids.ContainsKey($this.Value)) {
            throw [Security.Principal.IdentityNotMappedException]::new(
                'Some or all identity references could not be translated.')
        }
        return [Security.Principal.SecurityIdentifier]::new([string][VerifyAccount]::Sids[$this.Value])
    }

    [string] ToString() { return $this.Value }
}

# Stands in for Dawnholder.WindowsSql.CertificateKeyNative. The hooks answer from the certificate's case facts.
class VerifyKeyNative {
    static [scriptblock]$ReadHook = $null
    static [scriptblock]$BindHook = $null

    static [object] ReadProviderParameters([object]$certificate) {
        return & ([VerifyKeyNative]::ReadHook) $certificate
    }

    static [void] AssertKeyBinding([object]$certificate) {
        & ([VerifyKeyNative]::BindHook) $certificate
    }
}

# Stands in for RSACryptoServiceProvider. Only the CspParameters constructor opens a registered container; the
# key-generating constructors and every key use are refused and recorded.
class VerifyCspKey {
    static [string[]]$InfoMembers = @('ProviderName', 'ProviderType', 'KeyContainerName', 'KeyNumber',
        'MachineKeyStore', 'Exportable', 'UniqueKeyContainerName')
    hidden [object]$Parameters
    hidden [hashtable]$Container
    [bool]$PersistKeyInCsp = $true
    [bool]$Disposed = $false

    VerifyCspKey() { [VerifyWorld]::Refuse('KeyCreation', 'RSACryptoServiceProvider()') }
    VerifyCspKey([int]$size) { [VerifyWorld]::Refuse('KeyCreation', 'RSACryptoServiceProvider(int)') }
    VerifyCspKey([int]$size, [object]$parameters) {
        [VerifyWorld]::Refuse('KeyCreation', 'RSACryptoServiceProvider(int,CspParameters)')
    }

    VerifyCspKey([object]$parameters) {
        [VerifyWorld]::Add('CspOpen', ('{0}|{1}|{2}|{3}|{4}' -f $parameters.ProviderName, $parameters.ProviderType,
                $parameters.KeyContainerName, $parameters.KeyNumber, [int]$parameters.Flags))
        $this.Parameters = $parameters
        $facts = [VerifyWorld]::Containers[[string]$parameters.KeyContainerName]
        if ($null -eq $facts) { throw [VerifyWorld]::CryptoError(-2146893802, 'Keyset does not exist') }
        if ([int]$facts.Open -ne 0) {
            throw [VerifyWorld]::CryptoError([int]$facts.Open, ($facts.OpenMessage + ' ' + [VerifyWorld]::Sentinel()))
        }
        $this.Container = $facts
        [VerifyWorld]::Keys.Add($this)
    }

    hidden [void] Use([string]$member) {
        if ($this.Disposed) {
            [VerifyWorld]::Add('UseAfterDispose', $member)
            throw [ObjectDisposedException]::new('RSACryptoServiceProvider')
        }
        [VerifyWorld]::Add('KeyRead', $member)
        $fault = $this.Container.Faults[$member]
        if ($fault -ceq 'Throw') {
            throw [VerifyWorld]::CryptoError(-2146893779, ('An internal error occurred. ' + [VerifyWorld]::Sentinel()))
        }
        if ($fault -ceq 'ThrowKeyset') { throw [VerifyWorld]::CryptoError(-2146893802, 'Keyset does not exist') }
    }

    [object] get_CspKeyContainerInfo() {
        $this.Use('get_CspKeyContainerInfo')
        if ($this.Container.Faults['get_CspKeyContainerInfo'] -ceq 'Null') { return $null }
        $info = [pscustomobject]@{ Owner = $this }
        foreach ($name in [VerifyCspKey]::InfoMembers) {
            $info | Add-Member -MemberType ScriptMethod -Name ('get_' + $name) `
                -Value ([scriptblock]::Create('$this.Owner.InfoValue(''' + $name + ''')'))
        }
        return $info
    }

    # Name, type, container, KeyNumber and machine flag echo the stored parameters; the rest asks the container.
    [object] InfoValue([string]$name) {
        $this.Use('get_' + $name)
        $p = $this.Parameters
        $machine = ([int]$p.Flags -band [int][Security.Cryptography.CspProviderFlags]::UseMachineKeyStore) -ne 0
        $values = @{
            ProviderName = $p.ProviderName
            ProviderType = $p.ProviderType
            KeyContainerName = $p.KeyContainerName
            KeyNumber = [Security.Cryptography.KeyNumber]$p.KeyNumber
            MachineKeyStore = $machine
            Exportable = [bool]$this.Container.Exportable
            UniqueKeyContainerName = $this.Container.UniqueName
        }
        return $values[$name]
    }

    [int] get_KeySize() {
        $this.Use('get_KeySize')
        return [int]$this.Container.KeySize
    }

    [bool] get_PublicOnly() {
        $this.Use('get_PublicOnly')
        return [bool]$this.Container.PublicOnly
    }

    [void] Dispose() {
        [VerifyWorld]::Add('KeyDispose', [string]$this.Parameters.KeyContainerName)
        $this.Disposed = $true
    }

    hidden [void] Refuse([string]$member) { [VerifyWorld]::Refuse('KeyUse', $member) }
    [object] ExportParameters([bool]$includePrivate) { $this.Refuse('ExportParameters'); return $null }
    [object] ExportCspBlob([bool]$includePrivate) { $this.Refuse('ExportCspBlob'); return $null }
    [object] ExportRSAPrivateKey() { $this.Refuse('ExportRSAPrivateKey'); return $null }
    [string] ToXmlString([bool]$includePrivate) { $this.Refuse('ToXmlString'); return $null }
    [object] Decrypt([object]$data, [object]$padding) { $this.Refuse('Decrypt'); return $null }
    [object] SignData([object]$data, [object]$name, [object]$padding) { $this.Refuse('SignData'); return $null }
    [object] SignHash([object]$hash, [object]$name, [object]$padding) { $this.Refuse('SignHash'); return $null }
    [void] Clear() { $this.Refuse('Clear') }
}

# =====================================================================================================
# 1. Fail-closed guards and store/ACL/file wrappers, defined before any product file is parsed.
# =====================================================================================================
foreach ($name in @('Restart-Service', 'Stop-Service', 'Start-Service', 'Set-Service', 'Get-Service',
        'Import-PfxCertificate', 'Export-PfxCertificate', 'Export-Certificate', 'Import-Certificate',
        'Get-NetFirewallRule', 'New-NetFirewallRule', 'Set-NetFirewallRule', 'Remove-NetFirewallRule',
        'Get-NetTCPConnection', 'Invoke-Sqlcmd', 'sqlcmd', 'netsh', 'reg', 'certutil', 'sc.exe', 'icacls', 'docker',
        'wsl', 'wsl.exe', 'Start-Process', 'Get-ItemProperty', 'Set-ItemProperty', 'New-ItemProperty',
        'Remove-ItemProperty', 'Get-WmiObject', 'Get-CimInstance', 'Invoke-CimMethod')) {
    $guard = { [VerifyWorld]::Refuse('ForbiddenCommand', $name) }.GetNewClosure()
    Set-Item -Path ('Function:script:' + $name) -Value $guard
}

# The directory lease compiles its own class; only a definition of the key native class is refused.
function Add-Type {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TypeDefinition)
    if ($TypeDefinition.Contains('CertificateKeyNative')) { [VerifyWorld]::Refuse('RefusedNativeCompile', 'Add-Type') }
    Microsoft.PowerShell.Utility\Add-Type -TypeDefinition $TypeDefinition -ErrorAction Stop
}

function Resolve-VerifyTarget {
    param([string]$LiteralPath, [string]$Path)
    if ($LiteralPath) { return $LiteralPath }
    return $Path
}

function New-VerifyCertificateObject {
    param([string]$Thumbprint)
    # Each hand-out is a new object: the product disposes what it receives.
    $facts = [VerifyWorld]::Certificates[$Thumbprint]
    $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new([byte[]]$facts.RawData)
    Add-Member -InputObject $certificate -MemberType ScriptProperty -Name HasPrivateKey -Value { $true } -Force
    Add-Member -InputObject $certificate -MemberType ScriptProperty -Name PrivateKey -Force -Value {
        # The laptop observation: the managed wrapper of an SChannel CSP key is not RSACryptoServiceProvider.
        [VerifyWorld]::Add('PrivateKeyRead', $this.Thumbprint)
        $wrapper = [pscustomobject]@{ Wrapper = 'RSACng-like wrapper of an SChannel CSP key' }
        $wrapper | Add-Member -MemberType ScriptMethod -Name Dispose -Value {
            [VerifyWorld]::Add('PrivateKeyDispose', '')
        }
        return $wrapper
    }
    $facts.Current = $certificate.psobject.BaseObject
    [VerifyWorld]::Handed.Add([pscustomobject]@{ Run = [VerifyWorld]::Run; Certificate = $certificate })
    return $certificate
}

function Get-Item {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '', [switch]$Force)
    $target = Resolve-VerifyTarget -LiteralPath $LiteralPath -Path $Path
    if ($target -match '^(?i)cert:') {
        $prefix = 'Cert:\LocalMachine\My\'
        [VerifyWorld]::Add('StoreRead', $target)
        if (-not $target.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            [VerifyWorld]::Refuse('UnrepresentedStore', $target)
        }
        $thumbprint = $target.Substring($prefix.Length)
        if (-not [VerifyWorld]::Store.ContainsKey($thumbprint)) {
            throw [Management.Automation.ItemNotFoundException]::new("Cannot find path '$target'.")
        }
        return New-VerifyCertificateObject -Thumbprint $thumbprint
    }
    if (-not [VerifyWorld]::InSuite($target)) { [VerifyWorld]::Refuse('PathOutsideSuite', 'Get-Item ' + $target) }
    return Microsoft.PowerShell.Management\Get-Item -LiteralPath $target -Force:$Force
}

function Remove-Item {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '', [switch]$DeleteKey, [switch]$Recurse, [switch]$Force)
    $target = Resolve-VerifyTarget -LiteralPath $LiteralPath -Path $Path
    if ($target -match '^(?i)cert:') { [VerifyWorld]::Refuse('StoreRemove', $target) }
    if (-not [VerifyWorld]::InSuite($target)) { [VerifyWorld]::Refuse('PathOutsideSuite', 'Remove-Item ' + $target) }
    [VerifyWorld]::Add('FileRemove', $target)
    Microsoft.PowerShell.Management\Remove-Item -LiteralPath $target -Recurse:$Recurse -Force:$Force
}

function Get-Acl {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '')
    $target = Resolve-VerifyTarget -LiteralPath $LiteralPath -Path $Path
    if (-not [VerifyWorld]::InSuite($target)) { [VerifyWorld]::Refuse('PathOutsideSuite', 'Get-Acl ' + $target) }
    if ([VerifyWorld]::InKeyArea($target)) {
        $plan = [VerifyWorld]::Acl
        $plan.Reads++
        [VerifyWorld]::Add('KeyAclRead', [IO.Path]::GetFullPath($target) + '#' + $plan.Reads)
        if ($plan.Reads -in $plan.ReadDeny) {
            throw [UnauthorizedAccessException]::new('Attempted to perform an unauthorized operation.')
        }
        if ($plan.Reads -in $plan.ReadNull) { return $null }
    }
    return Microsoft.PowerShell.Security\Get-Acl -LiteralPath $target
}

function Set-Acl {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '', $AclObject)
    $target = Resolve-VerifyTarget -LiteralPath $LiteralPath -Path $Path
    if (-not [VerifyWorld]::InSuite($target)) { [VerifyWorld]::Refuse('PathOutsideSuite', 'Set-Acl ' + $target) }
    if ([VerifyWorld]::InKeyArea($target)) {
        $mode = [VerifyWorld]::Acl.Set
        [VerifyWorld]::Add('KeyAclWrite', [IO.Path]::GetFullPath($target) + '|' + $mode)
        if ($mode -ceq 'Throw') {
            throw [UnauthorizedAccessException]::new('Attempted to perform an unauthorized operation.')
        }
        # A write the system silently did not apply; only the re-read can tell.
        if ($mode -ceq 'NoOp') { return }
    }
    Microsoft.PowerShell.Security\Set-Acl -LiteralPath $target -AclObject $AclObject -ErrorAction Stop
}

function Import-Module {
    [CmdletBinding()]
    param([Parameter(Position = 0)][string[]]$Name)
    [VerifyWorld]::Add('ImportModule', ($Name -join ','))
    if (@($Name).Count -ne 1 -or $Name[0] -cne 'PKI') {
        [VerifyWorld]::Refuse('UnrepresentedModule', ($Name -join ','))
    }
}

function New-SelfSignedCertificate {
    [CmdletBinding()]
    param($Type, $Subject, $DnsName, $CertStoreLocation, $Provider, $KeyAlgorithm, $KeyLength, $KeySpec,
        $KeyExportPolicy, $HashAlgorithm, $KeyUsage, $NotAfter)
    [VerifyWorld]::Add('CertificateCreate', ('{0}|{1}|{2}|{3}|{4}' -f $CertStoreLocation, $Provider, $KeyLength,
            $KeySpec, $KeyExportPolicy))
    $thumbprint = [VerifyWorld]::PendingCreation
    if (-not $thumbprint) { [VerifyWorld]::Refuse('UnplannedCertificateCreation', [string]$Subject) }
    [VerifyWorld]::PendingCreation = ''
    [VerifyWorld]::Store[$thumbprint] = $true
    return New-VerifyCertificateObject -Thumbprint $thumbprint
}

# Native class hooks: the provider info and binding answers of the certificate's case facts.
function Read-VerifyProviderInfo {
    param($Certificate)
    $facts = [VerifyWorld]::CertificateFacts($Certificate, 'NativeRead')
    $read = $facts.Read
    if ($read.Throw -ceq 'Raw') {
        throw [InvalidOperationException]::new('raw provider text ' + [VerifyWorld]::Sentinel())
    }
    if ($read.Throw -ceq 'RawDenied') { throw [VerifyWorld]::CryptoError(-2147024891, 'Access is denied.') }
    if ($read.Throw) { throw [InvalidOperationException]::new([string]$read.Throw) }
    if ($read.Null) { return $null }
    $parameters = [Security.Cryptography.CspParameters]::new([int]$read.Type, [string]$read.Provider,
        [string]$read.Container)
    $parameters.KeyNumber = [int]$read.KeySpec
    $parameters.Flags = $(if ($read.Machine) { [Security.Cryptography.CspProviderFlags]::UseMachineKeyStore }
        else { [Security.Cryptography.CspProviderFlags]::NoFlags })
    return $parameters
}

function Assert-VerifyKeyBinding {
    param($Certificate)
    $facts = [VerifyWorld]::CertificateFacts($Certificate, 'NativeBind')
    if ($facts.Bind -ceq 'Ok') { return }
    if ($facts.Bind -ceq 'Raw') {
        throw [InvalidOperationException]::new('raw binding text ' + [VerifyWorld]::Sentinel())
    }
    throw [InvalidOperationException]::new([string]$facts.Bind)
}

# =====================================================================================================
# 2. Register the stand-ins, then prove they bind before any product file is parsed (fail closed).
# =====================================================================================================
[VerifyWorld]::SuiteRoot = $script:SuiteRoot
[VerifyEnvironment]::MachineName = $script:FixtureMachine
[VerifyAccount]::Sids[$script:ServiceAccount] = $script:ServiceSid
[VerifyKeyNative]::ReadHook = ${function:Read-VerifyProviderInfo}
[VerifyKeyNative]::BindHook = ${function:Assert-VerifyKeyBinding}
$standIns = [ordered]@{
    'Dawnholder.WindowsSql.CertificateKeyNative' = [VerifyKeyNative]
    'RSACryptoServiceProvider' = [VerifyCspKey]
    'Security.Cryptography.RSACryptoServiceProvider' = [VerifyCspKey]
    'System.Security.Cryptography.RSACryptoServiceProvider' = [VerifyCspKey]
    'Environment' = [VerifyEnvironment]
    'System.Environment' = [VerifyEnvironment]
    'Security.Principal.WindowsPrincipal' = [VerifyPrincipal]
    'System.Security.Principal.WindowsPrincipal' = [VerifyPrincipal]
    'Security.Principal.NTAccount' = [VerifyAccount]
    'System.Security.Principal.NTAccount' = [VerifyAccount]
}
$accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
foreach ($typeName in $standIns.Keys) { $accelerators::Add($typeName, $standIns[$typeName]) }
$unbound = @(foreach ($typeName in $standIns.Keys) {
        $resolved = (& ([scriptblock]::Create('[' + $typeName + ']'))).FullName
        if ($resolved -cne $standIns[$typeName].FullName) { $typeName + '=' + $resolved }
    })
[VerifyWorld]::Run = 'ready'
$ready = & ([scriptblock]::Create(@'
$refused = $false
try { Add-Type -TypeDefinition 'namespace VerifyProbe { public static class CertificateKeyNative {} }' }
catch { $refused = $true }
$openRefused = $null
$probeParameters = [Security.Cryptography.CspParameters]::new(12, 'Microsoft RSA SChannel Cryptographic Provider',
    'verify-unregistered')
try { $null = [Security.Cryptography.RSACryptoServiceProvider]::new($probeParameters) }
catch {
    $inner = $_.Exception
    while ($null -ne $inner.InnerException) { $inner = $inner.InnerException }
    $openRefused = $inner.HResult
}
$created = $null
try { $null = [Security.Cryptography.RSACryptoServiceProvider]::new(2048) } catch { $created = 'refused' }
[pscustomobject]@{
    AddTypeFile = (Get-Command -Name 'Add-Type').ScriptBlock.File
    Refused = $refused
    ProbeLoaded = $null -ne ('VerifyProbe.CertificateKeyNative' -as [type])
    Lookup = ('Dawnholder.WindowsSql.CertificateKeyNative' -as [type]).FullName
    Machine = [Environment]::MachineName
    OpenRefused = $openRefused
    Creation = $created
}
'@))
$readyOk = $unbound.Count -eq 0 -and $ready.AddTypeFile -ceq $PSCommandPath -and $ready.Refused -and
-not $ready.ProbeLoaded -and $ready.Lookup -ceq 'VerifyKeyNative' -and $ready.Machine -ceq $script:FixtureMachine -and
$ready.OpenRefused -eq -2146893802 -and $ready.Creation -ceq 'refused'
Assert-True -Name 'VERIFY-READY-01 protected type names bind to stand-ins; Add-Type, CSP open and key creation refuse' `
    -Condition $readyOk -Detail ((@($unbound) -join ',') + ' ' + ($ready | ConvertTo-Json -Compress))
if (-not $readyOk) {
    Add-TestResult -Name 'VERIFY-READY-00 product code not executed: stand-ins are not bound' -Outcome FAIL
    Complete-TestSuite
}

. (Join-Path $script:ProductRoot 'WindowsSql.Common.ps1')
. (Join-Path $script:ProductRoot 'WindowsSql.Adapter.ps1')
$script:HasKeyBoundary = $null -ne (Microsoft.PowerShell.Core\Get-Command -Name 'Get-WindowsSqlCertificateKeyFile' `
        -CommandType Function -ErrorAction Ignore)
$script:NotRun = 'NOT_RUN: this product has no certificate key boundary functions'

# =====================================================================================================
# 3. Fixture helpers: key facts, a key area per case, public certificates and the service ACL.
# =====================================================================================================
function Write-JsonFile {
    param([string]$Path, $Value)
    [IO.File]::WriteAllText($Path, ($Value | ConvertTo-Json -Depth 8) + "`n", [Text.UTF8Encoding]::new($false))
}

function Get-FileSha256 {
    param([string]$Path)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($Path)))
}

function New-PublicCertificateBytes {
    # The signing key exists only for this call and is disposed at once; the fixture keeps the public bytes alone.
    $rsa = [Security.Cryptography.RSA]::Create(2048)
    try {
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            ('CN=' + $script:FixtureMachine), $rsa, [Security.Cryptography.HashAlgorithmName]::SHA256,
            [Security.Cryptography.RSASignaturePadding]::Pkcs1)
        $san = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
        $san.AddDnsName($script:FixtureMachine)
        $san.AddDnsName($script:TlsServerName)
        $request.CertificateExtensions.Add($san.Build())
        $oids = [Security.Cryptography.OidCollection]::new()
        [void]$oids.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.1'))
        $request.CertificateExtensions.Add(
            [Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new($oids, $false))
        $request.CertificateExtensions.Add([Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                [Security.Cryptography.X509Certificates.X509KeyUsageFlags]'DigitalSignature, KeyEncipherment', $true))
        $signed = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1), [DateTimeOffset]::UtcNow.AddDays(30))
        # The comma keeps the byte array whole instead of unrolling it into the pipeline.
        try { return , [byte[]]$signed.RawData }
        finally { $signed.Dispose() }
    }
    finally { $rsa.Dispose() }
}

function New-KeyFacts {
    param([string]$Id, [hashtable]$Read = @{}, [hashtable]$Container = @{}, [hashtable]$Other = @{})
    # Defaults are the supported key: SChannel CSP, AT_KEYEXCHANGE machine key, non-exportable RSA 2048.
    $readFacts = @{ Provider = $script:SChannel; Type = 12; Container = ('vk-' + $Id); KeySpec = 1; Machine = $true
        Throw = $null; Null = $false }
    foreach ($key in $Read.Keys) { $readFacts[$key] = $Read[$key] }
    $containerFacts = @{ Open = 0; OpenMessage = 'Keyset does not exist'; KeySize = 2048; PublicOnly = $false
        Exportable = $false; UniqueName = ('uk-' + $Id); Faults = @{}; Register = $true }
    foreach ($key in $Container.Keys) { $containerFacts[$key] = $Container[$key] }
    $facts = @{ Id = $Id; Read = $readFacts; Container = $containerFacts; Bind = 'Ok'; KeyFile = 'Present'
        Junction = $false; Decoys = $false }
    foreach ($key in $Other.Keys) { $facts[$key] = $Other[$key] }
    return $facts
}

$script:AreaCount = 0
function Initialize-KeyArea {
    param([hashtable]$Facts)
    # Each case gets its own ProgramData; the Environment stand-in answers it for CommonApplicationData.
    $script:AreaCount++
    $root = Join-Path $script:SuiteRoot ('areas\a' + $script:AreaCount)
    $programData = Join-Path $root 'pd'
    $rsaRoot = Join-Path $programData 'Microsoft\Crypto\RSA'
    $keyRoot = Join-Path $rsaRoot 'MachineKeys'
    $cngRoot = Join-Path $programData 'Microsoft\Crypto\Keys'
    [void][IO.Directory]::CreateDirectory($rsaRoot)
    [void][IO.Directory]::CreateDirectory($cngRoot)
    if ($Facts.Junction) {
        $real = Join-Path $root 'real-machinekeys'
        [void][IO.Directory]::CreateDirectory($real)
        $null = Microsoft.PowerShell.Management\New-Item -ItemType Junction -Path $keyRoot -Target $real
    }
    else { [void][IO.Directory]::CreateDirectory($keyRoot) }
    $exact = $null
    if ($Facts.KeyFile -cin @('Present', 'Directory')) {
        $exact = [IO.Path]::GetFullPath((Join-Path $keyRoot ([string]$Facts.Container.UniqueName)))
        if ($Facts.KeyFile -ceq 'Present') { [IO.File]::WriteAllText($exact, 'fixture key file, not a key') }
        else { [void][IO.Directory]::CreateDirectory($exact) }
    }
    # Decoys carry the container name, which the product must never use as a key file name.
    $decoys = @()
    if ($Facts.Decoys) {
        $decoys = @((Join-Path $keyRoot $Facts.Read.Container), (Join-Path $cngRoot $Facts.Read.Container))
        foreach ($decoy in $decoys) { [IO.File]::WriteAllText($decoy, 'decoy named after the container') }
    }
    [VerifyEnvironment]::CommonApplicationData = $programData
    [VerifyWorld]::KeyArea = $programData
    [VerifyWorld]::Acl = @{ ReadDeny = @(); ReadNull = @(); Set = 'Real'; Reads = 0 }
    if ($Facts.Container.Register) { [VerifyWorld]::Containers[[string]$Facts.Read.Container] = $Facts.Container }
    return [pscustomobject]@{ Root = $root; ProgramData = $programData; KeyRoot = $keyRoot; Exact = $exact
        Decoys = $decoys }
}

$script:CertificateBytes = @{}
function Register-CaseCertificate {
    param([hashtable]$Facts)
    # A new public certificate per case keeps case facts apart; the certificate holds no private key.
    $bytes = New-PublicCertificateBytes
    $probe = [Security.Cryptography.X509Certificates.X509Certificate2]::new([byte[]]$bytes)
    try { $thumbprint = $probe.Thumbprint } finally { $probe.Dispose() }
    [VerifyWorld]::Certificates[$thumbprint] = @{ RawData = $bytes; Read = $Facts.Read; Bind = $Facts.Bind
        Current = $null }
    return $thumbprint
}

function Get-ServiceRules {
    param([string]$Path)
    $acl = Microsoft.PowerShell.Security\Get-Acl -LiteralPath $Path
    return @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]) |
            Where-Object { $_.IdentityReference.Value -ceq $script:ServiceSid })
}

function Set-ServiceRule {
    param([string]$Path, [string]$Rights, [string]$Type)
    $acl = Microsoft.PowerShell.Security\Get-Acl -LiteralPath $Path
    $acl.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new(
            [Security.Principal.SecurityIdentifier]::new($script:ServiceSid), $Rights, $Type))
    Microsoft.PowerShell.Security\Set-Acl -LiteralPath $Path -AclObject $acl
}

function Get-RunEvents {
    param([string]$Run, [string[]]$Kind = @())
    return @([VerifyWorld]::Events | Where-Object {
            $_.Run -ceq $Run -and ($Kind.Count -eq 0 -or $_.Kind -cin $Kind)
        })
}

function Invoke-ProductScript {
    param([string]$Path, [hashtable]$Arguments)
    # The entry runs in this process; its stderr code is captured and exit leaves only the entry script.
    $errorWriter = [IO.StringWriter]::new()
    $previousError = [Console]::Error
    $global:LASTEXITCODE = -999
    $output = $null
    $escaped = $null
    [Console]::SetError($errorWriter)
    try { $output = & $Path @Arguments }
    catch { $escaped = $_.Exception.Message }
    finally { [Console]::SetError($previousError) }
    return [pscustomobject]@{
        Exit = $global:LASTEXITCODE
        Stdout = (@($output) -join "`n")
        Code = $errorWriter.ToString().Trim()
        Escaped = $escaped
    }
}

# =====================================================================================================
# 4. Runners: the real key file function, the real create entry and the real Assert gate.
# =====================================================================================================
function Invoke-KeyFileCase {
    param([hashtable]$Facts)
    $run = 'unit:' + $Facts.Id
    [VerifyWorld]::Run = $run
    $area = Initialize-KeyArea -Facts $Facts
    $thumbprint = Register-CaseCertificate -Facts $Facts
    $certificate = New-VerifyCertificateObject -Thumbprint $thumbprint
    $result = $null
    $code = $null
    if (-not $script:HasKeyBoundary) { $code = $script:NotRun }
    else {
        try { $result = Get-WindowsSqlCertificateKeyFile -Certificate $certificate }
        catch { $code = $_.Exception.Message }
    }
    # The caller owns the certificate: it is still alive after the call and disposed here.
    $alive = $certificate.Handle -ne [IntPtr]::Zero
    $certificate.Dispose()
    return [pscustomobject]@{ Id = $Facts.Id; Run = $run; Result = $result; Code = $code; Area = $area
        AliveAfter = $alive; Thumbprint = $thumbprint }
}

function New-ExpiryText {
    param([double]$Hours)
    return [DateTimeOffset]::UtcNow.AddHours($Hours).UtcDateTime.ToString('yyyy-MM-ddTHH:mm:ssZ',
        [Globalization.CultureInfo]::InvariantCulture)
}

function Invoke-CreateCase {
    param([hashtable]$Facts, [hashtable]$AclPlan = @{}, [scriptblock]$Prepare = $null)
    $run = 'create:' + $Facts.Id
    [VerifyWorld]::Run = $run
    $area = Initialize-KeyArea -Facts $Facts
    foreach ($key in $AclPlan.Keys) { [VerifyWorld]::Acl[$key] = $AclPlan[$key] }
    if ($null -ne $Prepare) { & $Prepare $area }
    $thumbprint = Register-CaseCertificate -Facts $Facts
    $root = Join-Path $script:SuiteRoot ('create\' + $Facts.Id)
    $publicRoot = Join-Path $root 'public'
    [void][IO.Directory]::CreateDirectory($publicRoot)
    $lifetime = [Guid]::NewGuid().ToString('N')
    $private = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $lifetime)
    $caPath = Join-Path $publicRoot 'public-ca.pem'
    $preparationPath = Join-Path $root 'preparation.json'
    Write-JsonFile -Path $preparationPath -Value ([ordered]@{
            Version = 1; ExecutionApproved = $true; ExpiresUtc = (New-ExpiryText -Hours 3); LifetimeId = $lifetime
            MachineName = $script:FixtureMachine; InstanceName = $script:FixtureInstance
            ExecutorSid = $script:ExecutorSid; ServiceAccount = $script:ServiceAccount
            TlsServerName = $script:TlsServerName; PrivateRoot = $private; PublicCaPath = $caPath
        })
    [VerifyWorld]::PendingCreation = $thumbprint
    $arguments = @{ PreparationPath = $preparationPath; ExpectedPreparationHash = (Get-FileSha256 $preparationPath) }
    try {
        $result = Invoke-ProductScript -Path (Join-Path $script:ProductRoot 'New-WindowsSqlCertificate.ps1') `
            -Arguments $arguments
    }
    finally { [VerifyWorld]::PendingCreation = '' }
    $createdPath = Join-Path $private 'certificate-created.txt'
    return [pscustomobject]@{
        Id = $Facts.Id; Run = $run; Result = $result; Area = $area; Thumbprint = $thumbprint; Facts = $Facts
        Private = $private; PublicRoot = $publicRoot; CaPath = $caPath; Lifetime = $lifetime
        CreatedThumbprint = $(if ([IO.File]::Exists($createdPath)) { [IO.File]::ReadAllText($createdPath) })
        ReceiptPath = (Join-Path $private 'certificate.json')
        ReceiptExists = [IO.File]::Exists((Join-Path $private 'certificate.json'))
        CaExists = [IO.File]::Exists($caPath)
    }
}

function Invoke-AssertAfterCreate {
    param($Created, [string]$Label, [scriptblock]$Change = $null)
    # The receipt, public CA and key ACL come from the real create entry; the change happens afterwards.
    $run = 'assert:' + $Created.Id + ':' + $Label
    [VerifyWorld]::Run = $run
    [VerifyEnvironment]::CommonApplicationData = $Created.Area.ProgramData
    [VerifyWorld]::KeyArea = $Created.Area.ProgramData
    [VerifyWorld]::Acl = @{ ReadDeny = @(); ReadNull = @(); Set = 'Real'; Reads = 0 }
    if (-not $Created.ReceiptExists -or -not $Created.CaExists) {
        return [pscustomobject]@{ Run = $run; Code = 'NOT_RUN: create produced no receipt or public CA' }
    }
    if ($null -ne $Change) { & $Change $Created }
    $adapter = [pscustomobject]@{
        Plan = [pscustomobject]@{
            PrivateRoot = $Created.Private; LifetimeId = $Created.Lifetime; MachineName = $script:FixtureMachine
            InstanceName = $script:FixtureInstance; CertificateThumbprint = $Created.Thumbprint
            SourceRoot = $Created.PublicRoot
            Inputs = @([pscustomobject]@{ Path = 'public-ca.pem'; Sha256 = (Get-FileSha256 $Created.CaPath) })
        }
        Runtime = [pscustomobject]@{
            ExecutorSid = $script:ExecutorSid; CertificateReceiptHash = (Get-FileSha256 $Created.ReceiptPath)
            TlsServerName = $script:TlsServerName; ServiceAccount = $script:ServiceAccount
            PublicCaPath = 'public-ca.pem'
        }
    }
    $code = $null
    try { Assert-WindowsSqlCertificate -Adapter $adapter }
    catch { $code = $_.Exception.Message }
    return [pscustomobject]@{ Run = $run; Code = $code }
}

# Call order, lifetime and refusal facts of one run; an empty list means the run kept every boundary rule.
function Get-KeyRunViolations {
    param([string]$Run, [string]$Container, [ValidateSet('Read', 'Open', 'Bind', 'Full')][string]$Reached)
    $events = @(Get-RunEvents -Run $Run)
    $kinds = @($events | ForEach-Object Kind)
    $violations = [Collections.Generic.List[string]]::new()
    foreach ($native in @($events | Where-Object Kind -CIn @('NativeRead', 'NativeBind'))) {
        if (-not $native.Detail.EndsWith('|alive=True|same=True')) { $violations.Add('certificate:' + $native.Detail) }
    }
    $expectedOpen = $script:SChannel + '|12|' + $Container + '|1|73'
    foreach ($open in @($events | Where-Object Kind -CEQ 'CspOpen')) {
        if ($open.Detail -cne $expectedOpen) { $violations.Add('open:' + $open.Detail) }
    }
    $count = @{ NativeRead = 0; CspOpen = 0; NativeBind = 0; KeyRead = 0; KeyDispose = 0 }
    foreach ($kind in $kinds) { if ($count.ContainsKey($kind)) { $count[$kind]++ } }
    # How far the decision may go before it stops: read only, the CSP open, the binding, or every getter.
    $limits = @{
        Read = @{ CspOpen = 0; NativeBind = 0; KeyRead = 0; KeyDispose = 0 }
        Open = @{ CspOpen = 1; NativeBind = 0; KeyRead = 0; KeyDispose = 0 }
        Bind = @{ CspOpen = 1; NativeBind = 1; KeyRead = 0; KeyDispose = 1 }
        Full = @{ CspOpen = 1; NativeBind = 1; KeyDispose = 1 }
    }[$Reached]
    if ($count.NativeRead -ne 1) { $violations.Add('nativeRead=' + $count.NativeRead) }
    foreach ($kind in $limits.Keys) {
        if ($count[$kind] -ne $limits[$kind]) { $violations.Add($kind + '=' + $count[$kind]) }
    }
    if ($Reached -ceq 'Full') {
        $order = @(@('NativeRead', 'CspOpen', 'NativeBind', 'KeyRead', 'KeyDispose') |
                ForEach-Object { [Array]::IndexOf($kinds, $_) })
        for ($i = 1; $i -lt $order.Count; $i++) {
            if ($order[$i] -le $order[$i - 1]) {
                $violations.Add('order:' + ($kinds -join '>'))
                break
            }
        }
        if ([Array]::LastIndexOf($kinds, 'KeyRead') -gt [Array]::IndexOf($kinds, 'KeyDispose')) {
            $violations.Add('read after dispose')
        }
    }
    foreach ($kind in @('UseAfterDispose', 'KeyUse', 'KeyCreation', 'PrivateKeyRead', 'RefusedNativeCompile')) {
        if ($kinds -ccontains $kind) { $violations.Add($kind) }
    }
    return @($violations)
}

# =====================================================================================================
# 5. The key decision through the real Get-WindowsSqlCertificateKeyFile. Expected codes are requirement meanings:
#    Metadata (cannot be proven), Access (the key cannot be reached), Unsupported (not a SQL key) and path codes.
# =====================================================================================================
$ksp = 'Microsoft Software Key Storage Provider'
$aes = 'Microsoft Enhanced RSA and AES Cryptographic Provider'
$keyCases = @(
    @{ Id = 'supported'; Expect = 'Supported'; Reached = 'Full' },
    @{ Id = 'container-260'; Expect = 'Supported'; Reached = 'Full'; Read = @{ Container = ('c' * 260) } },
    @{ Id = 'keysize-4096'; Expect = 'Supported'; Reached = 'Full'; Container = @{ KeySize = 4096 } },
    @{ Id = 'decoys-unused'; Expect = 'Supported'; Reached = 'Full'; Other = @{ Decoys = $true } },
    @{ Id = 'read-missing'; Expect = 'Metadata'; Reached = 'Read'; Read = @{ Throw = $script:Codes.Metadata } },
    @{ Id = 'read-raw'; Expect = 'Metadata'; Reached = 'Read'; Read = @{ Throw = 'Raw' } },
    @{ Id = 'read-null'; Expect = 'Metadata'; Reached = 'Read'; Read = @{ Null = $true } },
    @{ Id = 'container-empty'; Expect = 'Metadata'; Reached = 'Read'; Read = @{ Container = '' } },
    @{ Id = 'container-space'; Expect = 'Metadata'; Reached = 'Read'; Read = @{ Container = '  ' } },
    @{ Id = 'container-261'; Expect = 'Metadata'; Reached = 'Read'; Read = @{ Container = ('c' * 261) } },
    @{ Id = 'container-nul'; Expect = 'Metadata'; Reached = 'Read'; Read = @{ Container = ('vk' + [char]0 + 'x') } },
    @{ Id = 'read-denied'; Expect = 'Access'; Reached = 'Read'; Read = @{ Throw = $script:Codes.Access } },
    @{ Id = 'read-raw-denied'; Expect = 'Access'; Reached = 'Read'; Read = @{ Throw = 'RawDenied' } },
    @{ Id = 'read-unsupported'; Expect = 'Unsupported'; Reached = 'Read'
        Read = @{ Throw = $script:Codes.Unsupported } },
    @{ Id = 'provider-aes'; Expect = 'Unsupported'; Reached = 'Read'; Read = @{ Provider = $aes; Type = 24 } },
    @{ Id = 'provider-ksp'; Expect = 'Unsupported'; Reached = 'Read'
        Read = @{ Provider = $ksp; Type = 0; KeySpec = 0 } },
    @{ Id = 'provider-case'; Expect = 'Unsupported'; Reached = 'Read'
        Read = @{ Provider = $script:SChannel.ToLower() } },
    @{ Id = 'schannel-type-24'; Expect = 'Unsupported'; Reached = 'Read'; Read = @{ Type = 24 } },
    @{ Id = 'keyspec-signature'; Expect = 'Unsupported'; Reached = 'Read'; Read = @{ KeySpec = 2 } },
    @{ Id = 'keyspec-zero'; Expect = 'Unsupported'; Reached = 'Read'; Read = @{ KeySpec = 0 } },
    @{ Id = 'user-key'; Expect = 'Unsupported'; Reached = 'Read'; Read = @{ Machine = $false } },
    @{ Id = 'open-keyset'; Expect = 'Access'; Reached = 'Open'; Container = @{ Register = $false } },
    @{ Id = 'open-denied'; Expect = 'Access'; Reached = 'Open'; Container = @{ Open = -2147024891 } },
    @{ Id = 'open-no-key'; Expect = 'Access'; Reached = 'Open'; Container = @{ Open = -2146893811 } },
    @{ Id = 'open-silent'; Expect = 'Access'; Reached = 'Open'; Container = @{ Open = -2146893790 } },
    @{ Id = 'open-keyset-not-defined'; Expect = 'Access'; Reached = 'Open'; Container = @{ Open = -2146893799 } },
    @{ Id = 'open-perm'; Expect = 'Access'; Reached = 'Open'; Container = @{ Open = -2146893808 } },
    @{ Id = 'open-internal'; Expect = 'Access'; Reached = 'Open'; Container = @{ Open = -2146893779 } },
    @{ Id = 'bind-mismatch'; Expect = 'Access'; Reached = 'Bind'; Other = @{ Bind = $script:Codes.Access } },
    @{ Id = 'bind-raw'; Expect = 'Access'; Reached = 'Bind'; Other = @{ Bind = 'Raw' } },
    @{ Id = 'bind-borrowed-handle'; Expect = 'Metadata'; Reached = 'Bind'; Other = @{ Bind = $script:Codes.Metadata } },
    @{ Id = 'bind-signature'; Expect = 'Unsupported'; Reached = 'Bind'; Other = @{ Bind = $script:Codes.Unsupported } },
    @{ Id = 'info-null'; Expect = 'Metadata'; Reached = 'Full'
        Container = @{ Faults = @{ get_CspKeyContainerInfo = 'Null' } } },
    @{ Id = 'info-throw'; Expect = 'Metadata'; Reached = 'Full'
        Container = @{ Faults = @{ get_CspKeyContainerInfo = 'Throw' } } },
    @{ Id = 'exportable-throw'; Expect = 'Metadata'; Reached = 'Full'
        Container = @{ Faults = @{ get_Exportable = 'Throw' } } },
    @{ Id = 'exportable-keyset'; Expect = 'Access'; Reached = 'Full'
        Container = @{ Faults = @{ get_Exportable = 'ThrowKeyset' } } },
    @{ Id = 'unique-throw'; Expect = 'Metadata'; Reached = 'Full'
        Container = @{ Faults = @{ get_UniqueKeyContainerName = 'Throw' } } },
    @{ Id = 'keysize-throw'; Expect = 'Metadata'; Reached = 'Full'
        Container = @{ Faults = @{ get_KeySize = 'Throw' } } },
    @{ Id = 'publiconly-throw'; Expect = 'Metadata'; Reached = 'Full'
        Container = @{ Faults = @{ get_PublicOnly = 'Throw' } } },
    @{ Id = 'exportable'; Expect = 'Unsupported'; Reached = 'Full'; Container = @{ Exportable = $true } },
    @{ Id = 'public-only'; Expect = 'Unsupported'; Reached = 'Full'; Container = @{ PublicOnly = $true } },
    @{ Id = 'keysize-2047'; Expect = 'Unsupported'; Reached = 'Full'; Container = @{ KeySize = 2047 } },
    @{ Id = 'unique-null'; Expect = 'Metadata'; Reached = 'Full'; Container = @{ UniqueName = $null }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-empty'; Expect = 'Metadata'; Reached = 'Full'; Container = @{ UniqueName = '' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-space'; Expect = 'Metadata'; Reached = 'Full'; Container = @{ UniqueName = ' ' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-dot'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = '.' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-dotdot'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = '..' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-trailing-dot'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = 'k.' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-trailing-space'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = 'k ' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-slash'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = 'a/b' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-backslash'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = '..\escape' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-stream'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = 'k:stream' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-256'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = ('k' * 256) }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'unique-device'; Expect = 'PathInvalid'; Reached = 'Full'; Container = @{ UniqueName = 'CON' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'key-directory'; Expect = 'PathInvalid'; Reached = 'Full'; Other = @{ KeyFile = 'Directory' } },
    @{ Id = 'key-missing'; Expect = 'PathInvalid'; Reached = 'Full'; Other = @{ KeyFile = 'None' } },
    @{ Id = 'decoys-key-missing'; Expect = 'PathInvalid'; Reached = 'Full'
        Other = @{ KeyFile = 'None'; Decoys = $true } },
    @{ Id = 'machinekeys-junction'; Expect = 'PathReparse'; Reached = 'Full'; Other = @{ Junction = $true } }
)
$script:AllCodes = [Collections.Generic.List[string]]::new()
foreach ($case in $keyCases) {
    $read = if ($case.ContainsKey('Read')) { $case.Read } else { @{} }
    $container = if ($case.ContainsKey('Container')) { $case.Container } else { @{} }
    $other = if ($case.ContainsKey('Other')) { $case.Other } else { @{} }
    $facts = New-KeyFacts -Id $case.Id -Read $read -Container $container -Other $other
    $r = Invoke-KeyFileCase -Facts $facts
    if ($null -ne $r.Code) { $script:AllCodes.Add([string]$r.Code) }
    $prefix = 'VERIFY-KEY ' + $case.Id + ': '
    if ($case.Expect -ceq 'Supported') {
        Assert-True -Name ($prefix + 'returns exactly MachineKeys\<UniqueKeyContainerName>') `
            -Condition ($null -eq $r.Code -and $null -ne $r.Area.Exact -and [string]$r.Result -ceq $r.Area.Exact) `
            -Detail ('code=' + $r.Code + ' result=' + $r.Result)
    }
    else {
        $expected = $script:Codes[$case.Expect]
        Assert-True -Name ($prefix + 'rejects with ' + $expected) -Condition ([string]$r.Code -ceq $expected) `
            -Detail ([string]$r.Code)
    }
    if (-not $script:HasKeyBoundary) { continue }
    $violations = @(Get-KeyRunViolations -Run $r.Run -Container ([string]$facts.Read.Container) -Reached $case.Reached)
    if (-not $r.AliveAfter) { $violations += 'caller certificate disposed by the product' }
    $stopName = $prefix + 'stops at ' + $case.Reached + '; same live certificate, exact CSP reopen, key disposed'
    Assert-True -Name $stopName `
        -Condition ($violations.Count -eq 0) -Detail ($violations -join ',')
}

# ---- 5a. Only the closed key codes cross the failure mapping; access HResults are access failures.
$mapping = @(
    @{ Id = 'closed-code-wrapped'; Default = $script:Codes.Metadata; Expect = $script:Codes.Unsupported
        Make = { [Management.Automation.MethodInvocationException]::new('wrapped',
                [InvalidOperationException]::new('WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED')) } },
    @{ Id = 'raw-metadata-default'; Default = $script:Codes.Metadata; Expect = $script:Codes.Metadata
        Make = { [InvalidOperationException]::new('raw text VERIFY_SENTINEL_7C2E') } },
    @{ Id = 'raw-access-default'; Default = $script:Codes.Access; Expect = $script:Codes.Access
        Make = { [InvalidOperationException]::new('raw text VERIFY_SENTINEL_7C2E') } },
    @{ Id = 'other-public-code'; Default = $script:Codes.Metadata; Expect = $script:Codes.Metadata
        Make = { [InvalidOperationException]::new('WINDOWS_SQL_PATH_INVALID') } },
    @{ Id = 'unauthorized'; Default = $script:Codes.Metadata; Expect = $script:Codes.Access
        Make = { [UnauthorizedAccessException]::new('denied VERIFY_SENTINEL_7C2E') } },
    @{ Id = 'nested-unauthorized'; Default = $script:Codes.Metadata; Expect = $script:Codes.Access
        Make = { [Exception]::new('outer', [UnauthorizedAccessException]::new('inner')) } },
    @{ Id = 'outer-code-first'; Default = $script:Codes.Access; Expect = $script:Codes.Metadata
        Make = { [InvalidOperationException]::new('WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE',
                [UnauthorizedAccessException]::new('inner')) } },
    @{ Id = 'nte-fail-not-access'; Default = $script:Codes.Metadata; Expect = $script:Codes.Metadata
        Make = { [VerifyWorld]::CryptoError(-2146893792, 'NTE_FAIL VERIFY_SENTINEL_7C2E') } }
)
foreach ($hresult in @(-2147024891, -2146893808, -2146893802, -2146893811, -2146893799, -2146893790)) {
    $mapping += @{ Id = ('hresult-' + ('{0:X8}' -f $hresult)); Default = $script:Codes.Metadata
        Expect = $script:Codes.Access
        Make = { [Management.Automation.MethodInvocationException]::new('wrapped',
                [VerifyWorld]::CryptoError($hresult, 'VERIFY_SENTINEL_7C2E')) }.GetNewClosure() }
}
foreach ($case in $mapping) {
    $actual = $script:NotRun
    if ($script:HasKeyBoundary) {
        $actual = Get-WindowsSqlCertificateKeyFailureCode -Exception (& $case.Make) -DefaultCode $case.Default
    }
    $script:AllCodes.Add([string]$actual)
    Assert-Equal -Name ('VERIFY-MAP ' + $case.Id + ': maps to its closed public code') -Expected $case.Expect `
        -Actual $actual
}

# ---- 5b. The key ACL read and the service rule check on a real suite file.
[VerifyWorld]::Run = 'acl'
$aclArea = Initialize-KeyArea -Facts (New-KeyFacts -Id 'acl-read')
function Invoke-AclCheck {
    param([scriptblock]$Action)
    if (-not $script:HasKeyBoundary) { return $script:NotRun }
    try { $null = & $Action; return 'PASSED' }
    catch { return $_.Exception.Message }
}
$aclCases = @(
    @{ Id = 'read'; Expect = 'PASSED'; Action = { Read-WindowsSqlCertificateKeyAcl -KeyFile $aclArea.Exact } },
    @{ Id = 'read-denied'; Expect = $script:Codes.AclAccess; Plan = @{ ReadDeny = @(1) }
        Action = { Read-WindowsSqlCertificateKeyAcl -KeyFile $aclArea.Exact } },
    @{ Id = 'read-null'; Expect = $script:Codes.AclAccess; Plan = @{ ReadNull = @(1) }
        Action = { Read-WindowsSqlCertificateKeyAcl -KeyFile $aclArea.Exact } },
    @{ Id = 'rules-unreadable'; Expect = $script:Codes.AclAccess
        Action = {
            $broken = [pscustomobject]@{}
            $broken | Add-Member -MemberType ScriptMethod -Name GetAccessRules -Value { throw 'VERIFY_SENTINEL_7C2E' }
            Assert-WindowsSqlCertificateServiceAcl -Acl $broken -ServiceSid $script:ServiceSid
        } }
)
$ruleCases = @(
    @{ Id = 'no-rule'; Expect = $script:Codes.AclInvalid; Rules = @() },
    @{ Id = 'allow-read'; Expect = 'PASSED'; Rules = @(, @('Read', 'Allow')) },
    @{ Id = 'allow-full'; Expect = 'PASSED'; Rules = @(, @('FullControl', 'Allow')) },
    @{ Id = 'read-attributes-only'; Expect = $script:Codes.AclInvalid; Rules = @(, @('ReadAttributes', 'Allow')) },
    @{ Id = 'deny-and-allow'; Expect = $script:Codes.AclInvalid; Rules = @(@('Read', 'Allow'), @('Read', 'Deny')) },
    @{ Id = 'deny-write-only'; Expect = $script:Codes.AclInvalid; Rules = @(@('Read', 'Allow'), @('Write', 'Deny')) }
)
foreach ($case in $aclCases) {
    [VerifyWorld]::Acl = @{ ReadDeny = @(); ReadNull = @(); Set = 'Real'; Reads = 0 }
    if ($case.ContainsKey('Plan')) { foreach ($key in $case.Plan.Keys) { [VerifyWorld]::Acl[$key] = $case.Plan[$key] } }
    $actual = Invoke-AclCheck -Action $case.Action
    $script:AllCodes.Add([string]$actual)
    Assert-Equal -Name ('VERIFY-ACL ' + $case.Id + ': key ACL read result') -Expected $case.Expect -Actual $actual
}
foreach ($case in $ruleCases) {
    $file = Join-Path $aclArea.KeyRoot ('rules-' + $case.Id)
    [IO.File]::WriteAllText($file, 'fixture key file, not a key')
    foreach ($rule in $case.Rules) { Set-ServiceRule -Path $file -Rights $rule[0] -Type $rule[1] }
    $acl = Microsoft.PowerShell.Security\Get-Acl -LiteralPath $file
    $actual = Invoke-AclCheck -Action {
        Assert-WindowsSqlCertificateServiceAcl -Acl $acl -ServiceSid $script:ServiceSid
    }
    $script:AllCodes.Add([string]$actual)
    Assert-Equal -Name ('VERIFY-ACL ' + $case.Id + ': service rule decision') -Expected $case.Expect -Actual $actual
}

# =====================================================================================================
# 6. The real create entry: key decision, then the exact key ACL write, its re-read, then PEM and receipt.
# =====================================================================================================
function Get-KeyAclWrites {
    param([string]$Run)
    return @(Get-RunEvents -Run $Run -Kind 'KeyAclWrite' | ForEach-Object { $_.Detail.Split('|')[0] })
}

$createCases = @(
    @{ Id = 'c-supported'; Expect = 'Supported' },
    @{ Id = 'c-deny-existing'; Expect = 'AclInvalid'; Writes = 0
        Prepare = { param($area) Set-ServiceRule -Path $area.Exact -Rights 'Read' -Type 'Deny' } },
    @{ Id = 'c-acl-read-denied'; Expect = 'AclAccess'; Writes = 0; Plan = @{ ReadDeny = @(1) } },
    @{ Id = 'c-acl-read-null'; Expect = 'AclAccess'; Writes = 0; Plan = @{ ReadNull = @(1) } },
    @{ Id = 'c-set-acl-throws'; Expect = 'AclAccess'; Writes = 1; Plan = @{ Set = 'Throw' } },
    @{ Id = 'c-set-acl-not-applied'; Expect = 'AclInvalid'; Writes = 1; Plan = @{ Set = 'NoOp' } },
    @{ Id = 'c-reread-denied'; Expect = 'AclAccess'; Writes = 1; Plan = @{ ReadDeny = @(2) } },
    @{ Id = 'c-unsupported-ksp'; Expect = 'Unsupported'; Writes = 0
        Read = @{ Provider = 'Microsoft Software Key Storage Provider'; Type = 0; KeySpec = 0 } },
    @{ Id = 'c-open-denied'; Expect = 'Access'; Writes = 0; Container = @{ Open = -2147024891 } },
    @{ Id = 'c-raw-provider-text'; Expect = 'Metadata'; Writes = 0; Read = @{ Throw = 'Raw' } },
    @{ Id = 'c-path-escape'; Expect = 'PathInvalid'; Writes = 0; Container = @{ UniqueName = '..' }
        Other = @{ KeyFile = 'None' } },
    @{ Id = 'c-decoys-key-missing'; Expect = 'PathInvalid'; Writes = 0; Other = @{ KeyFile = 'None'; Decoys = $true } }
)
$created = @{}
foreach ($case in $createCases) {
    $read = if ($case.ContainsKey('Read')) { $case.Read } else { @{} }
    $container = if ($case.ContainsKey('Container')) { $case.Container } else { @{} }
    $other = if ($case.ContainsKey('Other')) { $case.Other } else { @{} }
    $facts = New-KeyFacts -Id $case.Id -Read $read -Container $container -Other $other
    $plan = if ($case.ContainsKey('Plan')) { $case.Plan } else { @{} }
    $prepare = if ($case.ContainsKey('Prepare')) { $case.Prepare } else { $null }
    $c = Invoke-CreateCase -Facts $facts -AclPlan $plan -Prepare $prepare
    $created[$case.Id] = $c
    if ($c.Result.Code) { $script:AllCodes.Add($c.Result.Code) }
    $writes = @(Get-KeyAclWrites -Run $c.Run)
    $prefix = 'VERIFY-CREATE ' + $case.Id + ': '
    if ($case.Expect -ceq 'Supported') {
        $status = $null
        try { $status = $c.Result.Stdout | ConvertFrom-Json } catch { $status = $null }
        $prepared = $c.Result.Exit -eq 0 -and $null -ne $status -and $status.Status -ceq 'Prepared' -and
        $status.Thumbprint -ceq $c.Thumbprint
        Assert-True -Name ($prefix + 'supported key prepares the certificate (exit 0, Prepared, exact thumbprint)') `
            -Condition $prepared `
            -Detail ('exit=' + $c.Result.Exit + ' code=' + $c.Result.Code + ' ' + $c.Result.Escaped)
        $rules = @(Get-ServiceRules -Path $c.Area.Exact)
        $exactRule = $rules.Count -eq 1 -and $rules[0].AccessControlType -eq 'Allow' -and
        ($rules[0].FileSystemRights -band [Security.AccessControl.FileSystemRights]::Read) -eq
        [Security.AccessControl.FileSystemRights]::Read
        Assert-True -Name ($prefix + 'one ACL write, to the exact key file, leaves one service Allow Read rule') `
            -Condition ($exactRule -and $writes.Count -eq 1 -and $writes[0] -ceq $c.Area.Exact) `
            -Detail ('rules=' + $rules.Count + ' writes=' + ($writes -join ','))
        $receipt = $null
        if ($c.ReceiptExists) { $receipt = [IO.File]::ReadAllText($c.ReceiptPath) | ConvertFrom-Json }
        $bound = $null -ne $receipt -and $receipt.KeyFile -ceq $c.Area.Exact -and
        $receipt.ServiceSid -ceq $script:ServiceSid -and $receipt.Thumbprint -ceq $c.Thumbprint -and $c.CaExists -and
        $receipt.PublicCaSha256 -ceq (Get-FileSha256 $c.CaPath) -and $c.CreatedThumbprint -ceq $c.Thumbprint
        Assert-True -Name ($prefix + 'receipt binds the exact key file, service SID, thumbprint and public CA') `
            -Condition $bound -Detail $(if ($null -ne $receipt) { $receipt | ConvertTo-Json -Compress })
        $creation = @(Get-RunEvents -Run $c.Run -Kind 'CertificateCreate' | ForEach-Object Detail)
        $expectedCreation = 'Cert:\LocalMachine\My|' + $script:SChannel + '|2048|KeyExchange|NonExportable'
        Assert-True -Name ($prefix + 'creation asks for an SChannel CSP, KeyExchange, non-exportable RSA 2048 key') `
            -Condition ($creation.Count -eq 1 -and $creation[0] -ceq $expectedCreation) -Detail ($creation -join ',')
        continue
    }
    $expected = $script:Codes[$case.Expect]
    Assert-True -Name ($prefix + 'rejected with ' + $expected) `
        -Condition ($c.Result.Exit -eq 1 -and $c.Result.Code -ceq $expected -and $null -eq $c.Result.Escaped) `
        -Detail ('exit=' + $c.Result.Exit + ' code=' + $c.Result.Code + ' escaped=' + $c.Result.Escaped)
    $kept = $c.CreatedThumbprint -ceq $c.Thumbprint -and -not $c.CaExists -and -not $c.ReceiptExists -and
    [IO.Directory]::Exists($c.Private)
    $removals = @(Get-RunEvents -Run $c.Run -Kind @('StoreRemove', 'FileRemove'))
    Assert-True -Name ($prefix + 'thumbprint stays recorded; no public CA, receipt or removal') `
        -Condition ($kept -and $removals.Count -eq 0) -Detail ('created=' + $c.CreatedThumbprint + ' ca=' +
        $c.CaExists + ' receipt=' + $c.ReceiptExists + ' removals=' + $removals.Count)
    $writesOk = $writes.Count -eq $case.Writes -and @($writes | Where-Object { $_ -cne $c.Area.Exact }).Count -eq 0
    $decoyRules = @(foreach ($decoy in $c.Area.Decoys) { Get-ServiceRules -Path $decoy })
    Assert-True -Name ($prefix + 'key ACL writes: exactly ' + $case.Writes + ', only to the exact key file') `
        -Condition ($writesOk -and $decoyRules.Count -eq 0) `
        -Detail ('writes=' + ($writes -join ',') + ' decoyRules=' + $decoyRules.Count)
}
$denyRules = @(Get-ServiceRules -Path $created['c-deny-existing'].Area.Exact)
Assert-True -Name 'VERIFY-CREATE c-deny-existing: the pre-existing Deny is left as found (no write)' `
    -Condition ($denyRules.Count -eq 1 -and $denyRules[0].AccessControlType -eq 'Deny') `
    -Detail ('rules=' + $denyRules.Count)

# =====================================================================================================
# 7. The real Assert gate after a real creation: one key decision, the receipt's key file and service ACL.
# =====================================================================================================
$assertCases = @(
    @{ Id = 'a-supported'; Expect = $null },
    @{ Id = 'a-provider-now-ksp'; Expect = $script:Codes.Unsupported
        Change = { param($c) $c.Facts.Read.Provider = 'Microsoft Software Key Storage Provider'
            $c.Facts.Read.Type = 0 } },
    @{ Id = 'a-key-file-moved'; Expect = 'WINDOWS_SQL_CERTIFICATE_KEY_CHANGED'
        Change = { param($c)
            $moved = Join-Path $c.Area.KeyRoot 'uk-moved'
            [IO.File]::WriteAllText($moved, 'fixture key file, not a key')
            Set-ServiceRule -Path $moved -Rights 'Read' -Type 'Allow'
            $c.Facts.Container.UniqueName = 'uk-moved' } },
    @{ Id = 'a-decoy-only'; Expect = $script:Codes.PathInvalid
        Change = { param($c)
            $decoy = Join-Path $c.Area.KeyRoot $c.Facts.Read.Container
            [IO.File]::WriteAllText($decoy, 'decoy named after the container')
            $c.Facts.Container.UniqueName = 'uk-gone' } },
    @{ Id = 'a-service-deny'; Expect = $script:Codes.AclInvalid
        Change = { param($c) Set-ServiceRule -Path $c.Area.Exact -Rights 'Read' -Type 'Deny' } },
    @{ Id = 'a-acl-read-denied'; Expect = $script:Codes.AclAccess
        Change = { param($c) [VerifyWorld]::Acl.ReadDeny = @(1) } },
    @{ Id = 'a-open-denied'; Expect = $script:Codes.Access
        Change = { param($c) $c.Facts.Container.Open = -2147024891 } },
    @{ Id = 'a-binding-mismatch'; Expect = $script:Codes.Access
        Change = { param($c)
            [VerifyWorld]::Certificates[$c.Thumbprint].Bind = 'WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED' } }
)
foreach ($case in $assertCases) {
    $c = Invoke-CreateCase -Facts (New-KeyFacts -Id $case.Id)
    $change = if ($case.ContainsKey('Change')) { $case.Change } else { $null }
    $a = Invoke-AssertAfterCreate -Created $c -Label 'gate' -Change $change
    if ($a.Code) { $script:AllCodes.Add([string]$a.Code) }
    $privateKeyReads = @(Get-RunEvents -Run $a.Run -Kind 'PrivateKeyRead')
    $outcome = if ($case.Expect) { $case.Expect } else { 'passes' }
    Assert-True -Name ('VERIFY-ASSERT ' + $case.Id + ': gate result ' + $outcome) `
        -Condition ([string]$a.Code -ceq [string]$case.Expect -and $privateKeyReads.Count -eq 0) `
        -Detail ('code=' + $a.Code + ' privateKeyReads=' + $privateKeyReads.Count)
}

# =====================================================================================================
# 8. Source order and surfaces that behaviour alone cannot show.
# =====================================================================================================
$removeTokens = $null
$removeErrors = $null
$removeAst = [Management.Automation.Language.Parser]::ParseFile(
    (Join-Path $script:ProductRoot 'Remove-WindowsSqlCertificate.ps1'), [ref]$removeTokens, [ref]$removeErrors)
function Test-CommandHasParameter {
    param($Command, [string]$Parameter)
    if (-not $Parameter) { return $true }
    $named = @($Command.CommandElements | Where-Object {
            $_ -is [Management.Automation.Language.CommandParameterAst] -and $_.ParameterName -ceq $Parameter
        })
    return $named.Count -gt 0
}
function Get-FirstCommandOffset {
    param($Ast, [string]$Name, [string]$Parameter = '')
    $found = @($Ast.FindAll({ param($node)
                $node -is [Management.Automation.Language.CommandAst] -and $node.GetCommandName() -ceq $Name -and
                (Test-CommandHasParameter -Command $node -Parameter $Parameter)
            }, $true) | ForEach-Object { $_.Extent.StartOffset } | Sort-Object)
    if ($found.Count -eq 0) { return -1 }
    return $found[0]
}
$gateAt = Get-FirstCommandOffset -Ast $removeAst -Name 'Assert-WindowsSqlCertificate'
$journalAt = Get-FirstCommandOffset -Ast $removeAst -Name 'Write-WindowsSqlPrivateJson'
$deleteAt = Get-FirstCommandOffset -Ast $removeAst -Name 'Remove-Item' -Parameter 'DeleteKey'
Assert-True -Name 'VERIFY-STATIC-01 remove runs the key gate before the cleanup journal and the key deletion' `
    -Condition ($gateAt -ge 0 -and $journalAt -gt $gateAt -and $deleteAt -gt $journalAt) `
    -Detail ('gate=' + $gateAt + ' journal=' + $journalAt + ' delete=' + $deleteAt)

# Key code never reads or exports the private key through the managed wrapper, and never touches persistence.
$forbiddenSurface = '^(?:.*\.)?(PrivateKey|GetRSAPrivateKey|ExportParameters|ExportCspBlob|ExportRSAPrivateKey|' +
'ToXmlString|PersistKeyInCsp|X509Store|CngKey|Import-PfxCertificate|Export-PfxCertificate|Export-Certificate)$'
$surfaceHits = [Collections.Generic.List[string]]::new()
foreach ($file in @('New-WindowsSqlCertificate.ps1', 'WindowsSql.Certificate.ps1', 'WindowsSql.CertificateKey.ps1')) {
    $path = Join-Path $script:ProductRoot $file
    if (-not [IO.File]::Exists($path)) { continue }
    $tokens = $null
    $parseErrors = $null
    $null = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$parseErrors)
    foreach ($token in $tokens) {
        if ($token.Kind -eq [Management.Automation.Language.TokenKind]::Comment -or
            $token -is [Management.Automation.Language.StringToken]) { continue }
        if ($token.Text -imatch $forbiddenSurface) { $surfaceHits.Add($file + ':' + $token.Extent.StartLineNumber) }
    }
}
Assert-True -Name 'VERIFY-STATIC-02 key code has no managed private key, export or persistence surface' `
    -Condition ($surfaceHits.Count -eq 0) -Detail ($surfaceHits -join ',')

# =====================================================================================================
# 9. Safety, lifetime and closed codes across every run.
# =====================================================================================================
$refusalKinds = @('ForbiddenCommand', 'PathOutsideSuite', 'UnrepresentedStore', 'UnrepresentedFolder',
    'UnrepresentedRole', 'UnrepresentedModule', 'UnrepresentedCertificate', 'UnplannedCertificateCreation',
    'StoreRemove', 'KeyUse', 'UseAfterDispose', 'RefusedNativeCompile', 'KeyCreation')
$refusals = @([VerifyWorld]::Events | Where-Object { $_.Run -cne 'ready' -and $_.Kind -cin $refusalKinds } |
        ForEach-Object { $_.Run + ':' + $_.Kind + ':' + $_.Detail })
Assert-True -Name 'VERIFY-SAFE-01 no real command, outside path, store removal, key use, creation or native compile' `
    -Condition ($refusals.Count -eq 0) -Detail ($refusals -join ',')
$compiled = @([AppDomain]::CurrentDomain.GetAssemblies() |
        ForEach-Object { $_.GetType('Dawnholder.WindowsSql.CertificateKeyNative', $false) } |
        Where-Object { $null -ne $_ })
Assert-True -Name 'VERIFY-SAFE-02 no product native class was compiled or loaded' -Condition ($compiled.Count -eq 0)
$openKeys = @([VerifyWorld]::Keys | Where-Object { -not $_.Disposed -or -not $_.PersistKeyInCsp })
Assert-True -Name 'VERIFY-SAFE-03 every reopened CSP key is disposed and PersistKeyInCsp is never changed' `
    -Condition ([VerifyWorld]::Keys.Count -gt 0 -and $openKeys.Count -eq 0) `
    -Detail ('keys=' + [VerifyWorld]::Keys.Count + ' open=' + $openKeys.Count)
$liveCertificates = @([VerifyWorld]::Handed | Where-Object { $_.Certificate.Handle -ne [IntPtr]::Zero } |
        ForEach-Object Run)
Assert-True -Name 'VERIFY-SAFE-04 every certificate object handed to the product is disposed by its owner' `
    -Condition ($liveCertificates.Count -eq 0) -Detail ($liveCertificates -join ',')
$openCodes = @($script:AllCodes | Where-Object {
        $_ -cne 'PASSED' -and $_ -cnotlike 'NOT_RUN:*' -and ($_ -cnotmatch '^WINDOWS_SQL_[A-Z0-9_]+$' -or
            $_.Contains([VerifyWorld]::Sentinel()))
    })
Assert-True -Name 'VERIFY-SAFE-05 every failure is a closed public code; raw provider text never crosses' `
    -Condition ($openCodes.Count -eq 0) -Detail ($openCodes -join ',')
$surfaces = @([VerifyWorld]::Events | Where-Object Kind -CIn @('NativeRead', 'NativeBind', 'CspOpen', 'KeyRead') |
        Group-Object Kind | ForEach-Object { $_.Name + '=' + $_.Count })
Add-TestResult -Name ('VERIFY-OBS-01 key surfaces used: ' + ($surfaces -join ' ')) -Outcome OBSERVED

Complete-TestSuite
