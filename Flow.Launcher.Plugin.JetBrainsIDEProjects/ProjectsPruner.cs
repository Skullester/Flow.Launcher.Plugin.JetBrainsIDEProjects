using System.Linq;
using System.Xml;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects;

public static class ProjectsPruner
{
    public static void Prune(RecentProject project)
    {
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
}