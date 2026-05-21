using System.IO;
using System.Reflection;

namespace Diagrid.Aspire.Hosting.Dashboard;

public record DiagridDashboardConfiguration
{
    public const string DefaultContainerComponentsPath = "/app/components";
    
    private const string DefaultName = "diagrid-dashboard";
    
    private static readonly string ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new("Unable to determine executing path!");
    
    /// <summary>
    ///     Dapr application id passed to the dashboard container as the <c>APP_ID</c> environment variable.
    ///     <br /><br />
    ///     Identifies this dashboard instance within the Dapr runtime.
    /// </summary>
    public string AppId { get; init; } = DefaultName;

    /// <summary>
    ///     Name assigned to the underlying Docker container via <c>WithContainerName</c>.
    ///     <br /><br />
    ///     Use this when you need a predictable container name for tooling or external references.
    /// </summary>
    public string ContainerName { get; init; } = DefaultName;

    /// <summary>
    ///     Tag of the <c>ghcr.io/diagridio/diagrid-dashboard</c> image to pull. Defaults to <c>latest</c>;
    ///     <br /><br />
    ///     pin to a specific version for reproducible builds.
    /// </summary>
    public string Version { get; init; } = "latest";

    /// <summary>
    ///     Absolute host path bind-mounted into the container at <c>/app/components</c>.
    ///     <br /><br />
    ///     Contains the Dapr component YAML files the dashboard loads at startup. Defaults to the
    ///     <c>Resources/dapr/diagrid-dashboard-components</c> folder shipped alongside this assembly.
    /// </summary>
    public string ComponentsPath { get; init; } = Path.Join(ExecutingPath, "Resources", "dapr", $"{DefaultName}-components");

    /// <summary>
    ///     Filename (relative to <see cref="ComponentsPath"/>) of the component definition the dashboard should use,
    ///     exposed to the container via the <c>COMPONENT_FILE</c> environment variable.
    /// </summary>
    public string ComponentFile { get; init; } = $"{DefaultName}-state.yaml";

    /// <summary>
    ///     Optional fixed host port to bind to the container's HTTP endpoint (target port 8080).
    ///     <br /><br />
    ///     Leave <c>null</c> to let Aspire allocate an ephemeral port.
    /// </summary>
    public int? Port { get; init; }
}
