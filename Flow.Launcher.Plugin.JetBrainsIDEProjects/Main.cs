using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using Flow.Launcher.Plugin.JetBrainsIDEProjects.Settings;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Flow.Launcher.Plugin.JetBrainsIDEProjects
{
    /// <inheritdoc cref="IPlugin" />
    public partial class JetBrainsIDEProjects : IPlugin, ISettingProvider, IContextMenu
    {
        private PluginInitContext context;
        private Settings.Settings _settings;

        /// <inheritdoc />
        public void Init(PluginInitContext context)
        {
            this.context = context;
            if (this.context?.API != null)
            {
                _settings = this.context.API.LoadSettingJsonStorage<Settings.Settings>();
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
                if (project.IsDeleted)
                    continue;
                var stringToSearchIn = project.Name;
                if (_settings.IncludePathInSearch)
                {
                    stringToSearchIn += " " + project.Path;
                }

                var score = GetScore(query.Search, stringToSearchIn, true);

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
                                context.API.ChangeQuery(context.CurrentPluginMetadata.ActionKeyword + " ");
                            }

                            context.API.ShellRun($"\"{project.Path}\"", project.Application.ExePath);

                            return closeMainWindow;
                        },
                        ContextData = project,
                        Score = score
                    });
                }
            }

            const string pruneAllDeletedProjects = "Prune all deleted projects";
            var score2 = GetScore(query.Search, pruneAllDeletedProjects, false);
            if (score2 > 0)
            {
                results.Add(new Result()
                {
                    Title = pruneAllDeletedProjects,
                    Glyph = new GlyphInfo("Segoe MDL2 Assets", "\xF78A"),
                    AsyncAction = async _ =>
                    {
                        var prunableProjects = projects.Where(x => x.IsDeleted).ToArray();
                        if (prunableProjects.Length is 0)
                        {
                            context.API.ShowMsg("Nothing to prune");
                            return true;
                        }
    
                        foreach (var prunableProject in prunableProjects)
                        {
                            await HandlePruneTask(prunableProject);
                        }

                        return true;
                    },
                    Score = score2
                });
            }

            return results;
        }

        private async Task HandlePruneTask(RecentProject prunableProject)
        {
            var pruneTask = ProjectsPruner.Prune(prunableProject);
            if (!pruneTask.IsCompleted)
            {
                ShowMsgPruningScheduled();
                context.API.HideMainWindow();
            }

            var (pruningResult, message) = await pruneTask;
            switch (pruningResult)
            {
                case PruningResult.Error:
                    context.API.LogWarn(nameof(JetBrainsIDEProjects), message);
                    context.API.ShowMsgError("Prune failed");
                    break;
                case PruningResult.AlreadyScheduled:
                    context.API.ShowMsgError(nameof(JetBrainsIDEProjects), message);
                    break;
                case PruningResult.Success:
                    context.API.ShowMsg($"Project '{prunableProject.Name}' has been pruned");
                    break;
            }
        }

        private void ShowMsgPruningScheduled()
        {
            context.API.ShowMsg("Pruning has been scheduled");
        }

        private int GetScore(string query, string toCompare, bool canBeEmpty)
        {
            return string.IsNullOrWhiteSpace(query) && canBeEmpty
                ? 100
                : context.API.FuzzySearch(query, toCompare)
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
                var projectDirPath = CurrentDirRegex().Replace(proj.Path, "");
                results.Add(
                    new Result
                    {
                        Title = "Open in explorer",
                        Glyph = new GlyphInfo("Segoe MDL2 Assets", "\xED43"),
                        Action = _ =>
                        {
                            context.API.OpenDirectory(projectDirPath);
                            return true;
                        }
                    }
                );
                results.Add(
                    new Result
                    {
                        Title = "Open With Shell: pwsh",
                        Glyph = new GlyphInfo(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\ue756"),
                        Action = _ =>
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = "pwsh.exe",
                                WorkingDirectory = projectDirPath
                            };
                            Process.Start(psi);
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
                    AsyncAction = async _ =>
                    {
                        await HandlePruneTask(proj);
                        return true;
                    }
                }
            );
            return results;
        }

        [GeneratedRegex(@"([\\/][^/\\]*\.[^/\\]*$)")]
        private static partial Regex CurrentDirRegex();
    }
}