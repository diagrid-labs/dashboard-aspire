using System.IO;
using System.Reflection;

namespace Diagrid.Aspire.Hosting.Dashboard;

public class DiagridDashboardConfiguration
{
    private const string DefaultName = "diagrid-dashboard";
    
    private static readonly string ExecutingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new("Unable to determine executing path!");
    
    public string AppId { get; init; } = DefaultName;
    public string ContainerName { get; init; } = DefaultName;
    public string Version { get; init; } = "latest";
    public string ComponentsPath { get; init; } = Path.Join(ExecutingPath, "Resources", "dapr", $"{DefaultName}-components");
    public string ComponentFile { get; init; } = $"{DefaultName}-state.yaml";
    public int? Port { get; init; }
}
