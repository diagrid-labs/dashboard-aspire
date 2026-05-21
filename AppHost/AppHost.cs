using Aspire.Hosting.Yarp;
using Diagrid.Aspire.Hosting.Dashboard;
using Diagrid.Aspire.Test.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "local");
var password = builder.AddParameter("password", "zxczxc123", true);

var postgres = builder.AddPostgres("postgres", username, password, port: 5432);
var daprStateDatabase = postgres.AddDatabase("dapr-state", "dapr_state");

// note: This `AddDaprPostgresStateComponent` is a sample of some things we're thinking about...
var dashboardStateComponent = builder.AddDaprPostgresStateComponent(daprStateDatabase);

var diagridDashboard = builder
    .AddDiagridDashboard(dashboardStateComponent)
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