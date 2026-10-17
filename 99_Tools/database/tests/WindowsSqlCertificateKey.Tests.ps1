[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$WorkRoot,
    # Optional absolute windows-sql folder of an isolated same-shaped database tree (before/after product runs).
    [string]$ProductRoot = ''
)
# Pre-implementation tests for the private-key boundary of the Windows SQL certificate tools.
# Requirement (certificate key fix, user decision 2A): creation, Assert-WindowsSqlCertificate (used by Verify, Instance
# and Remove) and removal share one key support decision. An RSACng wrapper over the SChannel CSP machine key is
# accepted only when every SQL condition is proven, the RSACryptoServiceProvider path stays, access failures and
# unsupported keys are different public errors, and receipts, partial-failure evidence and protections are preserved.
# - Real product code: New-WindowsSqlCertificate.ps1 and Remove-WindowsSqlCertificate.ps1 run in this process through
#   the call operator and Assert-WindowsSqlCertificate runs as a function. Their gates, private store, path checks,
#   leases, receipts and error mapping run unchanged; this file never redefines a product function.
# - Stand-ins are registered before any product file is parsed. Type accelerators (the ServiceController mechanism of
#   WindowsSqlAdapterBoundary.Tests.ps1) replace the private key types, CommonApplicationData (moved below this
#   suite), the machine name, the administrator role, the service SID lookup and the SQL registry. Functions replace
#   the certificate store cmdlets and the PKI import. Key files and their ACLs are real files below this suite.
# - Stand-in keys carry the metadata the laptop investigation observed for the RSACng wrapper (SChannel CSP, machine
#   key, export policy None, UniqueName) and the CSP container info of the existing path. This is not CAPI/CNG
#   interop: the KeySpec of the RSACng wrapper was not observed, so it is represented on two surfaces (CNG key usage
#   and a CSP container reopened with UseExistingKey), not verified.
# - The product's native class Dawnholder.WindowsSql.CertificateKeyNative binds to a stand-in through the same
#   accelerators, and Add-Type refuses any definition naming that class, so neither its C# compile nor its P/Invoke
#   runs; other product types (the directory lease) compile as before. That stand-in is a high boundary: buffer
#   parsing, handle release and CAPI behaviour inside the class are not verified here.
# - Expected values are requirement constants and fixture facts; no product decision is recomputed. New error codes
#   are compared by meaning (access failure vs unsupported key); existing codes keep their exact text.
# - Code reachable from product code keeps its state in the CertKeyStandInWorld class: $script: inside a product
#   script names that script's scope, not this file's.
# Nothing here opens a certificate store, a key container, the registry, LSA, a service, SQL, the firewall or WSL.
. (Join-Path $PSScriptRoot 'TestSupport.ps1')
$null = Initialize-TestSuite -WorkRoot $WorkRoot -Suite 'windows-sql-certificate-key'

# =====================================================================================================
# 0. Stand-in types. They hold the fake world and record every call; members outside the represented surface throw.
# =====================================================================================================
class CertKeyStandInWorld {
    static [hashtable]$State = @{}

    static [void] Reset([string]$suiteRoot, [string]$keyArea) {
        [CertKeyStandInWorld]::State = @{
            SuiteRoot = $suiteRoot
            KeyArea = $keyArea
            Run = 'setup'
            NextId = 0
            Events = [Collections.Generic.List[object]]::new()
            Creations = [Collections.Generic.List[object]]::new()
            Store = @{}
            Containers = @{}
            PendingCreation = $null
            AclReadDenied = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            AfterKeyAclWrite = $null
        }
    }

    static [int] NextId() {
        [CertKeyStandInWorld]::State.NextId++
        return [CertKeyStandInWorld]::State.NextId
    }

    static [void] Add([string]$kind, [int]$id, [string]$detail) {
        [CertKeyStandInWorld]::State.Events.Add([pscustomobject]@{
                Run = [CertKeyStandInWorld]::State.Run
                Kind = $kind
                Id = $id
                Detail = $detail
            })
    }

    # Every stand-in member calls this first, so a disposed object is only reached through a recorded failure.
    static [void] Use([hashtable]$owner, [string]$member) {
        if ($owner.Disposed) {
            [CertKeyStandInWorld]::Add('UseAfterDispose', $owner.Id, $owner.TypeName + '.' + $member)
            throw [ObjectDisposedException]::new([string]$owner.TypeName)
        }
    }

    static [void] Fault([hashtable]$owner, [string]$member) {
        if ([string]$owner.Key.MetadataFault -ceq $member) {
            [CertKeyStandInWorld]::Add('MetadataFault', $owner.Id, $owner.TypeName + '.' + $member)
            throw [CertKeyStandInWorld]::CryptoError('Internal')
        }
    }

    static [void] Refuse([string]$kind, [int]$id, [string]$detail) {
        [CertKeyStandInWorld]::Add($kind, $id, $detail)
        throw ('CERTKEY_REFUSED_' + $kind + ': ' + $detail)
    }

    # CAPI/CNG failures carry an HResult whose setter is protected; the stand-in sets it as the runtime does.
    static [Exception] CryptoError([string]$kind) {
        $known = @{
            Keyset = @(-2146893802, 'Keyset does not exist')
            NoKey = @(-2146893811, 'Key does not exist.')
            Denied = @(-2147024891, 'Access is denied.')
            NotFound = @(-2146893807, 'Element not found.')
            ProviderType = @(-2146893801, 'Provider type not defined.')
            Internal = @(-2146893779, 'An internal error occurred.')
        }
        $exception = [Security.Cryptography.CryptographicException]::new([string]$known[$kind][1])
        [Exception].GetProperty('HResult').SetValue($exception, [int]$known[$kind][0])
        return $exception
    }

    static [bool] InSuite([string]$path) {
        if ([string]::IsNullOrEmpty($path) -or $path -match '^[A-Za-z][A-Za-z0-9]+:') { return $false }
        $full = [IO.Path]::GetFullPath($path)
        $root = ([string][CertKeyStandInWorld]::State.SuiteRoot).TrimEnd('\') + '\'
        return $full.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)
    }
}

class CertKeyStandInEnvironment {
    static [string]$MachineName = ''
    static [string]$CommonApplicationData = ''
    static [string]$NewLine = "`r`n"

    static [string] GetFolderPath([object]$folder) {
        [CertKeyStandInWorld]::Add('GetFolderPath', 0, [string]$folder)
        if ([string]$folder -cne 'CommonApplicationData') {
            [CertKeyStandInWorld]::Refuse('UnrepresentedFolder', 0, [string]$folder)
        }
        return [CertKeyStandInEnvironment]::CommonApplicationData
    }
}

class CertKeyStandInPrincipal {
    static [bool]$Administrator = $true
    hidden [object]$Identity

    CertKeyStandInPrincipal([object]$identity) {
        $this.Identity = $identity
    }

    [bool] IsInRole([object]$role) {
        [CertKeyStandInWorld]::Add('IsInRole', 0, [string]$role)
        if ([string]$role -cne 'Administrator') { [CertKeyStandInWorld]::Refuse('UnrepresentedRole', 0, [string]$role) }
        return [CertKeyStandInPrincipal]::Administrator
    }
}

class CertKeyStandInAccount {
    static [hashtable]$Sids = @{}
    [string]$Value

    CertKeyStandInAccount([string]$name) {
        $this.Value = $name
    }

    [object] Translate([type]$target) {
        [CertKeyStandInWorld]::Add('Translate', 0, $this.Value)
        if ($target.FullName -cne 'System.Security.Principal.SecurityIdentifier' -or
            -not [CertKeyStandInAccount]::Sids.ContainsKey($this.Value)) {
            throw [Security.Principal.IdentityNotMappedException]::new(
                'Some or all identity references could not be translated.')
        }
        return [Security.Principal.SecurityIdentifier]::new([string][CertKeyStandInAccount]::Sids[$this.Value])
    }

    [string] ToString() {
        return $this.Value
    }
}

class CertKeyStandInRegistry {
    static [string]$InstanceKey = ''
    static [hashtable]$Values = @{}
    hidden [string]$Suffix

    CertKeyStandInRegistry([string]$suffix) {
        $this.Suffix = $suffix
    }

    static [object] OpenBaseKey([object]$hive, [object]$view) {
        [CertKeyStandInWorld]::Add('RegistryOpenBase', 0, [string]$hive + '/' + [string]$view)
        if ([string]$hive -cne 'LocalMachine' -or [string]$view -cne 'Registry64') {
            [CertKeyStandInWorld]::Refuse('UnrepresentedHive', 0, [string]$hive)
        }
        return [CertKeyStandInRegistry]::new('')
    }

    [object] OpenSubKey([string]$name, [bool]$writable) {
        [CertKeyStandInWorld]::Add('RegistryOpen', 0, $name + '|writable=' + $writable)
        if ($writable) { [CertKeyStandInWorld]::Refuse('RegistryWrite', 0, $name) }
        $prefix = [CertKeyStandInRegistry]::InstanceKey
        if (-not $name.StartsWith($prefix, [StringComparison]::Ordinal)) { return $null }
        $rest = $name.Substring($prefix.Length)
        if (-not [CertKeyStandInRegistry]::Values.ContainsKey($rest)) { return $null }
        return [CertKeyStandInRegistry]::new($rest)
    }

    [object] GetValue([string]$name, [object]$default, [object]$options) {
        $entry = [CertKeyStandInRegistry]::Values[$this.Suffix][$name]
        if ($null -eq $entry) { return $default }
        return $entry.Value
    }

    [object] GetValueKind([string]$name) {
        return [Microsoft.Win32.RegistryValueKind][CertKeyStandInRegistry]::Values[$this.Suffix][$name].Kind
    }

    [void] Dispose() {
    }
}

class CertKeyStandInMarker {
}

class CertKeyStandInAsymmetric {
    hidden [hashtable]$State
}

class CertKeyStandInRsa : CertKeyStandInAsymmetric {
}

class CertKeyStandInRsaCng : CertKeyStandInRsa {
}

class CertKeyStandInRsaCsp : CertKeyStandInRsa {
    # Fixture keys are built with the marker. Every other constructor is the CAPI surface product code may reach.
    static [scriptblock]$OpenHook = $null

    CertKeyStandInRsaCsp([CertKeyStandInMarker]$marker) {
    }

    CertKeyStandInRsaCsp() {
        [CertKeyStandInWorld]::Refuse('ContainerCreation', 0, 'RSACryptoServiceProvider()')
    }

    CertKeyStandInRsaCsp([int]$size) {
        [CertKeyStandInWorld]::Refuse('ContainerCreation', 0, 'RSACryptoServiceProvider(int)')
    }

    CertKeyStandInRsaCsp([object]$parameters) {
        & ([CertKeyStandInRsaCsp]::OpenHook) $this 0 $parameters
    }

    CertKeyStandInRsaCsp([int]$size, [object]$parameters) {
        & ([CertKeyStandInRsaCsp]::OpenHook) $this $size $parameters
    }
}

class CertKeyStandInCngKey {
    static [scriptblock]$OpenHook = $null
    hidden [hashtable]$State

    static [object] Open([string]$name, [object]$provider, [object]$options) {
        return & ([CertKeyStandInCngKey]::OpenHook) $name $provider $options
    }

    static [object] Open([string]$name, [object]$provider) {
        return & ([CertKeyStandInCngKey]::OpenHook) $name $provider $null
    }

    static [object] Open([string]$name) {
        return & ([CertKeyStandInCngKey]::OpenHook) $name $null $null
    }

    static [bool] Exists([string]$name, [object]$provider, [object]$options) {
        [CertKeyStandInWorld]::Add('CngExists', 0, $name)
        return [CertKeyStandInWorld]::State.Containers.ContainsKey($name)
    }

    static [object] Create([object]$algorithm, [string]$name, [object]$parameters) {
        [CertKeyStandInWorld]::Refuse('KeyCreation', 0, 'CngKey.Create ' + $name)
        return $null
    }

    static [object] Import([object]$blob, [object]$format) {
        [CertKeyStandInWorld]::Refuse('KeyCreation', 0, 'CngKey.Import')
        return $null
    }
}

class CertKeyStandInCertificateExtensions {
    static [object] GetRSAPrivateKey([object]$certificate) {
        return $certificate.GetRSAPrivateKey()
    }

    static [object] GetRSAPublicKey([object]$certificate) {
        [CertKeyStandInWorld]::Refuse('UnrepresentedMember', 0, 'GetRSAPublicKey')
        return $null
    }

    static [object] CopyWithPrivateKey([object]$certificate, [object]$key) {
        [CertKeyStandInWorld]::Refuse('KeyOperation', 0, 'CopyWithPrivateKey')
        return $null
    }
}

class CertKeyStandInKeyNative {
    # Stands in for the product's Add-Type class Dawnholder.WindowsSql.CertificateKeyNative. Binding the type name
    # here keeps both the C# compile and its P/Invoke calls from running; the hooks answer from fixture facts with
    # the class's public contract (CspParameters from CERT_KEY_PROV_INFO, closed-code exceptions).
    static [scriptblock]$ReadHook = $null
    static [scriptblock]$BindHook = $null

    static [object] ReadProviderParameters([object]$certificate) {
        return & ([CertKeyStandInKeyNative]::ReadHook) $certificate
    }

    static [void] AssertKeyBinding([object]$certificate) {
        & ([CertKeyStandInKeyNative]::BindHook) $certificate
    }
}

# =====================================================================================================
# 1. Fail-closed guards and store/ACL/file wrappers, defined before anything else runs.
#    Product scripts find these functions through the caller scope chain; they use only CertKeyStandInWorld state.
# =====================================================================================================
foreach ($name in @('Restart-Service', 'Stop-Service', 'Start-Service', 'Set-Service', 'Get-Service',
        'Import-PfxCertificate', 'Export-PfxCertificate', 'Export-Certificate', 'Import-Certificate',
        'Get-NetFirewallRule', 'New-NetFirewallRule', 'Set-NetFirewallRule', 'Remove-NetFirewallRule',
        'Get-NetTCPConnection', 'Invoke-Sqlcmd', 'sqlcmd', 'netsh', 'reg', 'certutil', 'sc.exe', 'icacls', 'docker',
        'wsl', 'wsl.exe', 'Start-Process', 'Get-ItemProperty', 'Set-ItemProperty', 'New-ItemProperty',
        'Remove-ItemProperty', 'Get-WmiObject', 'Get-CimInstance', 'Invoke-CimMethod')) {
    $guard = { [CertKeyStandInWorld]::Refuse('ForbiddenCommand', 0, $name) }.GetNewClosure()
    Set-Item -Path ('Function:script:' + $name) -Value $guard
}

