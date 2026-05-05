using Aspire.Hosting;
using Aspire.Hosting.Yarp;
using Diagrid.Aspire.Hosting.Dashboard;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "local", true);
var password = builder.AddParameter("password", "zxczxc123", true);

var postgres = builder.AddPostgres("postgres", username, password, port: 5432);
var daprStateDatabase = postgres.AddDatabase("dapr-state", "dapr_state");

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
