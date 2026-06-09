using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using CopperDusk.Aspire.Hosting.Yaml;

namespace Diagrid.Aspire.Hosting.Dashboard;

public static class ResourceBuilderExtensions
{
    private const string ContainerImage = "ghcr.io/diagridio/diagrid-dashboard";

    public static IResourceBuilder<ContainerResource> AddDiagridDashboard(
        this IDistributedApplicationBuilder applicationBuilder,
        IResourceBuilder<YamlSourceResource> stateComponent,
        string name = "diagrid-dashboard",
        DiagridDashboardConfiguration? configuration = null
    )
    {
        configuration ??= new();
        
        var componentsGroup = applicationBuilder.AddYamlFileGroup($"{name}-components", [ stateComponent ]);

        // note: Override component configuration so that we now find the prepared YAML.
        configuration = configuration with
        {
            ComponentsPath = componentsGroup.Resource.ContainerPath,
            ComponentFile = stateComponent.Resource.FileName,
        };
        
        return applicationBuilder.AddDiagridDashboard(name, configuration);
    }
    
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
            .WithBindMount(configuration.ComponentsPath, DiagridDashboardConfiguration.DefaultContainerComponentsPath)
            .WithEnvironment("COMPONENT_FILE", $"{DiagridDashboardConfiguration.DefaultContainerComponentsPath}/{configuration.ComponentFile}")
            .WithEnvironment("APP_ID", configuration.AppId)
        ;

        if (configuration.Port.HasValue)
            diagridDashboard.WithHttpEndpoint(configuration.Port, 8080);
        else
            diagridDashboard.WithHttpEndpoint(targetPort: 8080);

        return diagridDashboard;
    }
}