# The directory lease compiles its own class as the original suite did; only the certificate key native class is
# refused, so its C# compile and P/Invoke never run even if the accelerator binding were lost.
function Add-Type {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$TypeDefinition)
    if ($TypeDefinition.Contains('CertificateKeyNative')) {
        [CertKeyStandInWorld]::Refuse('RefusedNativeCompile', 0, 'Add-Type')
    }
    Microsoft.PowerShell.Utility\Add-Type -TypeDefinition $TypeDefinition -ErrorAction Stop
}

function Resolve-StandInTarget {
    param([string]$LiteralPath, [string]$Path)
    if ($LiteralPath) { return $LiteralPath }
    return $Path
}

function Get-Item {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '', [switch]$Force)
    $target = Resolve-StandInTarget -LiteralPath $LiteralPath -Path $Path
    if ($target -match '^(?i)cert:') {
        $prefix = 'Cert:\LocalMachine\My\'
        [CertKeyStandInWorld]::Add('StoreRead', 0, $target)
        if (-not $target.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            [CertKeyStandInWorld]::Refuse('UnrepresentedStore', 0, $target)
        }
        $spec = [CertKeyStandInWorld]::State.Store[$target.Substring($prefix.Length)]
        if ($null -eq $spec) {
            throw [Management.Automation.ItemNotFoundException]::new(
                "Cannot find path '$target' because it does not exist.")
        }
        return New-StandInCertificate -Spec $spec
    }
    if (-not [CertKeyStandInWorld]::InSuite($target)) {
        [CertKeyStandInWorld]::Refuse('PathOutsideSuite', 0, 'Get-Item ' + $target)
    }
    return Microsoft.PowerShell.Management\Get-Item -LiteralPath $target -Force:$Force
}

function Remove-Item {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '', [switch]$DeleteKey, [switch]$Recurse, [switch]$Force)
    $target = Resolve-StandInTarget -LiteralPath $LiteralPath -Path $Path
    if ($target -match '^(?i)cert:') {
        [CertKeyStandInWorld]::Add('StoreRemove', 0, $target + '|DeleteKey=' + $DeleteKey.IsPresent)
        $thumbprint = $target.Substring($target.LastIndexOf('\') + 1)
        if (-not [CertKeyStandInWorld]::State.Store.ContainsKey($thumbprint)) {
            throw [Management.Automation.ItemNotFoundException]::new(
                "Cannot find path '$target' because it does not exist.")
        }
        [CertKeyStandInWorld]::State.Store.Remove($thumbprint)
        return
    }
    if (-not [CertKeyStandInWorld]::InSuite($target)) {
        [CertKeyStandInWorld]::Refuse('PathOutsideSuite', 0, 'Remove-Item ' + $target)
    }
    [CertKeyStandInWorld]::Add('FileRemove', 0, [IO.Path]::GetFullPath($target))
    Microsoft.PowerShell.Management\Remove-Item -LiteralPath $target -Recurse:$Recurse -Force:$Force
}

function Test-Path {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '', [string]$PathType = 'Any')
    $target = Resolve-StandInTarget -LiteralPath $LiteralPath -Path $Path
    if (-not [CertKeyStandInWorld]::InSuite($target)) {
        [CertKeyStandInWorld]::Refuse('PathOutsideSuite', 0, 'Test-Path ' + $target)
    }
    return Microsoft.PowerShell.Management\Test-Path -LiteralPath $target -PathType $PathType
}

function Get-ChildItem {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '', [string]$Filter = '', [switch]$File, [switch]$Directory,
        [switch]$Force, [switch]$Recurse)
    $target = Resolve-StandInTarget -LiteralPath $LiteralPath -Path $Path
    if (-not [CertKeyStandInWorld]::InSuite($target)) {
        [CertKeyStandInWorld]::Refuse('PathOutsideSuite', 0, 'Get-ChildItem ' + $target)
    }
    $arguments = @{ LiteralPath = $target; File = $File; Directory = $Directory; Force = $Force; Recurse = $Recurse }
    if ($Filter) { $arguments.Filter = $Filter }
    return Microsoft.PowerShell.Management\Get-ChildItem @arguments
}

function Get-Acl {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '')
    $target = Resolve-StandInTarget -LiteralPath $LiteralPath -Path $Path
    if (-not [CertKeyStandInWorld]::InSuite($target)) {
        [CertKeyStandInWorld]::Refuse('PathOutsideSuite', 0, 'Get-Acl ' + $target)
    }
    $full = [IO.Path]::GetFullPath($target)
    [CertKeyStandInWorld]::Add('AclRead', 0, $full)
    if ([CertKeyStandInWorld]::State.AclReadDenied.Contains($full)) {
        throw [UnauthorizedAccessException]::new('Attempted to perform an unauthorized operation.')
    }
    return Microsoft.PowerShell.Security\Get-Acl -LiteralPath $target
}

