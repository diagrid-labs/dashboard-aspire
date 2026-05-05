using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Diagrid.Aspire.Hosting.Dashboard;

public static class ResourceBuilderExtensions
{
    private const string ContainerImage = "ghcr.io/diagridio/diagrid-dashboard";

    public static IResourceBuilder<ContainerResource> AddDiagridDashboard(
        this IDistributedApplicationBuilder applicationBuilder,
        string name = "diagrid-dashboard",
        DiagridDashboardConfiguration? configuration = null
    )
    {
        configuration ??= new()
        {
            ContainerName = name,
        };
        
        var diagridDashboard = applicationBuilder
            .AddContainer(name, $"{ContainerImage}:{configuration.Version}")
            .WithContainerName(configuration.ContainerName)
            .WithBindMount(configuration.ComponentsPath, "/app/components")
            .WithEnvironment("COMPONENT_FILE", $"/app/components/{configuration.ComponentFile}")
            .WithEnvironment("APP_ID", configuration.AppId)
        ;

        if (configuration.Port.HasValue)
            diagridDashboard.WithHttpEndpoint(configuration.Port, 8080);
        else
            diagridDashboard.WithHttpEndpoint(targetPort: 8080);

        return diagridDashboard;
    }
}
