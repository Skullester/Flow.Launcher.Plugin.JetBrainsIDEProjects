using System;
using System.Linq;
using System.Xml;

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects;

#pragma warning disable CS1591
public static class XMLHelpers
{
    public static XmlNodeList? GetEntries(this XmlDocument xmlDocument)
    {
        var entries = xmlDocument.SelectNodes(
            "/application/component[@name='RecentProjectsManager']/option[@name='additionalInfo']/map/entry");

        if (entries is null || entries.Count == 0)
        {
            // try again with RiderRecentProjectsManager
            entries = xmlDocument.SelectNodes(
                "/application/component[@name='RiderRecentProjectsManager']/option[@name='additionalInfo']/map/entry");
        }

        return entries;
    }
}