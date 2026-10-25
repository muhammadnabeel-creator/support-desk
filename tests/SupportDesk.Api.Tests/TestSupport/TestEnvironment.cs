using System.Runtime.CompilerServices;

namespace SupportDesk.Api.Tests.TestSupport;

/// <summary>Process-wide settings applied once, before any test runs.</summary>
internal static class TestEnvironment
{
    /// <summary>
    /// Every in-memory API host normally watches appsettings.json for changes. Many hosts running in parallel would
    /// exhaust Linux's inotify watcher limit (128 by default), so tests read configuration once and never reload it.
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize() =>
        Environment.SetEnvironmentVariable("DOTNET_hostBuilder__reloadConfigOnChange", "false");
}
