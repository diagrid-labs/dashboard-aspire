using Aspire.Hosting.Yarp;
using Diagrid.Aspire.Hosting.Dashboard;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDapr();

var username = builder.AddParameter("username", "local");
var password = builder.AddParameter("password", "zxczxc123", true);

var postgres = builder.AddPostgres("postgres", username, password, port: 5432);
var daprStateDatabase = postgres.AddDatabase("dapr-state", "dapr_state");

// A dapr-ized app so the dashboard has a sidecar to discover.
builder
    .AddContainer("myapp", "docker.io/traefik/whoami")
    .WithHttpEndpoint(targetPort: 80)
    .WithDaprSidecar()
;

builder
    .AddContainer("myapp2", "docker.io/traefik/whoami")
    .WithHttpEndpoint(targetPort: 80)
    .WithDaprSidecar()
;

var diagridDashboard = builder
    .AddDiagridDashboard()
    .WaitFor(daprStateDatabase);

builder.AddYarp("proxy")
    .WithHostPort(80)
    .WaitFor(diagridDashboard)
    .WithConfiguration((yarpConfiguration) =>
    {
        yarpConfiguration
            .AddRoute(diagridDashboard.GetEndpoint("http"))
            .WithMatchHosts("diagrid-dashboard.localhost")
        ;
    })
;

builder.Build().Run();