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
        var ideName = project.Application!.DisplayName!.ToLower();
        await WaitTillIdeIsClosed(ideName);
        var xmlDoc = new XmlDocument();
        xmlDoc.Load(project.IDERecentLocationsPath!);
        var entries = xmlDoc.GetEntries()!;
        var projectEntry = entries.Cast<XmlNode>()
            .FirstOrDefault(entry => GetFullPath(entry).Equals(project.Path));
        if(projectEntry is null)
            throw new ArgumentException($"Entry with path: {project.Path} has not been found at {project.IDERecentLocationsPath}");
        projectEntry.ParentNode!.RemoveChild(projectEntry);
        xmlDoc.Save(project.IDERecentLocationsPath!);
    }

    private static string GetFullPath(XmlNode entry)
    {
        var key = entry.Attributes?["key"]?.Value!;
        var fullPath = key.Replace("$USER_HOME$", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        return fullPath;
    }

    private static async Task WaitTillIdeIsClosed(string ideName)
    {
        var ideProcesses = Process.GetProcessesByName($"{ideName}{(Environment.Is64BitOperatingSystem ? "64" : "32")}");
        using var process = ideProcesses.FirstOrDefault();
        if (process is null)
        {
            return;
        }
        await process.WaitForExitAsync();
    }
}