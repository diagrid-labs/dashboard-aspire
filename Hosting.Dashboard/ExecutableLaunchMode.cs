using System.IO;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CopperDusk.Aspire.Hosting.Yaml;

namespace Diagrid.Aspire.Hosting.Dashboard;

/// <summary>
///     Runs the Diagrid Dashboard as a local executable (a process on the host) instead of a container.
///     <br /><br />
///     Useful when you already have the dashboard binary available locally &mdash; for example when developing the
///     dashboard itself &mdash; and don't want a container runtime in the loop.
/// </summary>
/// <param name="Command">
///     Path to (or name resolvable on <c>PATH</c> of) the dashboard executable to run.
/// </param>
public sealed record ExecutableLaunchMode(string Command) : DiagridDashboardLaunchMode
{
    /// <summary>
    ///     Port the dashboard binary listens on when not told otherwise. Mirrors the container's fixed port so the
    ///     same binary behaves the same either way.
    /// </summary>
    private const int DefaultHttpPort = 8080;

    /// <summary>
    ///     Working directory the process is started in. Defaults to the AppHost's current directory.
    /// </summary>
    public string WorkingDirectory { get; init; } = ".";

    /// <summary>
    ///     Name of an environment variable to inject the resolved HTTP port into, for binaries that take their listen
    ///     port from the environment rather than defaulting to <see cref="DefaultHttpPort"/>. Leave <c>null</c> to not
    ///     inject anything and let the binary bind its own default.
    /// </summary>
    public string? PortEnvironmentVariable { get; init; }

    /// <inheritdoc />
    public string ResolveComponentsPath(IResourceBuilder<YamlFileGroupResource> components) =>
        // note: a local process reaches its dependencies from the host, so it wants the host-perspective copy.
        components.Resource.HostPath;

    /// <inheritdoc />
    public IResourceBuilder<IDiagridDashboardResource> Launch(
        IDistributedApplicationBuilder builder,
        string name,
        DiagridDashboardConfiguration configuration
    )
    {
        var workingDirectory = string.IsNullOrEmpty(WorkingDirectory) ? "." : WorkingDirectory;

        var dashboard = builder
            .AddResource(new DiagridDashboardExecutableResource(name, Command, workingDirectory))
            // note: no bind mount off the host, so COMPONENT_FILE points straight at the components on disk.
            .WithEnvironment("COMPONENT_FILE", Path.Combine(configuration.ComponentsPath, configuration.ComponentFile))
            .WithEnvironment("APP_ID", configuration.AppId)
        ;

        dashboard.WithHttpEndpoint(port: configuration.Port, targetPort: DefaultHttpPort, env: PortEnvironmentVariable);

        return dashboard;
    }
}
