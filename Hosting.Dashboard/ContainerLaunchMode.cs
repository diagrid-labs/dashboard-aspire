using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CopperDusk.Aspire.Hosting.Yaml;

namespace Diagrid.Aspire.Hosting.Dashboard;

/// <summary>
///     Runs the Diagrid Dashboard as a container pulled from a registry. This is the default launch mode.
/// </summary>
public sealed record ContainerLaunchMode : DiagridDashboardLaunchMode
{
    /// <summary>
    ///     The image the dashboard container runs from. Registry-qualified, without a tag.
    /// </summary>
    public const string DefaultImage = "ghcr.io/diagridio/dev-dashboard";

    /// <summary>
    ///     Path components are bind-mounted to inside the container. The dashboard reads its components from here.
    /// </summary>
    public const string DefaultContainerComponentsPath = "/app/components";

    /// <summary>
    ///     The port the dashboard listens on inside the container.
    /// </summary>
    private const int ContainerHttpPort = 8080;

    /// <summary>
    ///     Registry-qualified image (without tag) to pull.
    /// </summary>
    public string Image { get; init; } = DefaultImage;

    /// <summary>
    ///     Tag of <see cref="Image"/> to pull. Defaults to <c>latest</c>; pin it for reproducible builds.
    /// </summary>
    public string Version { get; init; } = "latest";

    /// <summary>
    ///     Explicit container name via <c>WithContainerName</c>. Defaults to the dashboard's resource name.
    ///     <br /><br />
    ///     Use this when you need a predictable container name for tooling or external references.
    /// </summary>
    public string? ContainerName { get; init; }

    /// <inheritdoc />
    public string ResolveComponentsPath(IResourceBuilder<YamlFileGroupResource> components) =>
        // note: a container reaches its dependencies by their in-cluster identities, so it wants the container-perspective copy.
        components.Resource.ContainerPath;

    /// <inheritdoc />
    public IResourceBuilder<IDiagridDashboardResource> Launch(
        IDistributedApplicationBuilder builder,
        string name,
        DiagridDashboardConfiguration configuration
    )
    {
        var dashboard = builder
            .AddResource(new DiagridDashboardContainerResource(name))
            .WithImage(Image, Version)
            .WithContainerName(ContainerName ?? name)
            .WithBindMount(configuration.ComponentsPath, DefaultContainerComponentsPath)
            .WithEnvironment("COMPONENT_FILE", $"{DefaultContainerComponentsPath}/{configuration.ComponentFile}")
            .WithEnvironment("DEVDASHBOARD_MODE", "aspire")
            // note: DEVDASHBOARD_APP_COUNT and the per-app block are owned by DaprSidecarDiscovery, so both launch modes stay out of it.
            .WithEnvironment("APP_ID", configuration.AppId)
        ;

        if (configuration.Port.HasValue)
            dashboard.WithHttpEndpoint(configuration.Port, ContainerHttpPort);
        else
            dashboard.WithHttpEndpoint(targetPort: ContainerHttpPort);

        return dashboard;
    }
}
