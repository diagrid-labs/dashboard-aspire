using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CopperDusk.Aspire.Hosting.Yaml;

namespace Diagrid.Aspire.Hosting.Dashboard;

public static class ResourceBuilderExtensions
{
    /// <summary>
    ///     Adds the Diagrid Dashboard, sourcing its Dapr component from the supplied <paramref name="stateComponent"/>.
    ///     <br /><br />
    ///     The component is materialized into a file group and the launch mode is asked for the perspective-correct
    ///     copy to read, so the dashboard sees connection strings that resolve from wherever it actually runs.
    /// </summary>
    /// <param name="applicationBuilder">The distributed application being built.</param>
    /// <param name="stateComponent">The Dapr state component the dashboard should load.</param>
    /// <param name="name">Resource name for the dashboard.</param>
    /// <param name="configuration">Shared, launch-mode-agnostic options.</param>
    /// <param name="launchMode">How to run the dashboard. Defaults to <see cref="ContainerLaunchMode"/>.</param>
    public static IResourceBuilder<IDiagridDashboardResource> AddDiagridDashboard(
        this IDistributedApplicationBuilder applicationBuilder,
        IResourceBuilder<YamlSourceResource> stateComponent,
        string name = DiagridDashboardConfiguration.DefaultName,
        DiagridDashboardConfiguration? configuration = null,
        DiagridDashboardLaunchMode? launchMode = null
    )
    {
        configuration ??= new();
        launchMode ??= new ContainerLaunchMode();

        var componentsGroup = applicationBuilder.AddYamlFileGroup($"{name}-components", [ stateComponent ]);

        // note: Override component configuration so that we now find the prepared YAML, from the launch mode's perspective.
        configuration = configuration with
        {
            ComponentsPath = launchMode.ResolveComponentsPath(componentsGroup),
            ComponentFile = stateComponent.Resource.FileName,
        };

        return applicationBuilder.AddDiagridDashboard(name, configuration, launchMode);
    }

    /// <summary>
    ///     Adds the Diagrid Dashboard using a pre-resolved <paramref name="configuration"/>.
    /// </summary>
    /// <param name="applicationBuilder">The distributed application being built.</param>
    /// <param name="name">Resource name for the dashboard.</param>
    /// <param name="configuration">Shared, launch-mode-agnostic options.</param>
    /// <param name="launchMode">How to run the dashboard. Defaults to <see cref="ContainerLaunchMode"/>.</param>
    public static IResourceBuilder<IDiagridDashboardResource> AddDiagridDashboard(
        this IDistributedApplicationBuilder applicationBuilder,
        string name = DiagridDashboardConfiguration.DefaultName,
        DiagridDashboardConfiguration? configuration = null,
        DiagridDashboardLaunchMode? launchMode = null
    )
    {
        configuration ??= new();
        launchMode ??= new ContainerLaunchMode();

        // note: discovery is launch-mode agnostic — the same daprd instances are projected either way, and Aspire renders
        // each sidecar URL from the dashboard's own perspective (container or host), so this belongs on the shared path.
        return launchMode
            .Launch(applicationBuilder, name, configuration)
            .WithDiscoveredDaprApps(applicationBuilder);
    }
}
