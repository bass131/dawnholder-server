using System.Formats.Tar;
using System.Text.Json;

namespace Dawnholder.Management.Backend;

internal sealed class ReleaseStore
{
    private readonly BackendSettings _settings;
    private readonly string _root;
    private readonly SemaphoreSlim _commands = new(1, 1);
    private readonly object _state = new();
    private readonly CancellationToken _stopping;
    private ReleaseIdentity? _current;

    public ReleaseStore(BackendSettings settings, CancellationToken stopping)
    {
        _settings = settings;
        _stopping = stopping;
        _root = Path.Combine(settings.DataDirectory, "releases");
        Directory.CreateDirectory(_root);
        string currentFile = Path.Combine(_root, "current.json");
        if (File.Exists(currentFile))
        {
            ReleaseIdentity saved = JsonFiles.Read<ReleaseIdentity>(currentFile);
            ValidateCommit(saved.Commit);
            _current = Identity(Find(saved.Commit)
                ?? throw new IOException("현재 운영 실행본의 manifest가 없습니다."));
        }
    }

    public ReleaseIdentity? Current
    {
        get { lock (_state) return _current; }
    }

    public static string ValidateCommit(string? commit)
    {
        if (commit == null || commit.Length != 40 || commit.Any(ch => ch is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
            throw new ApiFailure(400, "invalidCommit", "commit은 소문자 16진수 40자여야 합니다.");
        return commit;
    }

    public string DirectoryFor(string commit) => Path.Combine(_root, ValidateCommit(commit));

    public object List()
    {
        List<ReleaseManifest> manifests = [];
        foreach (string directory in Directory.EnumerateDirectories(_root))
        {
            string name = Path.GetFileName(directory);
            if (name.Length != 40 || name.Any(ch => ch is not (>= '0' and <= '9' or >= 'a' and <= 'f'))) continue;
            ReleaseManifest? manifest = Find(name);
            if (manifest != null) manifests.Add(manifest);
        }

        return new { releases = manifests.OrderByDescending(item => item.BuiltAt), currentRelease = Current };
    }

    public async Task<ReleaseManifest> BuildAsync(string commit, CancellationToken requestAborted)
    {
        ValidateCommit(commit);
        if (!await _commands.WaitAsync(0)) throw new ApiFailure(409, "busy", "다른 실행본 작업을 처리 중입니다.");
        try
        {
            if (string.IsNullOrEmpty(_settings.Release.SourceRepository))
                throw new ApiFailure(409, "releaseSourceNotConfigured", "실행본 원천 저장소가 설정되지 않았습니다.");
            ReleaseManifest? existing = Find(commit);
            if (existing != null) return existing;
            return await BuildCoreAsync(commit, requestAborted);
        }
        finally
        {
            _commands.Release();
        }
    }

    public async Task<ReleaseIdentity> SelectAsync(string commit)
    {
        ValidateCommit(commit);
        if (!await _commands.WaitAsync(0)) throw new ApiFailure(409, "busy", "다른 실행본 작업을 처리 중입니다.");
        try
        {
            _stopping.ThrowIfCancellationRequested();
            ReleaseManifest manifest = Find(commit)
                ?? throw new ApiFailure(404, "releaseNotFound", "빌드한 실행본을 찾을 수 없습니다.");
            ReleaseIdentity selected = Identity(manifest);
            lock (_state)
            {
                JsonFiles.Write(Path.Combine(_root, "current.json"), selected);
                _current = selected;
            }

            return selected;
        }
        finally
        {
            _commands.Release();
        }
    }

    public async Task WaitForIdleAsync()
    {
        await _commands.WaitAsync();
        _commands.Release();
    }

    private static ReleaseIdentity Identity(ReleaseManifest manifest) => new(manifest.Commit, manifest.BuiltAt);

    private ReleaseManifest? Find(string commit)
    {
        string path = Path.Combine(DirectoryFor(commit), "manifest.json");
        return File.Exists(path) ? JsonFiles.Read<ReleaseManifest>(path) : null;
    }

    private async Task<ReleaseManifest> BuildCoreAsync(string commit, CancellationToken requestAborted)
    {
        string source = _settings.Release.SourceRepository!;
        string work = Path.Combine(_settings.DataDirectory, "build-work", Guid.NewGuid().ToString("N"));
        string partial = DirectoryFor(commit) + ".partial";
        using CancellationTokenSource cancel = CancellationTokenSource.CreateLinkedTokenSource(requestAborted, _stopping);
        cancel.CancelAfter(TimeSpan.FromSeconds(_settings.Release.BuildTimeoutSeconds));
        using StreamWriter log = new(Path.Combine(_root, commit + ".build.log"), append: false);
        object logGate = new();
        void Append(string text)
        {
            lock (logGate)
            {
                log.Write(text);
                log.Flush();
            }
        }

        string[] gitPrefix = ["-c", $"safe.directory={source}", "-C", source];
        try
        {
            CommandResult exists = await ChildCommand.RunAsync("git",
                [.. gitPrefix, "cat-file", "-e", commit + "^{commit}"], _root, cancel.Token, Append);
            if (exists.ExitCode != 0) throw new ApiFailure(404, "commitNotFound", "원천 저장소에 해당 commit이 없습니다.");
            Directory.CreateDirectory(work);
            string archive = Path.Combine(work, "source.tar");
            CommandResult archived = await ChildCommand.RunAsync("git",
                [.. gitPrefix, "archive", "--format=tar", "-o", archive, commit, "--", .. _settings.Release.ArchivePaths],
                work, cancel.Token, Append);
            if (archived.ExitCode != 0) throw new IOException("원천 archive 실패");
            TarFile.ExtractToDirectory(archive, work, overwriteFiles: false);
            File.Delete(archive);

            // Build inside the archive: Shared's Unity DLL copy target stays inside this disposable tree.
            CommandResult built = await ChildCommand.RunAsync(_settings.Server.DotnetPath!,
                ["publish", _settings.Release.ProjectPath, "-c", "Release", "-o", partial, "--disable-build-servers"],
                work, cancel.Token, Append);
            if (built.ExitCode != 0 || !File.Exists(Path.Combine(partial, _settings.Release.EntryAssembly)))
                throw new IOException("실행본 publish 실패 또는 진입 어셈블리 부재");
            CommandResult sdk = await ChildCommand.RunAsync(_settings.Server.DotnetPath!, ["--version"], work, cancel.Token, Append);
            if (sdk.ExitCode != 0) throw new IOException("SDK 버전 확인 실패");
            ReleaseManifest manifest = new(commit, DateTime.UtcNow, sdk.Output.Trim(), source);
            JsonFiles.Write(Path.Combine(partial, "manifest.json"), manifest);
            cancel.Token.ThrowIfCancellationRequested();
            Directory.Move(partial, DirectoryFor(commit));
            return manifest;
        }
        catch (Exception error) when (error is not ApiFailure)
        {
            Append($"\n빌드 실패: {error.Message}\n");
            throw new ApiFailure(500, "buildFailed", "실행본 빌드에 실패했거나 시간 상한을 넘었습니다. 빌드 로그를 확인하세요.");
        }
        finally
        {
            if (Directory.Exists(partial)) Directory.Delete(partial, recursive: true);
            if (Directory.Exists(work)) Directory.Delete(work, recursive: true);
        }
    }
}

internal sealed record ReleaseManifest(string Commit, DateTime BuiltAt, string SdkVersion, string SourceRepository);
internal sealed record ReleaseIdentity(string Commit, DateTime BuiltAt);