function Set-Acl {
    [CmdletBinding()]
    param([string]$LiteralPath = '', [string]$Path = '', $AclObject)
    $target = Resolve-StandInTarget -LiteralPath $LiteralPath -Path $Path
    if (-not [CertKeyStandInWorld]::InSuite($target)) {
        [CertKeyStandInWorld]::Refuse('PathOutsideSuite', 0, 'Set-Acl ' + $target)
    }
    $full = [IO.Path]::GetFullPath($target).TrimEnd('\')
    $keyArea = [string][CertKeyStandInWorld]::State.KeyArea
    $inKeyArea = $full.StartsWith($keyArea + '\', [StringComparison]::OrdinalIgnoreCase) -or $full -ieq $keyArea
    [CertKeyStandInWorld]::Add($(if ($inKeyArea) { 'KeyAclWrite' } else { 'AclWrite' }), 0, $full)
    Microsoft.PowerShell.Security\Set-Acl -LiteralPath $target -AclObject $AclObject -ErrorAction Stop
    $hook = [CertKeyStandInWorld]::State.AfterKeyAclWrite
    if ($inKeyArea -and $null -ne $hook) {
        [CertKeyStandInWorld]::State.AfterKeyAclWrite = $null
        & $hook $full
    }
}

function Import-Module {
    [CmdletBinding()]
    param([Parameter(Position = 0)][string[]]$Name)
    [CertKeyStandInWorld]::Add('ImportModule', 0, ($Name -join ','))
    if (@($Name).Count -ne 1 -or $Name[0] -cne 'PKI') {
        [CertKeyStandInWorld]::Refuse('UnrepresentedModule', 0, ($Name -join ','))
    }
}

function New-SelfSignedCertificate {
    [CmdletBinding()]
    param($Type, $Subject, $DnsName, $CertStoreLocation, $Provider, $KeyAlgorithm, $KeyLength, $KeySpec,
        $KeyExportPolicy, $HashAlgorithm, $KeyUsage, $NotAfter)
    [CertKeyStandInWorld]::State.Creations.Add([pscustomobject]@{
            Run = [CertKeyStandInWorld]::State.Run
            Type = [string]$Type
            Subject = [string]$Subject
            DnsName = @($DnsName) -join ','
            CertStoreLocation = [string]$CertStoreLocation
            Provider = [string]$Provider
            KeyAlgorithm = [string]$KeyAlgorithm
            KeyLength = [string]$KeyLength
            KeySpec = [string]$KeySpec
            KeyExportPolicy = [string]$KeyExportPolicy
            HashAlgorithm = [string]$HashAlgorithm
            KeyUsage = @($KeyUsage) -join ','
        })
    [CertKeyStandInWorld]::Add('CertificateCreate', 0, [string]$Subject)
    $spec = [CertKeyStandInWorld]::State.PendingCreation
    if ($null -eq $spec) { [CertKeyStandInWorld]::Refuse('UnplannedCertificateCreation', 0, [string]$Subject) }
    [CertKeyStandInWorld]::State.PendingCreation = $null
    [CertKeyStandInWorld]::State.Store[$spec.Public.Thumbprint] = $spec
    return New-StandInCertificate -Spec $spec
}

# =====================================================================================================
# 2. Stand-in key and certificate objects: the members product code may read, with the fixture's metadata.
# =====================================================================================================
function Add-StandInKeyCommonMembers {
    param($Object)
    Add-Member -InputObject $Object -MemberType ScriptProperty -Name KeySize -Value {
        [CertKeyStandInWorld]::Use($this.State, 'KeySize')
        return [int]$this.State.Key.KeySize
    }
    # Explicit getter methods: the surface a caller uses when it must not let property access turn a fault into $null.
    Add-Member -InputObject $Object -MemberType ScriptMethod -Name get_KeySize -Value {
        [CertKeyStandInWorld]::Use($this.State, 'get_KeySize')
        return [int]$this.State.Key.KeySize
    }
    Add-Member -InputObject $Object -MemberType ScriptMethod -Name Dispose -Value {
        if (-not $this.State.Disposed) {
            $this.State.Disposed = $true
            [CertKeyStandInWorld]::Add('Dispose', $this.State.Id, $this.State.TypeName)
            # A CSP object told not to persist deletes its container on dispose.
            if ($this.State.ContainsKey('Persist') -and -not $this.PersistKeyInCsp) {
                [CertKeyStandInWorld]::Add('WouldDeleteContainer', $this.State.Id, [string]$this.State.Key.KeyName)
            }
            if ($null -ne $this.State.CngKey) { $this.State.CngKey.State.Disposed = $true }
        }
    }
    # Deciding support never needs the private key itself; exporting or using it is recorded and refused.
    foreach ($operation in @('ExportParameters', 'ExportRSAPrivateKey', 'ExportPkcs8PrivateKey',
            'ExportEncryptedPkcs8PrivateKey', 'TryExportRSAPrivateKey', 'ExportCspBlob', 'ToXmlString', 'Decrypt',
            'Encrypt', 'SignHash', 'SignData', 'DecryptValue', 'EncryptValue')) {
        $refusal = { [CertKeyStandInWorld]::Refuse('KeyOperation', $this.State.Id, $operation) }.GetNewClosure()
        Add-Member -InputObject $Object -MemberType ScriptMethod -Name $operation -Value $refusal
    }
}

function New-StandInKey {
    param([hashtable]$Key, [string]$Api, [bool]$Owned)
    $state = @{ Key = $Key; Id = [CertKeyStandInWorld]::NextId(); Api = $Api; Disposed = $false; CngKey = $null }
    $object = $null
    switch -CaseSensitive ($Key.Wrapper) {
        'Cng' {
            $object = [CertKeyStandInRsaCng]::new()
            $state.TypeName = 'RSACng'
        }
        'Csp' {
            $object = [CertKeyStandInRsaCsp]::new([CertKeyStandInMarker]::new())
            $state.TypeName = 'RSACryptoServiceProvider'
            $state.Persist = $true
        }
        default { throw ('Unknown fixture wrapper: ' + $Key.Wrapper) }
    }
    $object.State = $state
    [CertKeyStandInWorld]::Add('Acquire', $state.Id, $Api + ':' + $state.TypeName + ':owned=' + $Owned)
    Add-StandInKeyCommonMembers -Object $object
    if ($Key.Wrapper -ceq 'Cng') { Add-StandInCngWrapperMembers -Object $object }
    if ($Key.Wrapper -ceq 'Csp') { Add-StandInCspMembers -Object $object }
    return $object
}

function Add-StandInCngWrapperMembers {
    param($Object)
    Add-Member -InputObject $Object -MemberType ScriptProperty -Name Key -Value {
        [CertKeyStandInWorld]::Use($this.State, 'Key')
        # A key store failure surfaces when the CNG handle is reached, after the wrapper object exists.
        if ([string]$this.State.Key.MetadataFault -ceq 'Key') { throw [CertKeyStandInWorld]::CryptoError('Internal') }
        if ($null -eq $this.State.CngKey) {
            $this.State.CngKey = New-StandInCngKey -Key $this.State.Key -Origin ('RSACng.Key of ' + $this.State.Id) `
                -Owned $false
        }
        return $this.State.CngKey
    }
}

function New-StandInCngKey {
    param([hashtable]$Key, [string]$Origin, [bool]$Owned)
    $cngKey = [CertKeyStandInCngKey]::new()
    $cngKey.State = @{ Key = $Key; Id = [CertKeyStandInWorld]::NextId(); Disposed = $false; TypeName = 'CngKey' }
    [CertKeyStandInWorld]::Add('Acquire', $cngKey.State.Id, $Origin + ':CngKey:owned=' + $Owned)
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name Provider -Value {
        [CertKeyStandInWorld]::Use($this.State, 'Provider')
        [CertKeyStandInWorld]::Fault($this.State, 'Provider')
        if ($null -eq $this.State.Key.Provider) { return $null }
        return [Security.Cryptography.CngProvider]::new([string]$this.State.Key.Provider)
    }
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name IsMachineKey -Value {
        [CertKeyStandInWorld]::Use($this.State, 'IsMachineKey')
        [CertKeyStandInWorld]::Fault($this.State, 'IsMachineKey')
        return [bool]$this.State.Key.MachineKey
    }
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name ExportPolicy -Value {
        [CertKeyStandInWorld]::Use($this.State, 'ExportPolicy')
        [CertKeyStandInWorld]::Fault($this.State, 'ExportPolicy')
        if ($this.State.Key.Exportable) { return [Security.Cryptography.CngExportPolicies]::AllowExport }
        return [Security.Cryptography.CngExportPolicies]::None
    }
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name UniqueName -Value {
        [CertKeyStandInWorld]::Use($this.State, 'UniqueName')
        [CertKeyStandInWorld]::Fault($this.State, 'UniqueName')
        return $this.State.Key.UniqueName
    }
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name KeyName -Value {
        [CertKeyStandInWorld]::Use($this.State, 'KeyName')
        return $this.State.Key.KeyName
    }
    # KeySpec of the wrapped CAPI key was not observed; exchange keys are represented as usable for every usage.
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name KeyUsage -Value {
        [CertKeyStandInWorld]::Use($this.State, 'KeyUsage')
        [CertKeyStandInWorld]::Fault($this.State, 'KeyUsage')
        if ($this.State.Key.KeySpec -ceq 'Exchange') { return [Security.Cryptography.CngKeyUsages]::AllUsages }
        return [Security.Cryptography.CngKeyUsages]::Signing
    }
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name KeySize -Value {
        [CertKeyStandInWorld]::Use($this.State, 'KeySize')
        return [int]$this.State.Key.KeySize
    }
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name Algorithm -Value {
        [CertKeyStandInWorld]::Use($this.State, 'Algorithm')
        return [Security.Cryptography.CngAlgorithm]::Rsa
    }
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name AlgorithmGroup -Value {
        [CertKeyStandInWorld]::Use($this.State, 'AlgorithmGroup')
        return [Security.Cryptography.CngAlgorithmGroup]::Rsa
    }
    Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name IsEphemeral -Value {
        [CertKeyStandInWorld]::Use($this.State, 'IsEphemeral')
        return $false
    }
    foreach ($member in @('Handle', 'ProviderHandle', 'UIPolicy', 'ParentWindowHandle')) {
        $refusal = { [CertKeyStandInWorld]::Refuse('UnrepresentedMember', $this.State.Id, 'CngKey.' + $member) }
        Add-Member -InputObject $cngKey -MemberType ScriptProperty -Name $member -Value $refusal.GetNewClosure()
    }
    Add-Member -InputObject $cngKey -MemberType ScriptMethod -Name GetProperty -Value {
        param([string]$Name, $Options)
        [CertKeyStandInWorld]::Use($this.State, 'GetProperty')
        $value = Get-StandInCngPropertyBytes -Key $this.State.Key -Name $Name
        if ($null -eq $value) {
            [CertKeyStandInWorld]::Add('UnrepresentedProperty', $this.State.Id, $Name)
            throw [CertKeyStandInWorld]::CryptoError('NotFound')
        }
        [CertKeyStandInWorld]::Add('PropertyRead', $this.State.Id, $Name)
        $propertyOptions = [Security.Cryptography.CngPropertyOptions]::None
        if ($null -ne $Options) { $propertyOptions = [Security.Cryptography.CngPropertyOptions]$Options }
        return [Security.Cryptography.CngProperty]::new($Name, [byte[]]$value, $propertyOptions)
    }
    Add-Member -InputObject $cngKey -MemberType ScriptMethod -Name HasProperty -Value {
        param([string]$Name, $Options)
        [CertKeyStandInWorld]::Use($this.State, 'HasProperty')
        return $null -ne (Get-StandInCngPropertyBytes -Key $this.State.Key -Name $Name)
    }
    foreach ($operation in @('Export', 'Delete', 'SetProperty')) {
        $refusal = { [CertKeyStandInWorld]::Refuse('KeyOperation', $this.State.Id, 'CngKey.' + $operation) }
        Add-Member -InputObject $cngKey -MemberType ScriptMethod -Name $operation -Value $refusal.GetNewClosure()
    }
    Add-Member -InputObject $cngKey -MemberType ScriptMethod -Name Dispose -Value {
        if (-not $this.State.Disposed) {
            $this.State.Disposed = $true
            [CertKeyStandInWorld]::Add('Dispose', $this.State.Id, 'CngKey')
        }
    }
    return $cngKey
}

function Get-StandInCngPropertyBytes {
    param([hashtable]$Key, [string]$Name)
    # Property names and encodings of the CNG key storage API; a faulted member fails like an unreadable property.
    $faults = @{ 'Export Policy' = 'ExportPolicy'; 'Key Usage' = 'KeyUsage'; 'Unique Name' = 'UniqueName' }
    if ($faults.ContainsKey($Name) -and [string]$Key.MetadataFault -ceq $faults[$Name]) {
        throw [CertKeyStandInWorld]::CryptoError('Internal')
    }
    switch -CaseSensitive ($Name) {
        'Export Policy' { return [BitConverter]::GetBytes([int]$(if ($Key.Exportable) { 1 } else { 0 })) }
        'Key Usage' {
            return [BitConverter]::GetBytes([int]$(if ($Key.KeySpec -ceq 'Exchange') { 0x00ffffff } else { 2 }))
        }
        'Length' { return [BitConverter]::GetBytes([int]$Key.KeySize) }
        'Unique Name' { return [Text.Encoding]::Unicode.GetBytes([string]$Key.UniqueName + [char]0) }
        'Name' { return [Text.Encoding]::Unicode.GetBytes([string]$Key.KeyName + [char]0) }
    }
    return $null
}

function Add-StandInCspMembers {
    param($Object)
    Add-Member -InputObject $Object -MemberType NoteProperty -Name PersistKeyInCsp -Value $true
    Add-Member -InputObject $Object -MemberType ScriptProperty -Name PublicOnly -Value {
        [CertKeyStandInWorld]::Use($this.State, 'PublicOnly')
        return $false
    }
    Add-Member -InputObject $Object -MemberType ScriptProperty -Name CspKeyContainerInfo -Value {
        [CertKeyStandInWorld]::Use($this.State, 'CspKeyContainerInfo')
        [CertKeyStandInWorld]::Fault($this.State, 'ContainerInfo')
        return New-StandInContainerInfo -Owner $this.State
    }
    Add-Member -InputObject $Object -MemberType ScriptMethod -Name get_PublicOnly -Value {
        [CertKeyStandInWorld]::Use($this.State, 'get_PublicOnly')
        return $false
    }
    Add-Member -InputObject $Object -MemberType ScriptMethod -Name get_CspKeyContainerInfo -Value {
        [CertKeyStandInWorld]::Use($this.State, 'get_CspKeyContainerInfo')
        [CertKeyStandInWorld]::Fault($this.State, 'ContainerInfo')
        return New-StandInContainerInfo -Owner $this.State
    }
}

function New-StandInContainerInfo {
    param([hashtable]$Owner)
    $info = [pscustomobject]@{ CertKeyStandInOwner = $Owner }
    # Each body receives the owner state: $this is not visible inside a script block called from the getter.
    $members = [ordered]@{
        KeyNumber = {
            param($Owner)
            if ($Owner.Key.KeySpec -ceq 'Exchange') { return [Security.Cryptography.KeyNumber]::Exchange }
            return [Security.Cryptography.KeyNumber]::Signature
        }
        MachineKeyStore = { param($Owner) return [bool]$Owner.Key.MachineKey }
        # Exportability is the same key attribute whichever surface reports it; its fault reaches the CSP query too.
        Exportable = {
            param($Owner)
            [CertKeyStandInWorld]::Fault($Owner, 'ExportPolicy')
            return [bool]$Owner.Key.Exportable
        }
        UniqueKeyContainerName = {
            param($Owner)
            [CertKeyStandInWorld]::Fault($Owner, 'UniqueName')
            return $Owner.Key.UniqueName
        }
        KeyContainerName = { param($Owner) return $Owner.Key.KeyName }
        ProviderName = { param($Owner) return $Owner.Key.Provider }
        ProviderType = { param($Owner) return [int]$Owner.Key.ProviderType }
        Accessible = { return $true }
        HardwareDevice = { return $false }
        Protected = { return $false }
        RandomlyGenerated = { return $false }
        Removable = { return $false }
    }
    foreach ($name in $members.Keys) {
        $body = $members[$name]
        $getter = {
            [CertKeyStandInWorld]::Use($this.CertKeyStandInOwner, 'CspKeyContainerInfo.' + $name)
            & $body $this.CertKeyStandInOwner
        }.GetNewClosure()
        Add-Member -InputObject $info -MemberType ScriptProperty -Name $name -Value $getter
        Add-Member -InputObject $info -MemberType ScriptMethod -Name ('get_' + $name) -Value $getter
    }
    return $info
}

function Open-StandInCapiContainer {
    param($Object, [int]$Size, $Parameters)
    $flags = [Security.Cryptography.CspProviderFlags]$Parameters.Flags
    $request = '{0}|{1}|{2}|{3}|{4}' -f $Parameters.ProviderName, $Parameters.ProviderType,
    $Parameters.KeyContainerName, $flags, $Parameters.KeyNumber
    [CertKeyStandInWorld]::Add('CapiOpen', 0, $request)
    # Without UseExistingKey CAPI creates a new container under the requested name.
    if (($flags -band [Security.Cryptography.CspProviderFlags]::UseExistingKey) -eq 0) {
        [CertKeyStandInWorld]::Refuse('ContainerCreation', 0, $request)
    }
    $key = [CertKeyStandInWorld]::State.Containers[[string]$Parameters.KeyContainerName]
    $machine = ($flags -band [Security.Cryptography.CspProviderFlags]::UseMachineKeyStore) -ne 0
    # Only CAPI providers own containers; a CNG-only key (KSP) or a failed key store has none to reopen.
    $unreachable = $null -eq $key -or $key.Acquire -cne 'Ok' -or [string]$key.MetadataFault -ceq 'Key'
    if ($unreachable -or $key.ProviderType -eq 0 -or $machine -ne [bool]$key.MachineKey -or
        [string]$Parameters.ProviderName -ine [string]$key.Provider) {
        throw [CertKeyStandInWorld]::CryptoError('Keyset')
    }
    if ($Parameters.ProviderType -ne $key.ProviderType) { throw [CertKeyStandInWorld]::CryptoError('ProviderType') }
    # KeyNumber -1 is the RSACryptoServiceProvider default, which opens the exchange key pair.
    $requested = if ($Parameters.KeyNumber -eq 2) { 'Signature' } else { 'Exchange' }
    if ($requested -cne $key.KeySpec) { throw [CertKeyStandInWorld]::CryptoError('NoKey') }
    $Object.State = @{
        Key = $key
        Id = [CertKeyStandInWorld]::NextId()
        Api = 'CspParameters'
        Disposed = $false
        CngKey = $null
        TypeName = 'RSACryptoServiceProvider'
        Persist = $true
    }
    [CertKeyStandInWorld]::Add('Acquire', $Object.State.Id, 'CspParameters:RSACryptoServiceProvider:owned=True')
    Add-StandInKeyCommonMembers -Object $Object
    Add-StandInCspMembers -Object $Object
}

function Open-StandInCngKey {
    param([string]$Name, $Provider, $Options)
    $providerName = ''
    if ($Provider -is [string]) { $providerName = $Provider }
    elseif ($null -ne $Provider) { $providerName = [string]$Provider.Provider }
    $request = '{0}|{1}|{2}' -f $Name, $providerName, $Options
    [CertKeyStandInWorld]::Add('CngOpen', 0, $request)
    $key = [CertKeyStandInWorld]::State.Containers[$Name]
    $machine = [string]$Options -match 'MachineKey'
    if ($null -eq $key -or $key.Acquire -cne 'Ok' -or [string]$key.MetadataFault -ceq 'Key' -or
        $machine -ne [bool]$key.MachineKey -or $providerName -ine [string]$key.Provider) {
        throw [CertKeyStandInWorld]::CryptoError('Keyset')
    }
    return New-StandInCngKey -Key $key -Origin ('CngKey.Open ' + $Name) -Owned $true
}

function Read-StandInProviderInfo {
    param($Certificate)
    # CERT_KEY_PROV_INFO of the fixture certificate: the CAPI provider, container, KeySpec and machine flag, whatever
    # managed wrapper .NET would return. An unreadable provider leaves the property unprovable.
    $owner = $Certificate.CertKeyStandInState
    [CertKeyStandInWorld]::Use($owner, 'CertificateKeyNative.ReadProviderParameters')
    $key = $owner.Spec.Key
    [CertKeyStandInWorld]::Add('NativeProviderInfo', $owner.Id, [string]$key.KeyName)
    if ([string]$key.MetadataFault -ceq 'Provider') {
        throw [InvalidOperationException]::new('WINDOWS_SQL_CERTIFICATE_KEY_METADATA_UNAVAILABLE')
    }
    $parameters = [Security.Cryptography.CspParameters]::new()
    $parameters.ProviderName = [string]$key.Provider
    $parameters.ProviderType = [int]$key.ProviderType
    $parameters.KeyContainerName = [string]$key.KeyName
    $parameters.KeyNumber = $(if ($key.KeySpec -ceq 'Exchange') { 1 } else { 2 })
    $parameters.Flags = $(if ($key.MachineKey) { [Security.Cryptography.CspProviderFlags]::UseMachineKeyStore }
        else { [Security.Cryptography.CspProviderFlags]::NoFlags })
    return $parameters
}

function Assert-StandInKeyBinding {
    param($Certificate)
    # CryptAcquireCertificatePrivateKey through CAPI: an unreachable container fails the acquisition; a reachable
    # fixture container always holds this certificate's key pair and reports its own KeySpec.
    $owner = $Certificate.CertKeyStandInState
    [CertKeyStandInWorld]::Use($owner, 'CertificateKeyNative.AssertKeyBinding')
    $key = $owner.Spec.Key
    [CertKeyStandInWorld]::Add('NativeKeyBinding', $owner.Id, [string]$key.KeyName)
    $registered = [CertKeyStandInWorld]::State.Containers[[string]$key.KeyName]
    $unreachable = $null -eq $registered -or $key.Acquire -cne 'Ok' -or [string]$key.MetadataFault -ceq 'Key' -or
    $key.ProviderType -eq 0
    if ($unreachable) { throw [InvalidOperationException]::new('WINDOWS_SQL_CERTIFICATE_KEY_ACCESS_FAILED') }
    if ($key.KeySpec -cne 'Exchange') {
        throw [InvalidOperationException]::new('WINDOWS_SQL_CERTIFICATE_KEY_UNSUPPORTED')
    }
}

function New-StandInCertificate {
    param([hashtable]$Spec)
    $public = $Spec.Public
    $state = @{ Spec = $Spec; Id = [CertKeyStandInWorld]::NextId(); Cached = $null; Disposed = $false
        TypeName = 'X509Certificate2' }
    [CertKeyStandInWorld]::Add('CertificateIssue', $state.Id, $public.Thumbprint)
    # Certificate content is a real in-memory public certificate; only private key access is a stand-in.
    $certificate = [pscustomobject]@{
        Thumbprint = $public.Thumbprint
        Subject = $public.Subject
        Issuer = $public.Issuer
        SerialNumber = $public.SerialNumber
        NotBefore = $public.NotBefore
        NotAfter = $public.NotAfter
        Extensions = $public.Extensions
        RawData = $public.RawData
        HasPrivateKey = $true
        CertKeyStandInState = $state
    }
    # The property swallows getter failures into $null (pwsh 7), as the laptop's non-elevated read observed.
    Add-Member -InputObject $certificate -MemberType ScriptProperty -Name PrivateKey -Value {
        Get-StandInCertificateKey -Owner $this.CertKeyStandInState -Api 'PrivateKey'
    }
    Add-Member -InputObject $certificate -MemberType ScriptMethod -Name get_PrivateKey -Value {
        Get-StandInCertificateKey -Owner $this.CertKeyStandInState -Api 'get_PrivateKey'
    }
    Add-Member -InputObject $certificate -MemberType ScriptMethod -Name GetRSAPrivateKey -Value {
        Get-StandInCertificateKey -Owner $this.CertKeyStandInState -Api 'GetRSAPrivateKey'
    }
    Add-Member -InputObject $certificate -MemberType ScriptMethod -Name ExportCertificatePem -Value {
        [CertKeyStandInWorld]::Use($this.CertKeyStandInState, 'ExportCertificatePem')
        return $this.CertKeyStandInState.Spec.Public.ExportCertificatePem()
    }
    Add-Member -InputObject $certificate -MemberType ScriptMethod -Name CopyWithPrivateKey -Value {
        [CertKeyStandInWorld]::Refuse('KeyOperation', $this.CertKeyStandInState.Id, 'CopyWithPrivateKey')
    }
    Add-Member -InputObject $certificate -MemberType ScriptMethod -Name Dispose -Value {
        if (-not $this.CertKeyStandInState.Disposed) {
            $this.CertKeyStandInState.Disposed = $true
            [CertKeyStandInWorld]::Add('CertificateDispose', $this.CertKeyStandInState.Id, $this.Thumbprint)
        }
    }
    return $certificate
}

function Get-StandInCertificateKey {
    param([hashtable]$Owner, [string]$Api)
    [CertKeyStandInWorld]::Use($Owner, $Api)
    $key = $Owner.Spec.Key
    # PrivateKey and get_PrivateKey return one cached object per certificate object, as X509Certificate2 does;
    # GetRSAPrivateKey returns a new object that the caller owns and disposes.
    $cached = $Api -cne 'GetRSAPrivateKey'
    if ($cached -and $null -ne $Owner.Cached) {
        [CertKeyStandInWorld]::Add('KeyRequest', $Owner.Id, $Api + ':cached')
        return $Owner.Cached
    }
    [CertKeyStandInWorld]::Add('KeyRequest', $Owner.Id, $Api)
    switch -CaseSensitive ($key.Acquire) {
        'Null' { return $null }
        'Keyset' { throw [CertKeyStandInWorld]::CryptoError('Keyset') }
        'Denied' { throw [CertKeyStandInWorld]::CryptoError('Denied') }
    }
    $object = New-StandInKey -Key $key -Api $Api -Owned (-not $cached)
    if ($cached) { $Owner.Cached = $object }
    return $object
}

# =====================================================================================================
# 3. Register the stand-ins, then prove they bind before any product file is parsed (fail closed).
# =====================================================================================================
if (-not $ProductRoot) { $ProductRoot = Join-Path $script:ToolRoot 'windows-sql' }
elseif (-not [IO.Path]::IsPathRooted($ProductRoot)) { throw 'ProductRoot must be an explicit absolute path.' }
$script:ProductRoot = [IO.Path]::GetFullPath($ProductRoot)
foreach ($entry in @('New-WindowsSqlCertificate.ps1', 'Remove-WindowsSqlCertificate.ps1',
        'WindowsSql.Certificate.ps1')) {
    if (-not [IO.File]::Exists((Join-Path $script:ProductRoot $entry))) {
        throw ('ProductRoot has no ' + $entry + ': ' + $script:ProductRoot)
    }
}
$script:FixtureMachine = 'LAPTOP-FIXTURE'
$script:FixtureInstance = 'DHFIXTURE'
$script:FixtureInstanceId = 'MSSQL17.DHFIXTURE'
$script:ServiceAccount = 'NT Service\MSSQL$' + $script:FixtureInstance
$script:ServiceSid = 'S-1-5-80-1111111111-2222222222-3333333333-4044444444-555555555'
$script:SChannelProvider = 'Microsoft RSA SChannel Cryptographic Provider'
$script:AesProvider = 'Microsoft Enhanced RSA and AES Cryptographic Provider'
$script:KspProvider = 'Microsoft Software Key Storage Provider'
# CAPI provider types (PROV_RSA_SCHANNEL, PROV_RSA_AES); a KSP has none.
$script:ProviderTypes = @{ $script:SChannelProvider = 12; $script:AesProvider = 24; $script:KspProvider = 0 }
$script:TlsServerName = 'localhost'
$script:AreaCount = 0
$script:AreaName = ''
# Real host facts and types are read through reflection, so no type name below is resolved before its stand-in.
$script:HostMachine = [type]::GetType('System.Environment')::MachineName
$script:RealRsa = [type]::GetType('System.Security.Cryptography.RSA, System.Security.Cryptography', $true)
if ($script:HostMachine -ceq $script:FixtureMachine) {
    throw 'This host has the fixture machine name; refusing to run.'
}
$env:LOCALAPPDATA = Join-Path $script:SuiteRoot 'profile'
[void][IO.Directory]::CreateDirectory((Join-Path $env:LOCALAPPDATA 'Dawnholder'))
$currentIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
try { $script:ExecutorSid = $currentIdentity.User.Value } finally { $currentIdentity.Dispose() }
$script:ProgramData = Join-Path $script:SuiteRoot 'programdata'
$script:MachineKeys = Join-Path $script:ProgramData 'Microsoft\Crypto\RSA\MachineKeys'
[void][IO.Directory]::CreateDirectory($script:MachineKeys)
[CertKeyStandInWorld]::Reset($script:SuiteRoot, $script:ProgramData)
[CertKeyStandInEnvironment]::MachineName = $script:FixtureMachine
[CertKeyStandInEnvironment]::CommonApplicationData = $script:ProgramData
[CertKeyStandInAccount]::Sids[$script:ServiceAccount] = $script:ServiceSid
[CertKeyStandInRsaCsp]::OpenHook = ${function:Open-StandInCapiContainer}
[CertKeyStandInCngKey]::OpenHook = ${function:Open-StandInCngKey}
[CertKeyStandInKeyNative]::ReadHook = ${function:Read-StandInProviderInfo}
[CertKeyStandInKeyNative]::BindHook = ${function:Assert-StandInKeyBinding}
[CertKeyStandInRegistry]::InstanceKey = 'SOFTWARE\Microsoft\Microsoft SQL Server\' + $script:FixtureInstanceId

$standInTypes = [ordered]@{
    'Environment' = [CertKeyStandInEnvironment]
    'System.Environment' = [CertKeyStandInEnvironment]
    'Security.Principal.WindowsPrincipal' = [CertKeyStandInPrincipal]
    'System.Security.Principal.WindowsPrincipal' = [CertKeyStandInPrincipal]
    'Security.Principal.NTAccount' = [CertKeyStandInAccount]
    'System.Security.Principal.NTAccount' = [CertKeyStandInAccount]
    'Microsoft.Win32.RegistryKey' = [CertKeyStandInRegistry]
    'Security.Cryptography.AsymmetricAlgorithm' = [CertKeyStandInAsymmetric]
    'System.Security.Cryptography.AsymmetricAlgorithm' = [CertKeyStandInAsymmetric]
    'Security.Cryptography.RSA' = [CertKeyStandInRsa]
    'System.Security.Cryptography.RSA' = [CertKeyStandInRsa]
    'Security.Cryptography.RSACng' = [CertKeyStandInRsaCng]
    'System.Security.Cryptography.RSACng' = [CertKeyStandInRsaCng]
    'Security.Cryptography.RSACryptoServiceProvider' = [CertKeyStandInRsaCsp]
    'System.Security.Cryptography.RSACryptoServiceProvider' = [CertKeyStandInRsaCsp]
    'Security.Cryptography.CngKey' = [CertKeyStandInCngKey]
    'System.Security.Cryptography.CngKey' = [CertKeyStandInCngKey]
    'Security.Cryptography.X509Certificates.RSACertificateExtensions' = [CertKeyStandInCertificateExtensions]
    'System.Security.Cryptography.X509Certificates.RSACertificateExtensions' = [CertKeyStandInCertificateExtensions]
    'Dawnholder.WindowsSql.CertificateKeyNative' = [CertKeyStandInKeyNative]
}
# Short names cover an implementation that writes `using namespace System.Security.Cryptography`.
foreach ($shortName in @('AsymmetricAlgorithm', 'RSA', 'RSACng', 'RSACryptoServiceProvider', 'CngKey',
        'RSACertificateExtensions', 'NTAccount', 'WindowsPrincipal', 'RegistryKey')) {
    $standInTypes[$shortName] = ($standInTypes.GetEnumerator() | Where-Object {
            $_.Key.EndsWith('.' + $shortName, [StringComparison]::Ordinal)
        } | Select-Object -First 1).Value
}
$accelerators = [psobject].Assembly.GetType('System.Management.Automation.TypeAccelerators')
foreach ($typeName in $standInTypes.Keys) { $accelerators::Add($typeName, $standInTypes[$typeName]) }
$unboundTypes = @(foreach ($typeName in $standInTypes.Keys) {
        $resolved = (& ([scriptblock]::Create('[' + $typeName + ']'))).FullName
        if ($resolved -cne $standInTypes[$typeName].FullName) { $typeName + '=' + $resolved }
    })
Assert-True -Name 'CERTKEY-READY-01 every protected type name resolves to its offline stand-in' `
    -Condition ($unboundTypes.Count -eq 0) -Detail ($unboundTypes -join ',')

# Behaviour of the bound stand-ins, exercised from code parsed after registration like product code is.
[CertKeyStandInWorld]::State.Run = 'ready'
$readyContainer = @{ Wrapper = 'Csp'; Acquire = 'Ok'; Provider = $script:SChannelProvider; ProviderType = 12
    MachineKey = $true; Exportable = $false; KeySize = 2048; KeySpec = 'Exchange'; KeyName = 'certkey-ready-probe'
    UniqueName = 'certkey-ready-probe-file'; MetadataFault = $null }
[CertKeyStandInWorld]::State.Containers['certkey-ready-probe'] = $readyContainer
$readyText = @'
$parameters = [Security.Cryptography.CspParameters]::new(12, 'Microsoft RSA SChannel Cryptographic Provider',
    'certkey-ready-probe')
$parameters.Flags = [Security.Cryptography.CspProviderFlags]'UseMachineKeyStore, UseExistingKey'
$reopened = [Security.Cryptography.RSACryptoServiceProvider]::new($parameters)
$number = [string]$reopened.CspKeyContainerInfo.KeyNumber
$reopened.Dispose()
$parameters.Flags = [Security.Cryptography.CspProviderFlags]'UseMachineKeyStore'
$creation = $null
try { $null = [Security.Cryptography.RSACryptoServiceProvider]::new($parameters) } catch { $creation = 'refused' }
$sid = [Security.Principal.NTAccount]::new($args[0]).Translate([Security.Principal.SecurityIdentifier]).Value
[pscustomobject]@{
    Machine = [Environment]::MachineName
    Folder = [Environment]::GetFolderPath('CommonApplicationData')
    Administrator = [Security.Principal.WindowsPrincipal]::new($null).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)
    Sid = $sid
    KeyNumber = $number
    Creation = $creation
}
'@
$ready = $null
$readyError = $null
if ($unboundTypes.Count -eq 0) {
    try { $ready = & ([scriptblock]::Create($readyText)) $script:ServiceAccount }
    catch { $readyError = $_.Exception.Message }
}
$readyBehaviour = $null -ne $ready -and $ready.Machine -ceq $script:FixtureMachine -and
$ready.Folder -ceq $script:ProgramData -and $ready.Administrator -and $ready.Sid -ceq $script:ServiceSid -and
$ready.KeyNumber -ceq 'Exchange' -and $ready.Creation -ceq 'refused'
Assert-True -Name 'CERTKEY-READY-02 stand-ins answer machine, folder, role, SID and CSP reopen; creation refused' `
    -Condition $readyBehaviour -Detail $(if ($readyError) { $readyError } else { $ready | ConvertTo-Json -Compress })

# The product initializes its native class only when `'<name>' -as [type]` finds nothing, then calls it statically.
# Both lookups and both static calls must land on the stand-in, and Add-Type must refuse that class's definition.
$nativeText = @'
$found = 'Dawnholder.WindowsSql.CertificateKeyNative' -as [type]
$parameters = [Dawnholder.WindowsSql.CertificateKeyNative]::ReadProviderParameters($args[0])
[Dawnholder.WindowsSql.CertificateKeyNative]::AssertKeyBinding($args[0])
$refused = $false
try { Add-Type -TypeDefinition 'namespace CertKeyProbe { public static class CertificateKeyNative {} }' }
catch { $refused = $true }
[pscustomobject]@{
    Found = $(if ($null -ne $found) { $found.FullName })
    Literal = [Dawnholder.WindowsSql.CertificateKeyNative].FullName
    Container = $parameters.KeyContainerName
    AddType = (Get-Command -Name 'Add-Type').CommandType.ToString()
    AddTypeFile = (Get-Command -Name 'Add-Type').ScriptBlock.File
    Refused = $refused
    ProbeLoaded = $null -ne ('CertKeyProbe.CertificateKeyNative' -as [type])
}
'@
$readyCertificateState = @{ Id = 0; Disposed = $false; TypeName = 'X509Certificate2'
    Spec = @{ Key = $readyContainer } }
$readyCertificate = [pscustomobject]@{ CertKeyStandInState = $readyCertificateState }
$native = $null
$nativeError = $null
if ($unboundTypes.Count -eq 0) {
    try { $native = & ([scriptblock]::Create($nativeText)) $readyCertificate }
    catch { $nativeError = $_.Exception.Message }
}
$nativeCalls = @([CertKeyStandInWorld]::State.Events | Where-Object {
        $_.Run -ceq 'ready' -and $_.Kind -cin @('NativeProviderInfo', 'NativeKeyBinding')
    })
$refusedProbe = @([CertKeyStandInWorld]::State.Events | Where-Object {
        $_.Run -ceq 'ready' -and $_.Kind -ceq 'RefusedNativeCompile'
    })
$nativeBound = $null -ne $native -and $native.Found -ceq 'CertKeyStandInKeyNative' -and
$native.Literal -ceq 'CertKeyStandInKeyNative' -and $native.Container -ceq 'certkey-ready-probe' -and
$native.AddType -ceq 'Function' -and $native.AddTypeFile -ceq $PSCommandPath -and $native.Refused -and
-not $native.ProbeLoaded -and $nativeCalls.Count -eq 2 -and $refusedProbe.Count -eq 1
Assert-True -Name 'CERTKEY-READY-03 native key type lookup and static calls bind to the stand-in; Add-Type refuses' `
    -Condition $nativeBound -Detail $(if ($nativeError) { $nativeError } else { $native | ConvertTo-Json -Compress })
if ($unboundTypes.Count -ne 0 -or -not $readyBehaviour -or -not $nativeBound) {
    Add-TestResult -Name 'CERTKEY-READY-00 product entries not executed: stand-ins are not bound' -Outcome FAIL
    Complete-TestSuite
}
[CertKeyStandInWorld]::State.Containers.Remove('certkey-ready-probe')

. (Join-Path $script:ProductRoot 'WindowsSql.Common.ps1')
. (Join-Path $script:ProductRoot 'WindowsSql.Adapter.ps1')

# =====================================================================================================
# 4. Fixture helpers: in-memory public certificates, key specs, key files and ACLs below this suite.
# =====================================================================================================
function Write-JsonFile {
    param([string]$Path, $Value)
    $text = $Value | ConvertTo-Json -Depth 20
    [IO.File]::WriteAllText($Path, $text + "`n", [Text.UTF8Encoding]::new($false))
}

function Get-FileSha256 {
    param([string]$Path)
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([IO.File]::ReadAllBytes($Path)))
}

function New-ExpiryText {
    param([double]$Hours)
    return [DateTimeOffset]::UtcNow.AddHours($Hours).ToString("yyyy-MM-dd'T'HH:mm:ss'Z'",
        [Globalization.CultureInfo]::InvariantCulture)
}

function New-PublicCertificate {
    # The signing key exists only in memory for this call; the fixture keeps the public certificate alone.
    $rsa = $script:RealRsa::Create(2048)
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
        try { return [Security.Cryptography.X509Certificates.X509Certificate2]::new($signed.RawData) }
        finally { $signed.Dispose() }
    }
    finally { $rsa.Dispose() }
}

function New-KeySpec {
    param([string]$Id, [hashtable]$Override = @{})
    $guid = [Guid]::NewGuid().ToString()
    $spec = @{
        Wrapper = 'Cng'
        Acquire = 'Ok'
        Provider = $script:SChannelProvider
        MachineKey = $true
        Exportable = $false
        KeySize = 2048
        KeySpec = 'Exchange'
        KeyName = 'te-' + $guid
        UniqueName = [Guid]::NewGuid().ToString('N') + '_' + $guid
        MetadataFault = $null
    }
    foreach ($name in $Override.Keys) { $spec[$name] = $Override[$name] }
    # Placeholders for names that leave the MachineKeys folder or address an NTFS stream.
    if ($spec.UniqueName -ceq 'ESCAPE') { $spec.UniqueName = '..\..\..\..\escape-' + $Id }
    if ($spec.UniqueName -ceq 'STREAM') { $spec.UniqueName = 'stream-' + $Id + ':key' }
    $spec.ProviderType = 0
    if ($null -ne $spec.Provider -and $script:ProviderTypes.ContainsKey($spec.Provider)) {
        $spec.ProviderType = $script:ProviderTypes[$spec.Provider]
    }
    return $spec
}

function Set-FixtureServiceRule {
    param([string]$Path, [ValidateSet('None', 'Read', 'Deny', 'ReadAttributes')][string]$Mode)
    if ($Mode -ceq 'None') { return }
    $item = if ([IO.Directory]::Exists($Path)) { [IO.DirectoryInfo]::new($Path) } else { [IO.FileInfo]::new($Path) }
    $security = [IO.FileSystemAclExtensions]::GetAccessControl($item,
        [Security.AccessControl.AccessControlSections]::Access)
    $sid = [Security.Principal.SecurityIdentifier]::new($script:ServiceSid)
    $allowed = if ($Mode -ceq 'ReadAttributes') { 'ReadAttributes' } else { 'Read' }
    $security.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($sid, $allowed, 'Allow'))
    if ($Mode -ceq 'Deny') {
        $security.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($sid, 'Read', 'Deny'))
    }
    [IO.FileSystemAclExtensions]::SetAccessControl($item, $security)
}

