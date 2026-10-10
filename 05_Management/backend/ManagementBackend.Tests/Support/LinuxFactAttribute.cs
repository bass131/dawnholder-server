namespace Dawnholder.Management.Backend.Tests.Support;

/// <summary>
/// The backend runs in WSL and the tests rely on Linux processes, signals, flock, /proc and file modes.
/// Same convention as 99_Tools/Formatting.Tests; that project is not referenced to keep the test projects independent.
/// </summary>
public sealed class LinuxFactAttribute : FactAttribute
{
    public LinuxFactAttribute()
    {
        if (!OperatingSystem.IsLinux())
        {
            Skip = "Requires Linux/WSL processes, signals, flock, /proc and file modes.";
        }
    }
}

/// <summary>Theory counterpart of <see cref="LinuxFactAttribute"/>.</summary>
public sealed class LinuxTheoryAttribute : TheoryAttribute
{
    public LinuxTheoryAttribute()
    {
        if (!OperatingSystem.IsLinux())
        {
            Skip = "Requires Linux/WSL processes, signals, flock, /proc and file modes.";
        }
    }
}
