using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Diagrid.Aspire.Hosting.Dashboard;

/// <summary>
///     Discovers the Dapr sidecars in the application model and projects them into the environment variables the
///     dashboard reads in Aspire mode (see https://github.com/diagridio/dev-dashboard/blob/main/docs/aspire-discovery.md).
///     <br /><br />
///     The dashboard never connects to Aspire itself &mdash; it takes the app list purely from
///     <c>DEVDASHBOARD_APP_COUNT</c> and a <c>DEVDASHBOARD_APP_&lt;i&gt;_*</c> block per app, then polls each sidecar's
///     HTTP API directly at runtime. So "discovery" is entirely our job at build time: find the daprd instances, and hand
///     the dashboard a URL for each that resolves from <em>its</em> perspective (container or host).
/// </summary>
internal static class DaprSidecarDiscovery
{
    /// <summary>
    ///     Attaches a late environment callback that enumerates the Dapr sidecars and emits one dashboard app entry per
    ///     sidecar. Runs as a callback (rather than eagerly) because the Dapr integration only materializes its sidecar
    ///     resources during <c>BeforeStartAsync</c>, well after <c>AddDiagridDashboard</c> returns.
    /// </summary>
    internal static IResourceBuilder<T> WithDiscoveredDaprApps<T>(
        this IResourceBuilder<T> dashboard,
        IDistributedApplicationBuilder applicationBuilder
    )
        where T : IResourceWithEnvironment
    {
        return dashboard.WithEnvironment(async context =>
        {
            var index = 0;

            foreach (var sidecar in applicationBuilder.Resources.Where(IsDaprSidecar))
            {
                var args = await MaterializeArgsAsync(sidecar, context.CancellationToken);

                var httpEndpoint = ResolveDaprHttpEndpoint(sidecar, args);
                // note: without a referenceable HTTP endpoint we can't hand the dashboard a reachable URL, so the entry
                // would be useless. Skip it rather than emit a broken app the dashboard would fail to poll.
                if (httpEndpoint is null)
                    continue;

                // note: --app-id is Dapr's own contract and authoritative; fall back to the parent resource name, which
                // is what the integration defaults the app-id to anyway.
                var appId = GetArgValue(args, "--app-id") ?? ParentName(sidecar) ?? sidecar.Name;

                context.EnvironmentVariables[$"DEVDASHBOARD_APP_{index}_ID"] = appId;
                // note: assigning the EndpointReference (not a string) is what lets Aspire render the URL from the
                // dashboard's perspective and route container -> host through its portable bridge.
                context.EnvironmentVariables[$"DEVDASHBOARD_APP_{index}_DAPR_HTTP"] = httpEndpoint;

                // note: LABEL is optional and defaults to the app-id; only bother when the parent gives us something nicer.
                var label = ParentName(sidecar);
                if (!string.IsNullOrEmpty(label) && !string.Equals(label, appId, StringComparison.Ordinal))
                    context.EnvironmentVariables[$"DEVDASHBOARD_APP_{index}_LABEL"] = label!;

                index++;
            }

            // note: required, and 0 is valid (empty dashboard). We own it exclusively so the launch modes don't.
            context.EnvironmentVariables["DEVDASHBOARD_APP_COUNT"] = index.ToString(CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    ///     A resource is treated as a daprd instance when its launch binary is Dapr's launcher &mdash; the <c>dapr</c>
    ///     CLI (<c>dapr run ...</c>) today, or <c>daprd</c> directly / a daprd container in a future integration &mdash;
    ///     and it actually exposes endpoints. Keying on the binary rather than a specific integration's types keeps this
    ///     working if the sidecar ever moves from an executable into a container.
    /// </summary>
    private static bool IsDaprSidecar(IResource resource)
    {
        var binary = LaunchBinaryName(resource);

        return binary is "dapr" or "daprd"
            && resource is IResourceWithEndpoints
            && resource.Annotations.OfType<EndpointAnnotation>().Any();
    }

    /// <summary>
    ///     The bare binary name a resource launches, normalized so <c>/usr/local/bin/dapr</c>, <c>dapr.exe</c> and
    ///     <c>ghcr.io/dapr/daprd:1.14</c> all reduce to <c>dapr</c> / <c>daprd</c>. Returns <c>null</c> for resources that
    ///     don't launch a process we can name (e.g. marker resources).
    /// </summary>
    private static string? LaunchBinaryName(IResource resource)
    {
        var launcher = resource switch
        {
            ExecutableResource executable => executable.Command,
            ContainerResource container => container.Entrypoint,
            _ => null,
        };

        if (!string.IsNullOrEmpty(launcher))
            return NormalizeBinary(launcher!);

        // note: a daprd container may carry no explicit entrypoint and rely on the image's own, so fall back to the image.
        if (resource is ContainerResource
            && resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault() is { } image)
            return NormalizeBinary(image.Image);

        return null;
    }

    private static string NormalizeBinary(string value)
    {
        var lastSeparator = value.LastIndexOfAny(['/', '\\']);
        var name = lastSeparator >= 0 ? value[(lastSeparator + 1)..] : value;

        // note: strip an image tag/digest then any file extension: "daprd:1.14" -> "daprd", "dapr.exe" -> "dapr".
        var tag = name.IndexOfAny([':', '@']);
        if (tag >= 0)
            name = name[..tag];

        var extension = name.IndexOf('.');
        if (extension >= 0)
            name = name[..extension];

        return name;
    }

    /// <summary>
    ///     Picks the sidecar's HTTP endpoint as an <see cref="EndpointReference"/>. Prefers the conventional
    ///     <c>http</c>-named endpoint the Dapr integration registers, then falls back to matching the
    ///     <c>--dapr-http-port</c> argument, then to any <c>http</c>-scheme endpoint.
    /// </summary>
    private static EndpointReference? ResolveDaprHttpEndpoint(IResource resource, IReadOnlyList<string> args)
    {
        if (resource is not IResourceWithEndpoints endpointResource)
            return null;

        var endpoints = resource.Annotations.OfType<EndpointAnnotation>().ToList();

        var http = endpoints.FirstOrDefault(endpoint => endpoint.Name == "http");

        if (http is null && int.TryParse(GetArgValue(args, "--dapr-http-port"), out var httpPort))
            http = endpoints.FirstOrDefault(endpoint => endpoint.TargetPort == httpPort || endpoint.Port == httpPort);

        http ??= endpoints.FirstOrDefault(endpoint =>
            string.Equals(endpoint.UriScheme, "http", StringComparison.OrdinalIgnoreCase));

        return http is null ? null : new EndpointReference(endpointResource, http.Name);
    }

    /// <summary>
    ///     Materializes a resource's command-line arguments by running its <see cref="CommandLineArgsCallbackAnnotation"/>
    ///     callbacks against a throwaway list. Best-effort: any callback that throws when invoked out of the normal
    ///     pipeline is swallowed, leaving whatever was collected so far.
    /// </summary>
    private static async Task<IReadOnlyList<string>> MaterializeArgsAsync(IResource resource, CancellationToken cancellationToken)
    {
        var args = new List<object>();

        if (resource.TryGetAnnotationsOfType<CommandLineArgsCallbackAnnotation>(out var callbacks))
        {
            var context = new CommandLineArgsCallbackContext(args, cancellationToken);

            try
            {
                foreach (var callback in callbacks)
                    await callback.Callback(context);
            }
            catch
            {
                // note: best-effort; we still parse whatever args were appended before the failure.
            }
        }

        return args.Select(arg => arg as string ?? arg?.ToString() ?? string.Empty).ToList();
    }

    /// <summary>
    ///     Reads the value following a <c>--flag value</c> pair, or the <c>--flag=value</c> form.
    /// </summary>
    private static string? GetArgValue(IReadOnlyList<string> args, string flag)
    {
        for (var i = 0; i < args.Count; i++)
        {
            if (args[i] == flag)
                return i + 1 < args.Count ? args[i + 1] : null;

            var inline = flag + "=";
            if (args[i].StartsWith(inline, StringComparison.Ordinal))
                return args[i][inline.Length..];
        }

        return null;
    }

    private static string? ParentName(IResource resource) =>
        resource.Annotations
            .OfType<ResourceRelationshipAnnotation>()
            .FirstOrDefault(relationship => relationship.Type == "Parent")
            ?.Resource.Name;
}
