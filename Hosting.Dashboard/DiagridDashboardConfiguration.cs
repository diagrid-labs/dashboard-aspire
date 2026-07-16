using System.IO;
using System.Reflection;

namespace Diagrid.Aspire.Hosting.Dashboard;

/// <summary>
///     Launch-mode agnostic configuration for the Diagrid Dashboard.
///     <br /><br />
///     Every <see cref="DiagridDashboardLaunchMode"/> is handed the same instance, so these are the options that
///     make sense regardless of whether the dashboard runs as a container or a local executable. Options that only
///     make sense for a specific way of launching (image tag, container name, executable path, ...) live on the
///     concrete launch mode instead.
/// </summary>
public record DiagridDashboardConfiguration
{
    internal const string DefaultName = "diagrid-dashboard";

    private static readonly string ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new("Unable to determine executing path!");

    /// <summary>
    ///     Dapr application id passed to the dashboard as the <c>APP_ID</c> environment variable.
    ///     <br /><br />
    ///     Identifies this dashboard instance within the Dapr runtime.
    /// </summary>
    public string AppId { get; init; } = DefaultName;

    /// <summary>
    ///     Absolute host path the dashboard reads its Dapr component YAML from.
    ///     <br /><br />
    ///     When the dashboard is wired up from a <c>YamlSourceResource</c> this is filled in automatically with the
    ///     perspective-correct path chosen by the launch mode (the container-perspective copy for a container, the
    ///     host-perspective copy for a local executable). Defaults to the <c>Resources/dapr/diagrid-dashboard-components</c>
    ///     folder shipped alongside this assembly.
    /// </summary>
    public string ComponentsPath { get; init; } = Path.Join(ExecutingPath, "Resources", "dapr", $"{DefaultName}-components");

    /// <summary>
    ///     Filename (relative to <see cref="ComponentsPath"/>) of the component definition the dashboard should use,
    ///     exposed via the <c>COMPONENT_FILE</c> environment variable.
    /// </summary>
    public string ComponentFile { get; init; } = $"{DefaultName}-state.yaml";

    /// <summary>
    ///     Optional fixed host port for the dashboard's HTTP endpoint.
    ///     <br /><br />
    ///     Leave <c>null</c> to let Aspire allocate an ephemeral port.
    /// </summary>
    public int? Port { get; init; }
}
