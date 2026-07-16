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

`DiagridDashboardConfiguration` holds the options that apply no matter how the dashboard is launched:

```csharp
builder.AddDiagridDashboard(stateComponent, configuration: new DiagridDashboardConfiguration
{
    // AppId = "diagrid-dashboard",         // Optional, Dapr APP_ID
    // Port = 8080,                         // Optional, fixed host port (ephemeral by default)
    // ComponentsPath / ComponentFile are resolved automatically from the state component.
});
```

## 4 - Choosing how the Dashboard runs

How the dashboard is launched is a separate concern from its shared configuration, expressed through an
`IDiagridDashboardLaunchMode`. Two implementations ship in the box.

### Container (default)

If you pass no launch mode, the dashboard runs as a container. Container-specific knobs (image, tag, container name)
live on `ContainerLaunchMode`:

```csharp
builder.AddDiagridDashboard(stateComponent, launchMode: new ContainerLaunchMode
{
    // Image = "ghcr.io/diagridio/diagrid-dashboard", // Optional
    // Version = "latest",                            // Optional, image tag
    // ContainerName = "diagrid-dashboard",           // Optional, defaults to the resource name
});
```

### Local executable

To run the dashboard as a process on the host instead — no container runtime involved — pass an `ExecutableLaunchMode`
pointing at the binary. It reads the same component YAML, but from the host perspective (so connection strings resolve
against `localhost` rather than in-cluster names):

```csharp
builder.AddDiagridDashboard(stateComponent, launchMode: new ExecutableLaunchMode("/path/to/diagrid-dashboard")
{
    // WorkingDirectory = ".",              // Optional
    // PortEnvironmentVariable = "PORT",    // Optional, if the binary takes its listen port from the environment
});
```

Both modes present the same shared options and both hand back an `IResourceBuilder<IDiagridDashboardResource>`, so the
rest of your AppHost (`WaitFor`, `GetEndpoint`, `WithEnvironment`, ...) works identically regardless of which you choose.

### Custom Naming

If you want to run more than one dashboard instance, or you'd just prefer a different resource name, pass a name:

```csharp
builder.AddDiagridDashboard(stateComponent, name: "my-dashboard");
```

## Additional Resources

See the [Diagrid Dashboard](https://docs.diagrid.io/develop/local-development/dev-dashboard/) page in the Diagrid docs for more on the dashboard itself.
