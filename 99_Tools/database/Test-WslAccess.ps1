#requires -Version 7.0
# Optional native Linux sqlcmd probe, after the separately approved admin stage.
[CmdletBinding()]
param([ValidatePattern('^[A-Za-z0-9_-]+$')][string]$Distribution = 'Ubuntu')
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$stateDirectory = Join-Path $env:LOCALAPPDATA 'Dawnholder/MssqlWsl-SQLEXPRESS'
$state = Import-Clixml -LiteralPath (Join-Path $stateDirectory 'state.clixml')
if ($state.Restored -or $state.Database -ne 'Dawnholder_Dev' -or $state.Port -ne 14330) { throw 'Expected enabled local WSL configuration is absent.' }
$credential = Import-Clixml -LiteralPath (Join-Path $stateDirectory 'credential.clixml')
if ($credential.UserName -ne $state.Login -or $credential.UserName -notmatch '^Dawnholder_Dev_Wsl_[a-f0-9]{12}$') { throw 'Unexpected credential identity.' }
$sql = @'
SET NOCOUNT ON; SET XACT_ABORT ON;
IF IS_SRVROLEMEMBER('sysadmin')<>0 OR IS_MEMBER('db_owner')<>0
    THROW 51010, 'Runtime login must not be an administrator.', 1;
IF HAS_PERMS_BY_NAME('dh.Account','OBJECT','INSERT')<>1
 OR HAS_PERMS_BY_NAME('dh.Character','OBJECT','UPDATE')<>1
 OR HAS_PERMS_BY_NAME('dh.CharacterProgress','OBJECT','SELECT')<>1
 OR HAS_PERMS_BY_NAME('dh.CharacterProgress','OBJECT','DELETE')<>0
 OR HAS_PERMS_BY_NAME('dh.SchemaVersion','OBJECT','UPDATE')<>0
 OR HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER')<>0
    THROW 51011, 'Unexpected runtime permissions.', 1;
BEGIN TRANSACTION;
DECLARE @a uniqueidentifier=NEWID(), @c uniqueidentifier=NEWID(), @v binary(8);
INSERT dh.Account(AccountId) VALUES(@a);
INSERT dh.Character(CharacterId,AccountId,Class) VALUES(@c,@a,1);
INSERT dh.CharacterProgress(CharacterId,MapId,PositionX,PositionY,Hp,MaxHp) VALUES(@c,0,0,0,80,80);
SELECT @v=Version FROM dh.CharacterProgress WHERE CharacterId=@c;
UPDATE dh.CharacterProgress SET Hp=79,SavedUtc=SYSUTCDATETIME() WHERE CharacterId=@c AND Version=@v;
IF @@ROWCOUNT<>1 THROW 51012, 'Native WSL save failed.', 1;
UPDATE dh.CharacterProgress SET Hp=1 WHERE CharacterId=@c AND Version=@v;
IF @@ROWCOUNT<>0 THROW 51013, 'Native WSL stale save was accepted.', 1;
ROLLBACK;
IF EXISTS(SELECT 1 FROM dh.Account WHERE AccountId=@a) THROW 51014, 'Rollback failed.', 1;
SELECT 'PASS: native WSL SQL authentication, restricted permissions and rollback save' AS Result;
'@
# The password is stdin, never argv, terminal output, disk in Linux, or a repository file.
# The Linux child receives SQLCMDPASSWORD only for its own lifetime.
$bashScript = @'
set -eu
test -x /opt/mssql-tools18/bin/sqlcmd || { echo 'Native mssql-tools18 sqlcmd is not installed.' >&2; exit 2; }
IFS= read -r SQLCMDPASSWORD
export SQLCMDPASSWORD
exec /opt/mssql-tools18/bin/sqlcmd -S tcp:127.0.0.1,14330 -U '__LOGIN__' -d Dawnholder_Dev -C -b -l 5 -t 10 -W -Q "__SQL__"
'@
$bashScript = $bashScript.Replace('__LOGIN__',$credential.UserName).Replace('__SQL__',$sql).Replace("`r`n","`n")
$start = [Diagnostics.ProcessStartInfo]::new('wsl.exe')
$start.UseShellExecute = $false
$start.CreateNoWindow = $true
$start.RedirectStandardInput = $true
$start.RedirectStandardOutput = $true
$start.RedirectStandardError = $true
foreach ($argument in @('-d',$Distribution,'--','bash','-c',$bashScript)) { $start.ArgumentList.Add($argument) }
$process = [Diagnostics.Process]::new()
$process.StartInfo = $start
try {
    [void]$process.Start()
    $outputTask = $process.StandardOutput.ReadToEndAsync()
    $errorTask = $process.StandardError.ReadToEndAsync()
    # bash read strips LF only; Windows CRLF would append CR to the password.
    $process.StandardInput.NewLine = "`n"
    $process.StandardInput.WriteLine($credential.GetNetworkCredential().Password)
    $process.StandardInput.Close()
    if (-not $process.WaitForExit(30000)) { $process.Kill($true); throw 'WSL SQL probe timed out.' }
    Write-Output $outputTask.GetAwaiter().GetResult()
    if ($process.ExitCode -ne 0) { throw "WSL probe failed ($($process.ExitCode)): $($errorTask.GetAwaiter().GetResult())" }
} finally { $process.Dispose() }
