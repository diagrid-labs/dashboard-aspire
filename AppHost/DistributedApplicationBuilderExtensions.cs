using CopperDusk.Aspire.Hosting.Yaml;

namespace Diagrid.Aspire.Test.AppHost;

/// <summary>
///     Convenience extensions to keep the AppHost clean and legible.
/// </summary>
public static class DistributedApplicationBuilderExtensions
{
    /// <summary>
    ///     A sample of what could-be!
    ///     <br /><br />
    ///     We are looking into building an ecosystem of strongly typed YAML schemas & extension methods similar to this.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="daprStateDatabase"></param>
    /// <returns></returns>
    public static IResourceBuilder<YamlSourceResource> AddDaprPostgresStateComponent(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> daprStateDatabase
    )
    {
        return builder.AddYamlFile("dashboard-state", new
        {
            apiVersion = "dapr.io/v1alpha1",
            kind = "Component",
            metadata = new
            {
                name = "state",
            },
            spec = new
            {
                type = "state.postgresql",
                version = "v1",
                metadata = new object[]
                {
                    new
                    {
                        name = "connectionString",
                        value = BuildConnectionString(daprStateDatabase),
                    },
                    new { name = "actorStateStore", value = "true" },
                },
            },
        });
    }

    private static ReferenceExpression BuildConnectionString(IResourceBuilder<PostgresDatabaseResource> database)
    {
        var postgres = database.Resource.Parent;

        var entries = new (string Key, object Value)[]
        {
            ("user", postgres.UserNameParameter ?? throw new InvalidOperationException("Postgres server has no username parameter.")),
            ("password", postgres.PasswordParameter),
            // note: One gotcha is that we have to be mindful of the perspective of the component. Is it being used from inside a container, or from a locally-running process?
            ("host", postgres.Name),
            ("port", postgres.PrimaryEndpoint.Property(EndpointProperty.TargetPort)),
            ("connect_timeout", "10"),
            ("database", database.Resource.DatabaseName),
        };

        return entries
            .Select(e => e.Value switch
            {
                string s => ReferenceExpression.Create($"{e.Key}={s}"),
                ParameterResource p => ReferenceExpression.Create($"{e.Key}={p}"),
                EndpointReferenceExpression endpoint => ReferenceExpression.Create($"{e.Key}={endpoint}"),
                _ => throw new InvalidOperationException($"Unsupported value type for '{e.Key}': {e.Value.GetType()}"),
            })
            .Aggregate((acc, next) => ReferenceExpression.Create($"{acc} {next}"));
    }
}