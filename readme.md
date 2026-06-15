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

The dashboard needs a Dapr state store component. Describe it as a `YamlSourceResource` and pass it to `AddDiagridDashboard`:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "local");
var password = builder.AddParameter("password", "zxczxc123", secret: true);

var postgres = builder.AddPostgres("postgres", username, password);
var daprState = postgres.AddDatabase("dapr-state", "dapr_state");

var stateComponent = builder.AddYamlFile("dashboard-state", new
{
    apiVersion = "dapr.io/v1alpha1",
    kind = "Component",
    metadata = new { name = "state" },
    spec = new
    {
        type = "state.postgresql",
        version = "v1",
        metadata = new object[]
        {
            new { name = "connectionString", value = ReferenceExpression.Create($"host={postgres.Resource.Name} user={username.Resource} password={password.Resource} port={postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.TargetPort)} dbname={daprState.Resource.DatabaseName} sslmode=disable") },
            new { name = "actorStateStore", value = "true" },
        },
    },
});

builder.AddDiagridDashboard(stateComponent)
    .WaitFor(daprState);

builder.Build().Run();
```

This runs the [Diagrid Dashboard](https://github.com/diagridio/diagrid-dashboard) container alongside the rest of your Aspire resources, automatically wired into the dashboard so you can open it directly from the Aspire UI. The state component YAML is materialized at startup and bind-mounted into the container, so nothing needs to be checked in or copied to the build output.

For the full list of supported component types, see the [Dapr components reference](https://docs.dapr.io/reference/components-reference/).

## 3 - Configuring the Dashboard

By default, the integration runs the `latest` image and lets Aspire allocate an ephemeral host port for the dashboard's HTTP endpoint.

To override any of those, pass a `DiagridDashboardConfiguration`:

```csharp
builder.AddDiagridDashboard(stateComponent, configuration: new DiagridDashboardConfiguration
{
    // AppId = "diagrid-dashboard",         // Optional, Dapr APP_ID inside the container
    // ContainerName = "diagrid-dashboard", // Optional
    // Version = "latest",                  // Optional, image tag
    // Port = 8080,                         // Optional, fixed host port
});
```

### Custom Naming

If you want to run more than one dashboard instance, or you'd just prefer a different resource name, pass a name:

```csharp
builder.AddDiagridDashboard(stateComponent, name: "my-dashboard");
```

The returned `IResourceBuilder<ContainerResource>` can be chained with the usual Aspire builder extensions if you need to further customize the container.

## Additional Resources

See the [Diagrid Dashboard](https://docs.diagrid.io/develop/local-development/dev-dashboard/) page in the Diagrid docs for more on the dashboard itself.
