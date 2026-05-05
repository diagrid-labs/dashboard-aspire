using Aspire.Hosting;
using Diagrid.Aspire.Hosting.Dashboard;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "local", true);
var password = builder.AddParameter("password", "zxczxc123", true);

var postgres = builder.AddPostgres("postgres", username, password, port: 5432);
var daprStateDatabase = postgres.AddDatabase("dapr-state", "dapr_state");

builder
    .AddDiagridDashboard()
    .WaitFor(daprStateDatabase);

builder.Build().Run();
