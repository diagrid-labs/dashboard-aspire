using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CopperDusk.Aspire.Hosting.Yaml;

namespace Diagrid.Aspire.Hosting.Dashboard;

/// <summary>
///     Describes <em>how</em> the Diagrid Dashboard is launched.
///     <br /><br />
///     <c>AddDiagridDashboard</c> owns the mode-agnostic wiring (materializing components, naming, waiting) and defers
///     the parts that actually differ between running a container and running a local process to an implementation of
///     this interface. Every implementation is handed the same <see cref="DiagridDashboardConfiguration"/>, so the two
///     ways of running the dashboard always present the same set of shared options.
///     <br /><br />
///     Implementations ship in the box: <see cref="ContainerLaunchMode"/> (the default) and <see cref="ExecutableLaunchMode"/>.
/// </summary>
public interface DiagridDashboardLaunchMode
{
    /// <summary>
    ///     Picks the copy of a materialized component group this mode should read from.
    ///     <br /><br />
    ///     The YAML group is rendered from two perspectives &mdash; connection strings and endpoints resolve differently
    ///     depending on whether the reader lives inside a container (<see cref="YamlFileGroupResource.ContainerPath"/>) or
    ///     runs as a local process on the host (<see cref="YamlFileGroupResource.HostPath"/>). Each mode returns the path
    ///     that matches the perspective it runs from.
    /// </summary>
    string ResolveComponentsPath(IResourceBuilder<YamlFileGroupResource> components);

    /// <summary>
    ///     Adds the dashboard resource to the application and applies the shared <paramref name="configuration"/>.
    /// </summary>
    /// <param name="builder">The distributed application being built.</param>
    /// <param name="name">Resource name for the dashboard within the app model.</param>
    /// <param name="configuration">The shared, launch-mode-agnostic options.</param>
    IResourceBuilder<IDiagridDashboardResource> Launch(
        IDistributedApplicationBuilder builder,
        string name,
        DiagridDashboardConfiguration configuration
    );
}
