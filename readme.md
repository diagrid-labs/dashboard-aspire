# Diagrid Dashboard Aspire Integration

![NuGet Version](https://img.shields.io/nuget/v/Diagrid.Aspire.Hosting.Dashboard)


## Getting Started

### 1 - Add the integration to your AppHost project

You can find [the package on NuGet](https://www.nuget.org/packages/Diagrid.Aspire.Hosting.Dashboard).

Add the package by running:

```bash
dotnet add package Diagrid.Aspire.Hosting.Dashboard
```

### 2 - Configure your AppHost

At a minimum, you must always add the following to your project to enable the integration:

```csharp
builder.AddDiagridDashboard();
```

This will run the [Diagrid Dashboard](https://github.com/diagridio/diagrid-dashboard) container alongside the rest of your Aspire resources, automatically wired into the dashboard so you can open it directly from the Aspire UI.

## 3 - Configuring the Dashboard

By default, the integration runs the `latest` image, exposes the dashboard on port `8080`, and looks for component files in a `Resources/dapr/diagrid-dashboard-components` folder next to your AppHost output.

If you'd like to override any of those defaults, pass a `DiagridDashboardConfiguration`:

```csharp
builder.AddDiagridDashboard(configuration: new DiagridDashboardConfiguration
{
    // AppId = "diagrid-dashboard", // Optional
    // ComponentsPath = "/path/to/your/components", // Optional, defaults to `Resources/dapr/diagrid-dashboard-components` in output dir
    // ComponentFile = "diagrid-dashboard-state.yaml", // Optional, defaults to `diagrid-dashboard-state.yaml` in ComponentsPath
    // Version = "latest", // Optional
    // Port = 8080, // Optional
});
```

## Components

The dashboard reads its state store configuration from the component file specified by `ComponentFile`, resolved relative to `ComponentsPath`. Drop any additional Dapr component YAML files into the same folder and they'll be picked up by the container at startup.

For the full list of supported component types, see the [Dapr components reference](https://docs.dapr.io/reference/components-reference/).

### Resources Directory

The default `ComponentsPath` resolves next to the AppHost's compiled output, so your component YAML files need to be copied there as part of the build. Add a `Content` item to your AppHost csproj so the `Resources` folder is mirrored into `bin/`:

```xml
<ItemGroup>
    <Content Include="Resources\**\*.*">
        <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
        <Link>Resources\%(RecursiveDir)%(Filename)%(Extension)</Link>
    </Content>
</ItemGroup>
```

Then drop your component file at `Resources/dapr/diagrid-dashboard-components/diagrid-dashboard-state.yaml`. A minimal Postgres-backed state store looks like:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: state
spec:
  type: state.postgresql
  version: v1
  metadata:
    - name: connectionString
      value: "host=postgres user=local password=zxczxc123 dbname=dapr_state port=5432 sslmode=disable"
    - name: actorStateStore
      value: "true"
```

You can see this in action in the `AppHost` project in the solution, which wires up the dashboard against a Postgres state store with no further configuration:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddDiagridDashboard();

builder.Build().Run();
```

### Custom Naming

If you want to run more than one dashboard instance, or you'd just prefer a different resource name, pass a name as the first argument:

```csharp
builder.AddDiagridDashboard("my-dashboard");
```

The returned `IResourceBuilder<ContainerResource>` can be chained with the usual Aspire builder extensions if you need to further customize the container.

## Additional Resources

See the [Diagrid Dashboard repository](https://github.com/diagridio/diagrid-dashboard) for more on the dashboard itself.