function Get-ServiceRules {
    param([string]$Path)
    if (-not ([IO.File]::Exists($Path) -or [IO.Directory]::Exists($Path))) { return @() }
    $item = if ([IO.Directory]::Exists($Path)) { [IO.DirectoryInfo]::new($Path) } else { [IO.FileInfo]::new($Path) }
    $security = [IO.FileSystemAclExtensions]::GetAccessControl($item,
        [Security.AccessControl.AccessControlSections]::Access)
    return @($security.GetAccessRules($true, $false, [Security.Principal.SecurityIdentifier]) | Where-Object {
            $_.IdentityReference.Value -ceq $script:ServiceSid
        })
}

function Enter-KeyArea {
    # Every entry run gets its own CommonApplicationData, so an ACL that one run writes cannot satisfy or spoil the
    # observation of another run.
    $script:AreaCount++
    $script:AreaName = 'a' + $script:AreaCount
    $script:ProgramData = Join-Path $script:SuiteRoot ('programdata\' + $script:AreaName)
    $script:MachineKeys = Join-Path $script:ProgramData 'Microsoft\Crypto\RSA\MachineKeys'
    [void][IO.Directory]::CreateDirectory($script:MachineKeys)
    [CertKeyStandInEnvironment]::CommonApplicationData = $script:ProgramData
    [CertKeyStandInWorld]::State.KeyArea = $script:ProgramData
}

function Initialize-KeyArea {
    param([string]$Id, [hashtable]$Key, [string]$KeyFile = 'Present', [string]$ServiceAcl = 'None')
    # Returns the exact key file path the requirement names and every path a misdirected ACL write could reach.
    Enter-KeyArea
    $exact = $script:MachineKeys + '\' + [string]$Key.UniqueName
    $watched = [Collections.Generic.List[string]]::new()
    $watched.Add($script:MachineKeys)
    $usable = -not [string]::IsNullOrEmpty($Key.UniqueName) -and -not ([string]$Key.UniqueName).Contains(':')
    $target = $null
    if ($usable) {
        $target = [IO.Path]::GetFullPath($exact)
        $watched.Add($target)
        switch -CaseSensitive ($KeyFile) {
            'Present' {
                [IO.File]::WriteAllText($target, 'certificate key fixture file, not key material')
                Set-FixtureServiceRule -Path $target -Mode $ServiceAcl
            }
            'Junction' {
                $junctionTarget = Join-Path $script:SuiteRoot ('junction-targets\' + $script:AreaName)
                [void][IO.Directory]::CreateDirectory($junctionTarget)
                $watched.Add($junctionTarget)
                $null = Microsoft.PowerShell.Management\New-Item -ItemType Junction -Path $target `
                    -Target $junctionTarget
            }
        }
    }
    elseif ($KeyFile -ceq 'Present' -and [string]::IsNullOrEmpty($Key.UniqueName)) {
        # Without a container name the product path is the MachineKeys folder itself, which is the decoy here.
        Set-FixtureServiceRule -Path $script:MachineKeys -Mode $ServiceAcl
    }
    return [pscustomobject]@{
        ProgramData = $script:ProgramData
        Exact = $exact
        Target = $target
        Watched = $watched.ToArray()
    }
}

function Register-KeyContainer {
    param([hashtable]$Key)
    [CertKeyStandInWorld]::State.Containers[$Key.KeyName] = $Key
}

function Get-RunEvents {
    param([string]$Run, [string]$Kind = '')
    return @([CertKeyStandInWorld]::State.Events | Where-Object {
            $_.Run -ceq $Run -and ($Kind -eq '' -or $_.Kind -ceq $Kind)
        })
}

function Get-OwnedLeaks {
    param([string]$Run)
    $owned = @(Get-RunEvents -Run $Run -Kind 'Acquire' | Where-Object { $_.Detail -clike '*owned=True' } |
            ForEach-Object Id)
    $disposed = @(Get-RunEvents -Run $Run -Kind 'Dispose' | ForEach-Object Id)
    return @($owned | Where-Object { $_ -notin $disposed })
}

function Get-UndisposedCertificates {
    param([string]$Run)
    $issued = @(Get-RunEvents -Run $Run -Kind 'CertificateIssue' | ForEach-Object Id)
    $disposed = @(Get-RunEvents -Run $Run -Kind 'CertificateDispose' | ForEach-Object Id)
    return @($issued | Where-Object { $_ -notin $disposed })
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

$script:PublicCodePattern = '^WINDOWS_SQL_[A-Z0-9_]+$'
$script:FallbackCodes = @('WINDOWS_SQL_CERTIFICATE_PREPARATION_FAILED', 'WINDOWS_SQL_CERTIFICATE_CLEANUP_FAILED')
$script:EntryOutputs = [Collections.Generic.List[string]]::new()

# =====================================================================================================
# 5. Entry runners: the real create script, the real Assert function and the real remove script.
# =====================================================================================================
function Invoke-CreateCase {
    param([string]$Id, [hashtable]$Key, [string]$KeyFile = 'Present', [bool]$Administrator = $true,
        [switch]$OccupyPublicCaAfterKeyAcl)
    $run = 'create:' + $Id
    [CertKeyStandInWorld]::State.Run = $run
    $root = Join-Path $script:SuiteRoot ('create-' + $Id)
    $publicRoot = Join-Path $root 'public'
    [void][IO.Directory]::CreateDirectory($publicRoot)
    $lifetime = [Guid]::NewGuid().ToString('N')
    $private = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $lifetime)
    $caPath = Join-Path $publicRoot 'public-ca.pem'
    $preparationPath = Join-Path $root 'preparation.json'
    Write-JsonFile -Path $preparationPath -Value ([ordered]@{
            Version = 1
            ExecutionApproved = $true
            ExpiresUtc = (New-ExpiryText -Hours 3)
            LifetimeId = $lifetime
            MachineName = $script:FixtureMachine
            InstanceName = $script:FixtureInstance
            ExecutorSid = $script:ExecutorSid
            ServiceAccount = $script:ServiceAccount
            TlsServerName = $script:TlsServerName
            PrivateRoot = $private
            PublicCaPath = $caPath
        })
    $keySpec = New-KeySpec -Id $Id -Override $Key
    $area = Initialize-KeyArea -Id $Id -Key $keySpec -KeyFile $KeyFile -ServiceAcl 'None'
    Register-KeyContainer -Key $keySpec
    $public = New-PublicCertificate
    [CertKeyStandInWorld]::State.PendingCreation = @{ Public = $public; Key = $keySpec }
    [CertKeyStandInPrincipal]::Administrator = $Administrator
    if ($OccupyPublicCaAfterKeyAcl) {
        $occupy = { param($written) [IO.File]::WriteAllText($caPath, 'occupied after the key ACL write') }
        [CertKeyStandInWorld]::State.AfterKeyAclWrite = $occupy.GetNewClosure()
    }
    $arguments = @{ PreparationPath = $preparationPath; ExpectedPreparationHash = (Get-FileSha256 $preparationPath) }
    try {
        $result = Invoke-ProductScript -Path (Join-Path $script:ProductRoot 'New-WindowsSqlCertificate.ps1') `
            -Arguments $arguments
    }
    finally {
        [CertKeyStandInPrincipal]::Administrator = $true
        [CertKeyStandInWorld]::State.PendingCreation = $null
        [CertKeyStandInWorld]::State.AfterKeyAclWrite = $null
    }
    $script:EntryOutputs.Add($result.Stdout + "`n" + $result.Code)
    $createdPath = Join-Path $private 'certificate-created.txt'
    $receiptPath = Join-Path $private 'certificate.json'
    return [pscustomobject]@{
        Id = $Id
        Run = $run
        Result = $result
        Key = $keySpec
        Area = $area
        Public = $public
        Private = $private
        PublicRoot = $publicRoot
        CaPath = $caPath
        Lifetime = $lifetime
        PrivateExists = [IO.Directory]::Exists($private)
        CreatedThumbprint = $(if ([IO.File]::Exists($createdPath)) { [IO.File]::ReadAllText($createdPath) })
        ReceiptPath = $receiptPath
        ReceiptExists = [IO.File]::Exists($receiptPath)
        CaExists = [IO.File]::Exists($caPath)
    }
}

function New-CertificateReceiptCase {
    param([string]$Id, [string]$Root, [string]$Lifetime, [string]$Private, $Public, [string]$KeyFile,
        [string]$ServiceSid, [byte[]]$CaBytes, [string]$CaPath)
    # Fixture receipt in the product's receipt shape, owned by the executor like a prepared one.
    [IO.File]::WriteAllBytes($CaPath, $CaBytes)
    $receiptPath = Join-Path $Private 'certificate.json'
    Write-JsonFile -Path $receiptPath -Value ([ordered]@{
            Version = 1
            PreparationHash = ('E' * 64)
            LifetimeId = $Lifetime
            MachineName = $script:FixtureMachine
            InstanceName = $script:FixtureInstance
            ExecutorSid = $script:ExecutorSid
            Thumbprint = $Public.Thumbprint
            PublicCaSha256 = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($CaBytes))
            KeyFile = $KeyFile
            ServiceSid = $ServiceSid
        })
    Set-WindowsSqlPrivateFileOwner -Path $receiptPath -Sid $script:ExecutorSid
    return Get-FileSha256 -Path $receiptPath
}

function Invoke-AssertCase {
    param([string]$Id, [hashtable]$Key, [string]$KeyFile = 'Present', [string]$ServiceAcl = 'Read',
        [string]$ReceiptKeyFile = '', [string]$ReceiptServiceSid = '', [switch]$CaMismatch, [switch]$AclReadDenied)
    $run = 'assert:' + $Id
    [CertKeyStandInWorld]::State.Run = $run
    $root = Join-Path $script:SuiteRoot ('assert-' + $Id)
    $source = Join-Path $root 'source'
    [void][IO.Directory]::CreateDirectory($source)
    $lifetime = [Guid]::NewGuid().ToString('N')
    $private = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $lifetime)
    New-WindowsSqlPrivateDirectory -Path $private -Sid $script:ExecutorSid
    $keySpec = New-KeySpec -Id $Id -Override $Key
    $area = Initialize-KeyArea -Id $Id -Key $keySpec -KeyFile $KeyFile -ServiceAcl $ServiceAcl
    Register-KeyContainer -Key $keySpec
    if ($AclReadDenied -and $null -ne $area.Target) {
        [void][CertKeyStandInWorld]::State.AclReadDenied.Add($area.Target)
    }
    $public = New-PublicCertificate
    [CertKeyStandInWorld]::State.Store[$public.Thumbprint] = @{ Public = $public; Key = $keySpec }
    $caPublic = if ($CaMismatch) { New-PublicCertificate } else { $public }
    $caBytes = [Text.UTF8Encoding]::new($false).GetBytes($caPublic.ExportCertificatePem() + "`n")
    # OTHER names another container file in the same MachineKeys folder.
    $receiptKey = $area.Exact
    if ($ReceiptKeyFile -ceq 'OTHER') { $receiptKey = $script:MachineKeys + '\other-container' }
    $receiptHash = New-CertificateReceiptCase -Id $Id -Root $root -Lifetime $lifetime -Private $private `
        -Public $public -CaBytes $caBytes -CaPath (Join-Path $source 'public-ca.pem') -KeyFile $receiptKey `
        -ServiceSid $(if ($ReceiptServiceSid) { $ReceiptServiceSid } else { $script:ServiceSid })
    $adapter = [pscustomobject]@{
        Plan = [pscustomobject]@{
            PrivateRoot = $private
            LifetimeId = $lifetime
            MachineName = $script:FixtureMachine
            InstanceName = $script:FixtureInstance
            CertificateThumbprint = $public.Thumbprint
            SourceRoot = $source
            Inputs = @([pscustomobject]@{
                    Path = 'public-ca.pem'
                    Sha256 = (Get-FileSha256 (Join-Path $source 'public-ca.pem'))
                })
        }
        Runtime = [pscustomobject]@{
            ExecutorSid = $script:ExecutorSid
            CertificateReceiptHash = $receiptHash
            TlsServerName = $script:TlsServerName
            ServiceAccount = $script:ServiceAccount
            PublicCaPath = 'public-ca.pem'
        }
    }
    $message = $null
    try { Assert-WindowsSqlCertificate -Adapter $adapter }
    catch { $message = $_.Exception.Message }
    finally { if ($null -ne $area.Target) { [void][CertKeyStandInWorld]::State.AclReadDenied.Remove($area.Target) } }
    return [pscustomobject]@{ Id = $Id; Run = $run; Code = $message; Key = $keySpec; Area = $area; Public = $public }
}

function Invoke-AssertOnCreated {
    param($Created)
    # Verification after a real creation: the receipt, CA file and key ACL come from the create entry itself.
    $run = 'assert-after-create:' + $Created.Id
    [CertKeyStandInWorld]::State.Run = $run
    [CertKeyStandInEnvironment]::CommonApplicationData = $Created.Area.ProgramData
    [CertKeyStandInWorld]::State.KeyArea = $Created.Area.ProgramData
    if (-not $Created.ReceiptExists -or -not $Created.CaExists) {
        return [pscustomobject]@{ Run = $run; Code = 'NOT_RUN: create produced no receipt or public CA' }
    }
    $adapter = [pscustomobject]@{
        Plan = [pscustomobject]@{
            PrivateRoot = $Created.Private
            LifetimeId = $Created.Lifetime
            MachineName = $script:FixtureMachine
            InstanceName = $script:FixtureInstance
            CertificateThumbprint = $Created.Public.Thumbprint
            SourceRoot = $Created.PublicRoot
            Inputs = @([pscustomobject]@{ Path = 'public-ca.pem'; Sha256 = (Get-FileSha256 $Created.CaPath) })
        }
        Runtime = [pscustomobject]@{
            ExecutorSid = $script:ExecutorSid
            CertificateReceiptHash = (Get-FileSha256 $Created.ReceiptPath)
            TlsServerName = $script:TlsServerName
            ServiceAccount = $script:ServiceAccount
            PublicCaPath = 'public-ca.pem'
        }
    }
    $message = $null
    try { Assert-WindowsSqlCertificate -Adapter $adapter }
    catch { $message = $_.Exception.Message }
    return [pscustomobject]@{ Run = $run; Code = $message }
}

function New-RemoveTree {
    # A byte-identical copy of the product's required inputs: Remove binds its SourceRoot to its own checkout and
    # this suite may not write fixture inputs into the repository.
    $tree = Join-Path $script:SuiteRoot 'remove-tree'
    $sourceRepo = [IO.Path]::GetFullPath((Join-Path $script:ProductRoot '..\..\..'))
    $database = '99_Tools/database/'
    $relative = [Collections.Generic.List[string]]::new()
    foreach ($name in @('Database.Common.ps1', 'SqlError.Common.ps1', 'Module.Common.ps1', 'ModuleHash.Common.ps1',
            'Test-ModuleStructure.ps1', 'verify-schema.sql', 'modules/manifest.json', 'migrations/001_initial.sql',
            'migrations/002_persistence_metadata.sql', 'migrations/003_module_metadata.sql',
            'migrations/004_module_release.sql', 'windows-sql/WindowsSqlVerifyHost/Program.cs',
            'windows-sql/WindowsSqlVerifyHost/WindowsSqlVerifyHost.csproj')) {
        $relative.Add($database + $name)
    }
    $manifest = [IO.File]::ReadAllText((Join-Path $sourceRepo ($database + 'modules/manifest.json'))) | ConvertFrom-Json
    foreach ($entry in $manifest.Entries) { $relative.Add($database + [string]$entry.Path) }
    foreach ($file in [IO.Directory]::GetFiles($script:ProductRoot, '*.ps1')) {
        $relative.Add($database + 'windows-sql/' + [IO.Path]::GetFileName($file))
    }
    foreach ($name in @('99_Tools/sync-wsl.sh', '99_Tools/Formatting/sdk.sh', 'global.json')) { $relative.Add($name) }
    $mismatch = [Collections.Generic.List[string]]::new()
    foreach ($path in $relative) {
        $from = Join-Path $sourceRepo $path
        $to = Join-Path $tree $path
        [void][IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($to))
        [IO.File]::Copy($from, $to)
        if ((Get-FileSha256 $from) -cne (Get-FileSha256 $to)) { $mismatch.Add($path) }
    }
    [void][IO.Directory]::CreateDirectory((Join-Path $tree '.backups\evidence'))
    return [pscustomobject]@{ Root = $tree; Inputs = $relative.ToArray(); Mismatch = $mismatch.ToArray() }
}

function Invoke-RemoveCase {
    param([string]$Id, [hashtable]$Key, [string]$ServiceAcl = 'Read')
    $run = 'remove:' + $Id
    [CertKeyStandInWorld]::State.Run = $run
    $tree = $script:RemoveTree.Root
    $fixtureRelative = 'fixtures/' + $Id
    $fixtureRoot = Join-Path $tree $fixtureRelative
    [void][IO.Directory]::CreateDirectory($fixtureRoot)
    $lifetime = [Guid]::NewGuid().ToString('N')
    $private = Join-Path $env:LOCALAPPDATA ('Dawnholder\' + $lifetime)
    New-WindowsSqlPrivateDirectory -Path $private -Sid $script:ExecutorSid
    $keySpec = New-KeySpec -Id $Id -Override $Key
    $area = Initialize-KeyArea -Id ('remove-' + $Id) -Key $keySpec -ServiceAcl $ServiceAcl
    Register-KeyContainer -Key $keySpec
    $public = New-PublicCertificate
    [CertKeyStandInWorld]::State.Store[$public.Thumbprint] = @{ Public = $public; Key = $keySpec }
    $caBytes = [Text.UTF8Encoding]::new($false).GetBytes($public.ExportCertificatePem() + "`n")
    $receiptHash = New-CertificateReceiptCase -Id $Id -Root $fixtureRoot -Lifetime $lifetime -Private $private `
        -Public $public -KeyFile $area.Exact -ServiceSid $script:ServiceSid -CaBytes $caBytes `
        -CaPath (Join-Path $fixtureRoot 'public-ca.pem')
    [IO.File]::WriteAllText((Join-Path $fixtureRoot 'profile.json'), "{`"fixture`":true}`n")
    Write-JsonFile -Path (Join-Path $fixtureRoot 'runtime.json') -Value ([ordered]@{
            Version = 1
            LifetimeId = $lifetime
            ExecutorSid = $script:ExecutorSid
            ServiceAccount = $script:ServiceAccount
            EndpointAddress = '127.0.0.1'
            TlsServerName = $script:TlsServerName
            NetworkMode = 'Mirrored'
            WslSubnet = $null
            WslDistribution = 'Ubuntu'
            AccountId = [Guid]::NewGuid().ToString('D')
            CharacterId = [Guid]::NewGuid().ToString('D')
            PublicCaPath = $fixtureRelative + '/public-ca.pem'
            CredentialPath = (Join-Path $private 'runtime.clixml')
            CertificateReceiptHash = $receiptHash
            ProfilePath = $fixtureRelative + '/profile.json'
            VerifyHostFiles = @(foreach ($file in @('WindowsSqlVerifyHost.dll', 'WindowsSqlVerifyHost.deps.json',
                        'WindowsSqlVerifyHost.runtimeconfig.json', 'Microsoft.Data.SqlClient.dll')) {
                    [ordered]@{ Path = $file; Sha256 = ('D' * 64) }
                })
        })
    $inputs = @(foreach ($path in @($script:RemoveTree.Inputs) + @(
                ($fixtureRelative + '/runtime.json'), ($fixtureRelative + '/profile.json'),
                ($fixtureRelative + '/public-ca.pem'))) {
            [ordered]@{ Path = $path; Sha256 = (Get-FileSha256 (Join-Path $tree $path)) }
        })
    $planPath = Join-Path $script:SuiteRoot ('remove-plan-' + $Id + '.json')
    Write-JsonFile -Path $planPath -Value ([ordered]@{
            Version = 1
            ExecutionApproved = $true
            ExpiresUtc = (New-ExpiryText -Hours 3)
            LifetimeId = $lifetime
            MachineName = $script:FixtureMachine
            InstanceName = $script:FixtureInstance
            InstanceId = $script:FixtureInstanceId
            ExpectedProductVersion = '17.0.1000.7'
            Port = 15433
            Database = 'Dawnholder_CertKeyFixture'
            RuntimeLogin = 'dh_certkey_fixture'
            SchemaVersion = 4
            CertificateThumbprint = $public.Thumbprint
            Encrypt = 'Mandatory'
            TrustServerCertificate = $false
            EnableSa = $false
            SwitchWslNetworkingMode = $false
            ForceEncryption = 'Preserve'
            RestartApproved = $true
            FirewallApproved = $true
            SourceRoot = $tree
            EvidenceRoot = (Join-Path $tree '.backups\evidence')
            PrivateRoot = $private
            Inputs = $inputs
        })
    $planHash = Get-FileSha256 $planPath
    # The separate removal step runs only after Cleanup restored the original instance configuration.
    $storeAdapter = [pscustomobject]@{
        Plan = [pscustomobject]@{ PrivateRoot = $private; LifetimeId = $lifetime; PlanHash = $planHash }
        Runtime = [pscustomobject]@{ ExecutorSid = $script:ExecutorSid }
    }
    Write-WindowsSqlPrivateJson -Adapter $storeAdapter -Name 'lifecycle.json' -Value ([ordered]@{
            LifetimeId = $lifetime
            PlanHash = $planHash
            Status = 'Cleaned'
            OriginalConfiguration = [ordered]@{
                LoginMode = 'Windows'
                TcpEnabled = $false
                TcpPort = $null
                DynamicPorts = $true
                ForceEncryption = $false
                CertificateThumbprint = $null
            }
        })
    $entry = Join-Path $tree '99_Tools\database\windows-sql\Remove-WindowsSqlCertificate.ps1'
    $arguments = @{
        PlanPath = $planPath
        ExpectedPlanHash = $planHash
        RuntimePath = ($fixtureRelative + '/runtime.json')
    }
    $result = Invoke-ProductScript -Path $entry -Arguments $arguments
    $script:EntryOutputs.Add($result.Stdout + "`n" + $result.Code)
    $journalPath = Join-Path $private 'certificate-cleanup.json'
    $journal = $null
    if ([IO.File]::Exists($journalPath)) { $journal = [IO.File]::ReadAllText($journalPath) | ConvertFrom-Json }
    return [pscustomobject]@{ Id = $Id; Run = $run; Result = $result; Public = $public; Journal = $journal }
}

# =====================================================================================================
# 6. Key cases. Class is the requirement meaning: Supported, Access (the key cannot be reached), Unsupported (the
#    key is readable but not a SQL-supported key) or Rejected (metadata or key file path cannot be proven).
# =====================================================================================================
$keyCases = @(
    @{ Id = 'csp-supported'; Class = 'Supported'; Key = @{ Wrapper = 'Csp' } },
    @{ Id = 'cng-supported'; Class = 'Supported'; Key = @{ Wrapper = 'Cng' } },
    @{ Id = 'key-null'; Class = 'Access'; Key = @{ Wrapper = 'Cng'; Acquire = 'Null' } },
    @{ Id = 'keyset-missing'; Class = 'Access'; Key = @{ Wrapper = 'Cng'; Acquire = 'Keyset' } },
    @{ Id = 'key-access-denied'; Class = 'Access'; Key = @{ Wrapper = 'Cng'; Acquire = 'Denied' } },
    @{ Id = 'key-store-failure'; Class = 'Access'; Key = @{ Wrapper = 'Cng'; MetadataFault = 'Key' } },
    @{ Id = 'cng-other-provider'; Class = 'Unsupported'; Key = @{ Wrapper = 'Cng'; Provider = $script:KspProvider } },
    @{ Id = 'cng-user-key'; Class = 'Unsupported'; Key = @{ Wrapper = 'Cng'; MachineKey = $false } },
    @{ Id = 'cng-exportable'; Class = 'Unsupported'; Key = @{ Wrapper = 'Cng'; Exportable = $true } },
    @{ Id = 'cng-1024-bit'; Class = 'Unsupported'; Key = @{ Wrapper = 'Cng'; KeySize = 1024 } },
    @{ Id = 'cng-signature-key'; Class = 'Unsupported'; Key = @{ Wrapper = 'Cng'; KeySpec = 'Signature' } },
    @{ Id = 'csp-other-provider'; Class = 'Unsupported'; Key = @{ Wrapper = 'Csp'; Provider = $script:AesProvider } },
    @{ Id = 'csp-user-key'; Class = 'Unsupported'; Key = @{ Wrapper = 'Csp'; MachineKey = $false } },
    @{ Id = 'csp-exportable'; Class = 'Unsupported'; Key = @{ Wrapper = 'Csp'; Exportable = $true } },
    @{ Id = 'csp-1024-bit'; Class = 'Unsupported'; Key = @{ Wrapper = 'Csp'; KeySize = 1024 } },
    @{ Id = 'csp-signature-key'; Class = 'Unsupported'; Key = @{ Wrapper = 'Csp'; KeySpec = 'Signature' } },
    @{ Id = 'cng-export-policy-unreadable'; Class = 'Rejected'
        Key = @{ Wrapper = 'Cng'; MetadataFault = 'ExportPolicy' } },
    @{ Id = 'cng-provider-unreadable'; Class = 'Rejected'; Key = @{ Wrapper = 'Cng'; MetadataFault = 'Provider' } },
    @{ Id = 'cng-unique-name-missing'; Class = 'Rejected'; Key = @{ Wrapper = 'Cng'; UniqueName = '' } },
    @{ Id = 'csp-unique-name-missing'; Class = 'Rejected'; Key = @{ Wrapper = 'Csp'; UniqueName = '' } },
    @{ Id = 'csp-container-info-unreadable'; Class = 'Rejected'
        Key = @{ Wrapper = 'Csp'; MetadataFault = 'ContainerInfo' } },
    # Path cases: the key metadata is valid, the key file the receipt would name is not provable.
    @{ Id = 'cng-container-missing'; Class = 'Rejected'; Path = $true; KeyFile = 'Missing'; Key = @{ Wrapper = 'Cng' }
    },
    @{ Id = 'csp-container-missing'; Class = 'Rejected'; Path = $true; KeyFile = 'Missing'; Key = @{ Wrapper = 'Csp' }
    },
    @{ Id = 'cng-path-escape'; Class = 'Rejected'; Path = $true; Key = @{ Wrapper = 'Cng'; UniqueName = 'ESCAPE' } },
    @{ Id = 'csp-path-escape'; Class = 'Rejected'; Path = $true; Key = @{ Wrapper = 'Csp'; UniqueName = 'ESCAPE' } },
    @{ Id = 'cng-stream-name'; Class = 'Rejected'; Path = $true; Key = @{ Wrapper = 'Cng'; UniqueName = 'STREAM' } },
    @{ Id = 'cng-key-file-junction'; Class = 'Rejected'; Path = $true; KeyFile = 'Junction'; Key = @{ Wrapper = 'Cng' }
    },
    @{ Id = 'csp-key-file-junction'; Class = 'Rejected'; Path = $true; KeyFile = 'Junction'; Key = @{ Wrapper = 'Csp' }
    }
)

# ---- 6a. The create entry for every key case.
$created = @{}
foreach ($case in $keyCases) {
    $keyFile = if ($case.ContainsKey('KeyFile')) { $case.KeyFile } else { 'Present' }
    $created[$case.Id] = Invoke-CreateCase -Id $case.Id -Key $case.Key -KeyFile $keyFile
}
foreach ($case in $keyCases) {
    $c = $created[$case.Id]
    $events = @(Get-RunEvents -Run $c.Run)
    $keyAclWrites = @($events | Where-Object Kind -CEQ 'KeyAclWrite' | ForEach-Object Detail)
    $serviceRulesOnWatched = @(foreach ($path in $c.Area.Watched) {
            if ($path -cne $c.Area.Target -or $case.Class -cne 'Supported') { Get-ServiceRules -Path $path }
        })
    $storeRemovals = @($events | Where-Object Kind -CEQ 'StoreRemove')
    $fileRemovals = @($events | Where-Object Kind -CEQ 'FileRemove')
    $prefix = 'CERTKEY-CREATE ' + $case.Id + ': '
    if ($case.Class -ceq 'Supported') {
        $exitSucceeded = $c.Result.Exit -eq 0 -and $c.Result.Code -eq ''
        $status = $null
        try { $status = $c.Result.Stdout | ConvertFrom-Json } catch { $status = $null }
        $statusPrepared = $null -ne $status -and $status.Status -ceq 'Prepared' -and
        $status.Thumbprint -ceq $c.Public.Thumbprint
        Assert-True -Name ($prefix + 'supported key prepares the certificate (exit 0, Prepared, exact thumbprint)') `
            -Condition ($exitSucceeded -and $statusPrepared) `
            -Detail ('exit=' + $c.Result.Exit + ' code=' + $c.Result.Code + ' escaped=' + $c.Result.Escaped)
        $exactRules = @(Get-ServiceRules -Path $c.Area.Target)
        $readGranted = $exactRules.Count -eq 1 -and $exactRules[0].AccessControlType -eq 'Allow' -and
        ($exactRules[0].FileSystemRights -band [Security.AccessControl.FileSystemRights]::Read) -eq
        [Security.AccessControl.FileSystemRights]::Read
        Assert-True -Name ($prefix + 'service SID gets one Allow Read rule on the exact key file') `
            -Condition $readGranted -Detail ('rules=' + $exactRules.Count)
        $onlyExactWrite = $keyAclWrites.Count -eq 1 -and $keyAclWrites[0] -ieq $c.Area.Target -and
        $serviceRulesOnWatched.Count -eq 0
        Assert-True -Name ($prefix + 'the only key-area ACL write is the exact key file') -Condition $onlyExactWrite `
            -Detail ('writes=' + ($keyAclWrites -join ',') + ' otherServiceRules=' + $serviceRulesOnWatched.Count)
        $receipt = $null
        if ($c.ReceiptExists) { $receipt = [IO.File]::ReadAllText($c.ReceiptPath) | ConvertFrom-Json }
        $receiptBound = $null -ne $receipt -and $receipt.KeyFile -ceq $c.Area.Exact -and
        $receipt.ServiceSid -ceq $script:ServiceSid -and $receipt.Thumbprint -ceq $c.Public.Thumbprint -and
        $c.CaExists -and $receipt.PublicCaSha256 -ceq (Get-FileSha256 $c.CaPath)
        Assert-True -Name ($prefix + 'receipt binds the exact key file, service SID, thumbprint and public CA hash') `
            -Condition $receiptBound -Detail $(if ($null -ne $receipt) { $receipt | ConvertTo-Json -Compress })
        $after = Invoke-AssertOnCreated -Created $c
        Assert-True -Name ($prefix + 'Assert accepts what the create entry prepared (same key decision)') `
            -Condition ($null -eq $after.Code) -Detail ([string]$after.Code)
        continue
    }
    $exitFailed = $c.Result.Exit -eq 1 -and $null -eq $c.Result.Escaped
    $codePublic = $c.Result.Code -cmatch $script:PublicCodePattern -and $c.Result.Code -cnotin $script:FallbackCodes
    Assert-True -Name ($prefix + 'rejected with a specific public error code') `
        -Condition ($exitFailed -and $codePublic) `
        -Detail ('exit=' + $c.Result.Exit + ' code=' + $c.Result.Code + ' escaped=' + $c.Result.Escaped)
    $noKeyAclChange = $keyAclWrites.Count -eq 0 -and $serviceRulesOnWatched.Count -eq 0
    Assert-True -Name ($prefix + 'no ACL write reaches the key file, MachineKeys folder or any redirected target') `
        -Condition $noKeyAclChange `
        -Detail ('writes=' + ($keyAclWrites -join ',') + ' serviceRules=' + $serviceRulesOnWatched.Count)
    $thumbprintKept = $c.PrivateExists -and $c.CreatedThumbprint -ceq $c.Public.Thumbprint
    $noPublicOutput = -not $c.CaExists -and -not $c.ReceiptExists
    $noRemoval = $storeRemovals.Count -eq 0 -and $fileRemovals.Count -eq 0 -and
    [CertKeyStandInWorld]::State.Store.ContainsKey($c.Public.Thumbprint)
    Assert-True -Name ($prefix + 'created thumbprint stays recorded; no public CA or receipt; nothing removed') `
        -Condition ($thumbprintKept -and $noPublicOutput -and $noRemoval) `
        -Detail ('thumbprint=' + $thumbprintKept + ' noOutput=' + $noPublicOutput + ' noRemoval=' + $noRemoval)
}

# ---- 6b. The Assert function for every key case (the decision Verify, Instance and Remove reuse).
$asserted = @{}
foreach ($case in $keyCases) {
    $keyFile = if ($case.ContainsKey('KeyFile')) { $case.KeyFile } else { 'Present' }
    # A path that leaves MachineKeys points at a decoy that already grants the service SID read access.
    $asserted[$case.Id] = Invoke-AssertCase -Id $case.Id -Key $case.Key -KeyFile $keyFile -ServiceAcl 'Read'
    $a = $asserted[$case.Id]
    $prefix = 'CERTKEY-ASSERT ' + $case.Id + ': '
    if ($case.Class -ceq 'Supported') {
        Assert-True -Name ($prefix + 'supported key passes the certificate gate') -Condition ($null -eq $a.Code) `
            -Detail ([string]$a.Code)
        continue
    }
    $codePublic = [string]$a.Code -cmatch $script:PublicCodePattern
    Assert-True -Name ($prefix + 'rejected with a specific public error code') -Condition $codePublic `
        -Detail ([string]$a.Code)
}

# ---- 6c. Meaning of the codes and one decision for creation and verification.
$accessCodes = [Collections.Generic.HashSet[string]]::new()
$unsupportedCodes = [Collections.Generic.HashSet[string]]::new()
foreach ($case in $keyCases) {
    $codes = @([string]$created[$case.Id].Result.Code, [string]$asserted[$case.Id].Code)
    if ($case.Class -ceq 'Access') { foreach ($code in $codes) { [void]$accessCodes.Add($code) } }
    if ($case.Class -ceq 'Unsupported') { foreach ($code in $codes) { [void]$unsupportedCodes.Add($code) } }
}
$sharedCodes = @($accessCodes | Where-Object { $unsupportedCodes.Contains($_) })
$allSpecific = @(@($accessCodes) + @($unsupportedCodes) | Where-Object {
        $_ -cnotmatch $script:PublicCodePattern -or $_ -cin $script:FallbackCodes
    }).Count -eq 0
$bothClassesSeen = $accessCodes.Count -gt 0 -and $unsupportedCodes.Count -gt 0
Assert-True -Name 'CERTKEY-MEANING-01 access failures and unsupported keys never share a public error code' `
    -Condition ($sharedCodes.Count -eq 0 -and $bothClassesSeen -and $allSpecific) `
    -Detail ('access=' + (@($accessCodes) -join ',') + ' unsupported=' + (@($unsupportedCodes) -join ','))
foreach ($case in @($keyCases | Where-Object { -not $_.ContainsKey('Path') })) {
    $createCode = [string]$created[$case.Id].Result.Code
    $assertCode = [string]$asserted[$case.Id].Code
    Assert-True -Name ('CERTKEY-SAME-01 ' + $case.Id + ': create and Assert reach the same key decision') `
        -Condition ($createCode -ceq $assertCode) -Detail ('create=' + $createCode + ' assert=' + $assertCode)
}

# ---- 6d. Existing verification conditions keep their codes for both supported wrappers.
$preservationCases = @(
    @{ Id = 'receipt-key-file-changed'; Code = 'WINDOWS_SQL_CERTIFICATE_KEY_CHANGED'; ReceiptKeyFile = 'OTHER' },
    @{ Id = 'service-sid-changed'; Code = 'WINDOWS_SQL_SERVICE_CHANGED'; ReceiptServiceSid = 'S-1-5-80-0' },
    @{ Id = 'service-acl-missing'; Code = 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_INVALID'; ServiceAcl = 'None' },
    @{ Id = 'service-acl-deny'; Code = 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_INVALID'; ServiceAcl = 'Deny' },
    @{ Id = 'service-acl-read-attributes-only'; Code = 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_INVALID'
        ServiceAcl = 'ReadAttributes' },
    @{ Id = 'public-ca-changed'; Code = 'WINDOWS_SQL_PUBLIC_CA_CHANGED'; CaMismatch = $true }
)
foreach ($wrapper in @('Csp', 'Cng')) {
    foreach ($case in $preservationCases) {
        $id = $wrapper.ToLowerInvariant() + '-' + $case.Id
        $arguments = @{ Id = $id; Key = @{ Wrapper = $wrapper } }
        if ($case.ContainsKey('ServiceAcl')) { $arguments.ServiceAcl = $case.ServiceAcl }
        if ($case.ContainsKey('ReceiptServiceSid')) { $arguments.ReceiptServiceSid = $case.ReceiptServiceSid }
        if ($case.ContainsKey('CaMismatch')) { $arguments.CaMismatch = $true }
        if ($case.ContainsKey('ReceiptKeyFile')) { $arguments.ReceiptKeyFile = $case.ReceiptKeyFile }
        $result = Invoke-AssertCase @arguments
        Assert-True -Name ('CERTKEY-KEEP-01 ' + $id + ': Assert keeps ' + $case.Code) `
            -Condition ([string]$result.Code -ceq $case.Code) -Detail ([string]$result.Code)
    }
    $id = $wrapper.ToLowerInvariant() + '-key-acl-unreadable'
    $result = Invoke-AssertCase -Id $id -Key @{ Wrapper = $wrapper } -AclReadDenied
    $readFailure = [string]$result.Code -cmatch $script:PublicCodePattern -and
    -not $unsupportedCodes.Contains([string]$result.Code)
    Assert-True -Name ('CERTKEY-KEEP-02 ' + $id + ': unreadable key ACL is a public failure, not an unsupported key') `
        -Condition $readFailure -Detail ([string]$result.Code)
}

# ---- 6e. Creation contract, executor gate and failure after the key ACL write.
$supportedCreation = @([CertKeyStandInWorld]::State.Creations | Where-Object Run -CEQ 'create:csp-supported')
$asked = $supportedCreation | Select-Object -First 1
$creationContract = $supportedCreation.Count -eq 1 -and $asked.Provider -ceq $script:SChannelProvider -and
$asked.KeySpec -ceq 'KeyExchange' -and $asked.KeyExportPolicy -ceq 'NonExportable' -and
$asked.KeyLength -ceq '2048' -and $asked.KeyAlgorithm -ceq 'RSA' -and
$asked.CertStoreLocation -ceq 'Cert:\LocalMachine\My' -and
$asked.Type -ceq 'SSLServerAuthentication' -and $asked.HashAlgorithm -ceq 'SHA256' -and
$asked.KeyUsage -ceq 'DigitalSignature,KeyEncipherment' -and
$asked.DnsName -ceq ($script:FixtureMachine + ',' + $script:TlsServerName)
Assert-True -Name 'CERTKEY-KEEP-03 creation still asks for an SChannel CSP, KeyExchange, non-exportable RSA 2048 key' `
    -Condition $creationContract -Detail ($supportedCreation | ConvertTo-Json -Compress)

$gate = Invoke-CreateCase -Id 'executor-not-administrator' -Key @{ Wrapper = 'Cng' } -Administrator $false
$gateEvents = @(Get-RunEvents -Run $gate.Run)
$gateHeld = $gate.Result.Exit -eq 1 -and $gate.Result.Code -ceq 'WINDOWS_SQL_EXECUTOR_MISMATCH' -and
@($gateEvents | Where-Object Kind -CIn @('CertificateCreate', 'ImportModule')).Count -eq 0 -and -not $gate.PrivateExists
Assert-True -Name 'CERTKEY-KEEP-04 a non-administrator executor stops before PKI, the private folder and creation' `
    -Condition $gateHeld -Detail ('exit=' + $gate.Result.Exit + ' code=' + $gate.Result.Code)

foreach ($wrapper in @('Csp', 'Cng')) {
    $id = $wrapper.ToLowerInvariant() + '-fails-after-key-acl'
    $partial = Invoke-CreateCase -Id $id -Key @{ Wrapper = $wrapper } -OccupyPublicCaAfterKeyAcl
    $events = @(Get-RunEvents -Run $partial.Run)
    $keyAclWrites = @($events | Where-Object Kind -CEQ 'KeyAclWrite' | ForEach-Object Detail)
    # The hook occupies the public CA path only after the key ACL write, so the CA text proves which step failed.
    $caText = $null
    if ([IO.File]::Exists($partial.CaPath)) { $caText = [IO.File]::ReadAllText($partial.CaPath) }
    $reachedAclWrite = $keyAclWrites.Count -eq 1 -and $keyAclWrites[0] -ieq $partial.Area.Target -and
    $caText -ceq 'occupied after the key ACL write'
    $failedKept = $partial.Result.Exit -eq 1 -and $partial.CreatedThumbprint -ceq $partial.Public.Thumbprint -and
    -not $partial.ReceiptExists
    $nothingRemoved = @($events | Where-Object Kind -CIn @('StoreRemove', 'FileRemove')).Count -eq 0 -and
    [CertKeyStandInWorld]::State.Store.ContainsKey($partial.Public.Thumbprint)
    Assert-True -Name ('CERTKEY-PARTIAL-01 ' + $id + ': exact key ACL written, then the public CA step failed') `
        -Condition $reachedAclWrite `
        -Detail ('writes=' + ($keyAclWrites -join ',') + ' code=' + $partial.Result.Code)
    $name = 'CERTKEY-PARTIAL-02 ' + $id + ': failure keeps the thumbprint, writes no receipt, removes nothing'
    Assert-True -Name $name -Condition ($failedKept -and $nothingRemoved) `
        -Detail ('exit=' + $partial.Result.Exit + ' code=' + $partial.Result.Code + ' kept=' + $failedKept)
}

# ---- 6f. The remove entry: the same decision before the journal and the certificate store removal.
$script:RemoveTree = New-RemoveTree
Assert-True -Name 'CERTKEY-REMOVE-00 remove tree is a byte-identical copy of the product inputs' `
    -Condition ($script:RemoveTree.Mismatch.Count -eq 0) -Detail ($script:RemoveTree.Mismatch -join ',')
[CertKeyStandInRegistry]::Values = @{
    '\MSSQLServer' = @{ LoginMode = @{ Value = [int]1; Kind = 'DWord' } }
    '\MSSQLServer\SuperSocketNetLib' = @{
        ForceEncryption = @{ Value = [int]0; Kind = 'DWord' }
        Certificate = @{ Value = ''; Kind = 'String' }
    }
    '\MSSQLServer\SuperSocketNetLib\Tcp' = @{
        Enabled = @{ Value = [int]0; Kind = 'DWord' }
        ListenOnAllIPs = @{ Value = [int]1; Kind = 'DWord' }
    }
    '\MSSQLServer\SuperSocketNetLib\Tcp\IPAll' = @{
        TcpPort = @{ Value = ''; Kind = 'String' }
        TcpDynamicPorts = @{ Value = '0'; Kind = 'String' }
    }
}
$removeCases = @(
    @{ Id = 'csp-supported'; Class = 'Supported'; Key = @{ Wrapper = 'Csp' } },
    @{ Id = 'cng-supported'; Class = 'Supported'; Key = @{ Wrapper = 'Cng' } },
    @{ Id = 'keyset-missing'; Class = 'Access'; Key = @{ Wrapper = 'Cng'; Acquire = 'Keyset' } },
    @{ Id = 'cng-other-provider'; Class = 'Unsupported'; Key = @{ Wrapper = 'Cng'; Provider = $script:KspProvider } },
    @{ Id = 'cng-service-acl-missing'; Class = 'Kept'; Key = @{ Wrapper = 'Cng' }; ServiceAcl = 'None'
        Code = 'WINDOWS_SQL_CERTIFICATE_KEY_ACL_INVALID' }
)
foreach ($case in $removeCases) {
    $serviceAcl = if ($case.ContainsKey('ServiceAcl')) { $case.ServiceAcl } else { 'Read' }
    $r = Invoke-RemoveCase -Id $case.Id -Key $case.Key -ServiceAcl $serviceAcl
    $removals = @(Get-RunEvents -Run $r.Run -Kind 'StoreRemove' | ForEach-Object Detail)
    $prefix = 'CERTKEY-REMOVE ' + $case.Id + ': '
    if ($case.Class -ceq 'Supported') {
        $exactRemoval = 'Cert:\LocalMachine\My\' + $r.Public.Thumbprint + '|DeleteKey=True'
        $removedExact = $r.Result.Exit -eq 0 -and $r.Result.Stdout -ceq '{"Status":"PreparedCertificateRemoved"}' -and
        $removals.Count -eq 1 -and $removals[0] -ceq $exactRemoval -and
        $null -ne $r.Journal -and $r.Journal.Status -ceq 'Removed'
        Assert-True -Name ($prefix + 'supported key removes exactly the prepared certificate with its key') `
            -Condition $removedExact -Detail ('exit=' + $r.Result.Exit + ' code=' + $r.Result.Code + ' removals=' +
            ($removals -join ','))
        continue
    }
    $expected = if ($case.ContainsKey('Code')) { $case.Code } else { [string]$asserted[$case.Id].Code }
    $refused = $r.Result.Exit -eq 1 -and $removals.Count -eq 0 -and $null -eq $r.Journal
    Assert-True -Name ($prefix + 'refused before the cleanup journal and the store removal') -Condition $refused `
        -Detail ('exit=' + $r.Result.Exit + ' code=' + $r.Result.Code)
    Assert-True -Name ($prefix + 'remove reports the same code as Assert') -Condition ($r.Result.Code -ceq $expected) `
        -Detail ('remove=' + $r.Result.Code + ' expected=' + $expected)
}

# =====================================================================================================
# 7. Object lifetime, key use and safety across every entry run.
# =====================================================================================================
$entryRuns = @([CertKeyStandInWorld]::State.Events | Where-Object { $_.Run -cne 'setup' -and $_.Run -cne 'ready' } |
        ForEach-Object Run | Select-Object -Unique)
$useAfterDispose = @([CertKeyStandInWorld]::State.Events | Where-Object Kind -CEQ 'UseAfterDispose' |
        ForEach-Object { $_.Run + ':' + $_.Detail })
Assert-True -Name 'CERTKEY-LIFE-01 no key, container info or certificate is used after it was disposed' `
    -Condition ($useAfterDispose.Count -eq 0) -Detail ($useAfterDispose -join ',')
$leaks = @(foreach ($run in $entryRuns) { foreach ($id in (Get-OwnedLeaks -Run $run)) { $run + '#' + $id } })
Assert-True -Name 'CERTKEY-LIFE-02 every key object the caller owns (GetRSAPrivateKey, reopened key) is disposed' `
    -Condition ($leaks.Count -eq 0) -Detail ($leaks -join ',')
$openCertificates = @(foreach ($run in $entryRuns) {
        foreach ($id in (Get-UndisposedCertificates -Run $run)) { $run + '#' + $id }
    })
Assert-True -Name 'CERTKEY-LIFE-03 every certificate object handed to the product is disposed' `
    -Condition ($openCertificates.Count -eq 0) -Detail ($openCertificates -join ',')
$keyUseKinds = @('KeyOperation', 'ContainerCreation', 'KeyCreation', 'WouldDeleteContainer')
$keyUse = @([CertKeyStandInWorld]::State.Events | Where-Object { $_.Run -cne 'ready' -and $_.Kind -cin $keyUseKinds } |
        ForEach-Object { $_.Run + ':' + $_.Kind + ':' + $_.Detail })
Assert-True -Name 'CERTKEY-SAFE-01 no private key export/use, key creation or container deletion was attempted' `
    -Condition ($keyUse.Count -eq 0) -Detail ($keyUse -join ',')
$outsideKinds = @('ForbiddenCommand', 'PathOutsideSuite', 'UnplannedCertificateCreation', 'UnrepresentedStore',
    'UnrepresentedFolder', 'UnrepresentedHive', 'RegistryWrite', 'UnrepresentedModule', 'UnrepresentedRole')
$outside = @([CertKeyStandInWorld]::State.Events | Where-Object Kind -CIn $outsideKinds |
        ForEach-Object { $_.Run + ':' + $_.Kind + ':' + $_.Detail })
Assert-True -Name 'CERTKEY-SAFE-02 no real command, outside path, store, registry write or module was reached' `
    -Condition ($outside.Count -eq 0) -Detail ($outside -join ',')
$privateText = @($script:EntryOutputs | Where-Object { $_ -match 'PRIVATE KEY' }).Count
Assert-True -Name 'CERTKEY-SAFE-03 no entry output carries private key text' -Condition ($privateText -eq 0)
# A compiled product native class would be a real type of that name in some loaded assembly.
$compiledNative = @([AppDomain]::CurrentDomain.GetAssemblies() |
        ForEach-Object { $_.GetType('Dawnholder.WindowsSql.CertificateKeyNative', $false) } |
        Where-Object { $null -ne $_ })
$addTypeReached = @([CertKeyStandInWorld]::State.Events | Where-Object {
        $_.Run -cne 'ready' -and $_.Kind -ceq 'RefusedNativeCompile'
    }).Count
Assert-True -Name 'CERTKEY-SAFE-04 no product native class was compiled or loaded; its Add-Type was never reached' `
    -Condition ($compiledNative.Count -eq 0 -and $addTypeReached -eq 0) `
    -Detail ('compiled=' + $compiledNative.Count + ' addType=' + $addTypeReached)

# Which key surfaces the product used: evidence for the independent verifier, not a requirement.
$surfaceKinds = @('KeyRequest', 'CapiOpen', 'CngOpen', 'PropertyRead', 'UnrepresentedProperty',
    'UnrepresentedMember', 'MetadataFault')
$surfaces = @([CertKeyStandInWorld]::State.Events |
        Where-Object { $_.Run -cne 'ready' -and $_.Kind -cin $surfaceKinds } |
        ForEach-Object { $_.Kind + ':' + ($_.Detail -replace '\|.*$', '') } | Sort-Object -Unique)
Add-TestResult -Name 'CERTKEY-OBS-01 key surfaces read by the product (stand-in calls)' -Outcome OBSERVED `
    -Detail ($surfaces -join ', ')
Add-TestResult -Name 'CERTKEY-OBS-02 codes observed per class' -Outcome OBSERVED `
    -Detail ('access=' + (@($accessCodes) -join ',') + '; unsupported=' + (@($unsupportedCodes) -join ','))
$eventsPath = Join-Path $script:SuiteRoot 'stand-in-events.json'
$eventsJson = ConvertTo-Json -InputObject @([CertKeyStandInWorld]::State.Events.ToArray()) -Depth 4
[IO.File]::WriteAllText($eventsPath, $eventsJson + "`n", [Text.UTF8Encoding]::new($false))
$creationsPath = Join-Path $script:SuiteRoot 'certificate-creations.json'
$creationsJson = ConvertTo-Json -InputObject @([CertKeyStandInWorld]::State.Creations.ToArray()) -Depth 4
[IO.File]::WriteAllText($creationsPath, $creationsJson + "`n", [Text.UTF8Encoding]::new($false))

Complete-TestSuite
