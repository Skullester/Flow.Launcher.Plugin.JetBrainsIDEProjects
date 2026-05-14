using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects;

public static partial class ProjectsPruner
{
    public static async Task Prune(RecentProject project)
    {
        var ideName = project.Application.DisplayName.ToLower();
        while (IsIDERunning(ideName))
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
        }

        var xmlDoc = new XmlDocument();
        xmlDoc.Load(project.IDERecentLocationsPath!);
        var entries = xmlDoc.GetEntries();
        var projectEntry = entries.Cast<XmlNode>()
            .FirstOrDefault(entry => entry.Attributes?["key"]
                ?.Value
                .Equals(project.Path) ?? false);
        projectEntry?.ParentNode!.RemoveChild(projectEntry);
        xmlDoc.Save(project.IDERecentLocationsPath!);
    }

    private static bool IsIDERunning(string ideName)
    {
        var riderProcesses = Process.GetProcessesByName($"{ideName}{(Environment.Is64BitOperatingSystem ? "64" : "32")}");
        return riderProcesses.Any();
    }
}