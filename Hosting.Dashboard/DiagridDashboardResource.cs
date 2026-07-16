using Aspire.Hosting.ApplicationModel;

namespace Diagrid.Aspire.Hosting.Dashboard;

/// <summary>
///     Common surface for a running Diagrid Dashboard, regardless of how it was launched.
///     <br /><br />
///     Both <see cref="ContainerResource"/> and <see cref="ExecutableResource"/> already implement this exact set of
///     capabilities, so pinning the launch modes to this interface lets <c>AddDiagridDashboard</c> hand back a single
///     <see cref="IResourceBuilder{T}"/> type that still supports <c>WaitFor</c>, <c>GetEndpoint</c>, <c>WithEnvironment</c>
///     and <c>WithArgs</c> no matter which implementation produced it. <see cref="IResourceBuilder{T}"/> is covariant in
///     <c>T</c>, so the concrete container/executable builders assign cleanly to <c>IResourceBuilder&lt;IDiagridDashboardResource&gt;</c>.
/// </summary>
public interface IDiagridDashboardResource
    : IResourceWithEndpoints, IResourceWithEnvironment, IResourceWithArgs, IResourceWithWaitSupport;

/// <summary>
///     The dashboard running as a container. Produced by <see cref="ContainerLaunchMode"/>.
/// </summary>
public sealed class DiagridDashboardContainerResource(string name)
    : ContainerResource(name), IDiagridDashboardResource;

/// <summary>
///     The dashboard running as a local executable. Produced by <see cref="ExecutableLaunchMode"/>.
/// </summary>
public sealed class DiagridDashboardExecutableResource(string name, string command, string workingDirectory)
    : ExecutableResource(name, command, workingDirectory), IDiagridDashboardResource;
