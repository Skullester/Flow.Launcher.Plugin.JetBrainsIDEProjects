using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using Flow.Launcher.Plugin.JetBrainsIDEProjects.Settings;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects
{
    /// <inheritdoc cref="IPlugin" />
    public partial class JetBrainsIDEProjects : IPlugin, ISettingProvider, IContextMenu
    {
        private PluginInitContext _context;
        private Settings.Settings _settings;

        /// <inheritdoc />
        public void Init(PluginInitContext context)
        {
            _context = context;
            if (_context?.API != null)
            {
                _settings = _context.API.LoadSettingJsonStorage<Settings.Settings>();
            }
            else
            {
                _settings = new Settings.Settings();
            }
        }

        /// <inheritdoc />
        public List<Result> Query(Query query)
        {
            List<RecentProject> projects;
            try
            {
                var applications = RecentProjectsReader.GetApplications();
                projects = RecentProjectsReader.GetRecentProjects(applications);
            }
            catch (Exception e)
            {
                return new List<Result>(
                    new[]
                    {
                        new Result
                        {
                            Title = "Error reading JetBrains IDE projects",
                            SubTitle = e.Message,
                            Action = _ => false,
                            IcoPath = "icon.png",
                            Score = 0
                        }
                    }
                );
            }

            var results = new List<Result>();

            foreach (var project in projects)
            {
                if (project.IsDeleted) continue;
                var stringToSearchIn = project.Name;
                if (_settings.IncludePathInSearch)
                {
                    stringToSearchIn += " " + project.Path;
                }

                var score = GetScore(query.Search, stringToSearchIn);

                if (score > 0)
                {
                    results.Add(new Result
                    {
                        Title = project.Name,
                        SubTitle = project.Path,
                        IcoPath = project.Application?.IcoFile ?? "icon.png",
                        Action = actionContext =>
                        {
                            var resetQuery = !actionContext.SpecialKeyState.ShiftPressed;
                            var closeMainWindow = !actionContext.SpecialKeyState.CtrlPressed;

                            if (!closeMainWindow && resetQuery)
                            {
                                _context.API.ChangeQuery(_context.CurrentPluginMetadata.ActionKeyword + " ");
                            }

                            _context.API.ShellRun($"\"{project.Path}\"", project.Application.ExePath);

                            return closeMainWindow;
                        },
                        ContextData = project,
                        Score = score
                    });
                }
            }

            const string pruneAllDeletedProjects = "Prune all deleted projects";
            var score2 = GetScore(query.Search, pruneAllDeletedProjects);
            if (score2 > 0)
            {
                results.Add(new Result()
                {
                    Title = pruneAllDeletedProjects,
                    Glyph = new GlyphInfo("Segoe MDL2 Assets", "\xF78A"),
                    Action = _ =>
                    {
                        foreach (var prunableProject in projects.Where(x => x.IsDeleted))
                        {
                            ProjectsPruner.Prune(prunableProject);
                        }

                        return true;
                    },
                    Score = score2
                });
            }
            return results;
        }

        private int GetScore(string query, string toCompare)
        {
            return string.IsNullOrWhiteSpace(query)
                ? 100
                : _context.API.FuzzySearch(query, toCompare)
                    .Score;
        }

        /// <inheritdoc />
        public Control CreateSettingPanel()
        {
            return new SettingsControl(_settings);
        }

        public List<Result> LoadContextMenus(Result selectedResult)
        {
            if (selectedResult.ContextData is null)
                return [];
            var proj = (RecentProject)selectedResult.ContextData;
            var results = new List<Result>();
            if (!proj.IsDeleted)
            {
                results.Add(
                    new Result
                    {
                        Title = "Open in explorer",
                        Glyph = new GlyphInfo("Segoe MDL2 Assets", "\xED43"),
                        Action = _ =>
                        {
                            var projectDirPath = Regex.Replace(proj.Path, @"([\\/][^/\\]*\.[^/\\]*$)", "");
                            _context.API.ShellRun($"""
                                                   -Command "Start-Process '{projectDirPath}'"
                                                   """, "pwsh.exe");
                            return true;
                        }
                    }
                );
            }

            results.Add(
                new Result
                {
                    Title = "Prune project",
                    Glyph = new GlyphInfo("Segoe MDL2 Assets", "\xF78A"),
                    Action = _ =>
                    {
                        ProjectsPruner.Prune(proj);
                        return true;
                    }
                }
            );
            return results;
        }
    }
}