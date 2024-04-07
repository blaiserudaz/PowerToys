// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using ManagedCommon;
using Microsoft.PowerToys.Settings.UI.Library;
using Wox.Infrastructure;
using Wox.Plugin;
using Wox.Plugin.Common;
using Wox.Plugin.Logger;
using BrowserInfo = Wox.Plugin.Common.DefaultBrowserInfo;

namespace Community.PowerToys.Run.Plugin.JsonSearch
{
    public class Main : IPlugin, IPluginI18n, IContextMenu, IDisposable
    {
        public string Name => Properties.Resources.plugin_name;

        public string Description => Properties.Resources.plugin_description;

        public static string PluginID => "8ed1802bc23b4c52ac07eea00d57ee9a";

        private PluginInitContext? _context;
        private bool _disposed;

        private string? IconPath { get; set; }

        private string? JsonShortcutsFullPath { get; set; }

        private string? CustomIconPath { get; set; }

        private List<Data.Shortcut>? _shortcuts;

        public IEnumerable<PluginAdditionalOption> AdditionalOptions => new List<PluginAdditionalOption>()
        {
            new PluginAdditionalOption()
            {
                Key = "NotGlobalIfUri",
                DisplayLabel = Properties.Resources.plugin_global_if_uri,
                Value = false,
            },
        };

        public void Init(PluginInitContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            _context = context;
            _context.API.ThemeChanged += OnThemeChanged;
            UpdateIconPath(_context.API.GetCurrentTheme());

            InitializeUserProfilePath();
            ReadUserJsonShortcuts();
        }

        private void InitializeUserProfilePath()
        {
            var workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            workingDirectory = Path.Combine(workingDirectory, "JsonSearch");

            JsonShortcutsFullPath = File.ReadAllText(Path.Combine(workingDirectory, "shortcuts.json"));
            CustomIconPath = Path.Combine(workingDirectory, "Images");
        }

        private void ReadUserJsonShortcuts()
        {
            if (!string.IsNullOrEmpty(JsonShortcutsFullPath))
            {
                _shortcuts = JsonSerializer.Deserialize<List<Data.Shortcut>>(JsonShortcutsFullPath);
            }
        }

        public List<Result> Query(Query query)
        {
            var results = new List<Result>();

            string searchTerm = query.Search;
            if (searchTerm.Length >= 3)
            {
                if (_shortcuts != null && _shortcuts.Count > 0)
                {
                    var entries = _shortcuts.Where(x => x.keyword.StartsWith(searchTerm, System.StringComparison.CurrentCulture)).ToList();
                    foreach (var e in entries)
                    {
                        string icon = string.Empty;
                        if (IconPath != null)
                        {
                            icon = IconPath;
                        }

                        if (!string.IsNullOrEmpty(e.icon) && !string.IsNullOrEmpty(CustomIconPath))
                        {
                            icon = Path.Combine(CustomIconPath, e.icon);
                        }

                        if (!string.IsNullOrEmpty(e.url))
                        {
                            results.Add(new Result
                            {
                                Title = e.title,
                                SubTitle = e.url,
                                QueryTextDisplay = string.Empty,
                                IcoPath = icon,
                                ProgramArguments = string.Empty,
                                Action = action =>
                                {
                                    if (!Helper.OpenCommandInShell(BrowserInfo.Path, BrowserInfo.ArgumentsPattern, e.url))
                                    {
                                        return false;
                                    }

                                    return true;
                                },
                            });
                        }

                        if (!string.IsNullOrEmpty(e.path))
                        {
                            if (e.path.Contains("%username%"))
                            {
                                e.path = Regex.Replace(e.path, @"%username%", Environment.UserName.ToLowerInvariant());
                            }

                            if (e.param.Contains("%username%"))
                            {
                                e.param = Regex.Replace(e.param, @"%username%", Environment.UserName.ToLowerInvariant());
                            }

                            results.Add(new Result
                            {
                                Title = e.title,
                                SubTitle = "cmd",
                                QueryTextDisplay = string.Empty,
                                IcoPath = icon,
                                ProgramArguments = string.Empty,
                                Action = action =>
                                {
#pragma warning disable CS8621
                                    Execute(Process.Start, PrepareProcessStartInfo(e.path, e.param));
#pragma warning restore CS8621
                                    return true;
                                },
                            });
                        }
                    }
                }
            }

            return results;
        }

        private ProcessStartInfo PrepareProcessStartInfo(string command, string arguments)
        {
            ProcessStartInfo info;
            var workingDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            info = ShellCommand.SetProcessStartInfo(command, workingDirectory, arguments);
            return info;
        }

        private void Execute(Func<ProcessStartInfo, Process> startProcess, ProcessStartInfo info)
        {
#pragma warning disable CS8602
            try
            {
                startProcess(info);
            }
            catch (FileNotFoundException e)
            {
                var name = "Plugin: " + Properties.Resources.plugin_name;
                var message = $"{Properties.Resources.plugin_cmd_command_not_found}: {e.Message}";
                _context.API.ShowMsg(name, message);
            }
            catch (Win32Exception e)
            {
                var name = "Plugin: " + Properties.Resources.plugin_name;
                var message = $"{Properties.Resources.plugin_cmd_command_failed}: {e.Message}";
                _context.API.ShowMsg(name, message);
            }
#pragma warning restore CS8602
        }

        private void OnThemeChanged(Theme oldtheme, Theme newTheme)
        {
            UpdateIconPath(newTheme);
        }

        private void UpdateIconPath(Theme theme)
        {
            if (theme == Theme.Light || theme == Theme.HighContrastWhite)
            {
                IconPath = "Images/shell.light.png";
            }
            else
            {
                IconPath = "Images/shell.dark.png";
            }
        }

        public List<ContextMenuResult> LoadContextMenus(Result selectedResult)
        {
            return new List<ContextMenuResult>(0);
        }

        public string GetTranslatedPluginTitle()
        {
            return Properties.Resources.plugin_name;
        }

        public string GetTranslatedPluginDescription()
        {
            return Properties.Resources.plugin_description;
        }

        public Control CreateSettingPanel()
        {
            throw new NotImplementedException();
        }

        public void ReloadData()
        {
            if (_context is null)
            {
                return;
            }

            UpdateIconPath(_context.API.GetCurrentTheme());
            BrowserInfo.UpdateIfTimePassed();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_context != null && _context.API != null)
                {
                    _context.API.ThemeChanged -= OnThemeChanged;
                }

                _disposed = true;
            }
        }
    }
}
