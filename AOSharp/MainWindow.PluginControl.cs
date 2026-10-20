using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using AOSharp.Bootstrap.IPC;
using MahApps.Metro.Controls.Dialogs;
using Activity = AOSharp.Models.Activity;

namespace AOSharp
{
    public partial class MainWindow
    {
        private string _contextPluginKey;

        private void LibraryContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (_contextPluginKey == null && LibraryGrid.SelectedItems.Count == 1 &&
                LibraryGrid.SelectedItem is KeyValuePair<string, PluginModel> selected)
                _contextPluginKey = selected.Key;
            EditPluginShortNameMenuItem.IsEnabled = !_activityDialogOpen && _contextPluginKey != null &&
                Config.Plugins.ContainsKey(_contextPluginKey);
            // Capture the clicked row before the context menu closes (including keyboard activation).
            EditPluginShortNameMenuItem.Tag = _contextPluginKey;
        }

        private void LibraryContextMenu_Closed(object sender, RoutedEventArgs e) { _contextPluginKey = null; }

        private static bool ValidShortName(string name) => name.Length <= 32 &&
            !name.Any(character => char.IsWhiteSpace(character) || char.IsControl(character));

        private async void EditPluginShortName_Click(object sender, RoutedEventArgs e)
        {
            string key = ((MenuItem)sender).Tag as string;
            if (_activityDialogOpen || key == null || !Config.Plugins.TryGetValue(key, out PluginModel plugin)) return;
            _activityDialogOpen = true;
            try
            {
                var settings = new MetroDialogSettings { DefaultText = plugin.ShortName,
                    AffirmativeButtonText = "Save", NegativeButtonText = "Cancel" };
                while (Config.Plugins.TryGetValue(key, out PluginModel current) && ReferenceEquals(plugin, current))
                {
                    string name = await this.ShowInputAsync("Short name", plugin.Name +
                        "\nOne word, up to 32 characters. Leave blank to clear it.", settings);
                    if (name == null || !Config.Plugins.TryGetValue(key, out current) || !ReferenceEquals(plugin, current)) return;
                    name = name.Trim();
                    bool duplicate = name.Length > 0 && Config.Plugins.Any(pair => pair.Key != key &&
                        string.Equals(pair.Value.ShortName?.Trim(), name, StringComparison.OrdinalIgnoreCase));
                    if (!ValidShortName(name) || duplicate)
                    {
                        settings.DefaultText = name;
                        await this.ShowMessageAsync("Short name", duplicate ? "That short name belongs to another plugin." :
                            "Use one word, up to 32 characters, without spaces or control characters.");
                        continue;
                    }
                    if (plugin.ShortName == name) return;
                    plugin.ShortName = name;
                    await SaveConfigurationAsync();
                    return;
                }
            }
            finally { _activityDialogOpen = false; }
        }

        private bool ApplyPluginChoice(Activity activity, Profile character, string key, bool enabled)
        {
            bool changed = activity.IsEnabled(character.Name, key) != enabled;
            if (enabled && !activity.PluginKeys.Contains(key))
            {
                activity.PluginKeys.Add(key);
                changed = true;
            }
            activity.SetEnabled(character.Name, key, enabled);
            return changed;
        }

        private void HandlePluginCommand(Profile requester, string verb, string arguments)
        {
            string[] tokens = arguments.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 1 || tokens.Length > 2 ||
                (tokens.Length == 2 && !tokens[1].Equals("all", StringComparison.OrdinalIgnoreCase)) ||
                !ValidShortName(tokens[0]))
            {
                _ = requester.SendGameTextAsync("AO#: use /" + verb + " <short name> [all]. Set short names through Plugin list > right-click > Edit.");
                return;
            }
            var matches = Config.Plugins.Where(pair => string.Equals(pair.Value.ShortName?.Trim(), tokens[0],
                StringComparison.OrdinalIgnoreCase)).ToArray();
            if (matches.Length != 1 || string.IsNullOrWhiteSpace(matches[0].Value.Path))
            {
                _ = requester.SendGameTextAsync(matches.Length > 1 ? "AO#: that short name is ambiguous. Give each plugin a unique short name." :
                    "AO#: short name not found or DLL path missing. Set it through Plugin list > right-click > Edit.");
                return;
            }
            var plugin = matches[0];
            bool load = verb == "load";
            Profile[] characters = tokens.Length == 2 ? Profiles.Where(character => character.IsActive && character.IsInjected).ToArray() :
                new[] { requester };
            var outcomes = new List<string>();
            bool changed = false;
            int queued = 0;
            foreach (Profile character in characters)
            {
                Activity activity = Config.Activities.FirstOrDefault(candidate => candidate.Id == character.LiveActivityId);
                if (activity == null || !character.CanReinject)
                {
                    outcomes.Add(character.Name.Trim() + ": " + (character.RequiresRestart ? "restart this game client" :
                        character.IsSwitching ? "handover already queued" : "running activity/ready matching loader required"));
                    continue;
                }

                // Change exactly one live DLL. Unrelated staged choices wait for a later explicit Reinject.
                var live = new HashSet<string>(character.LivePluginPaths, StringComparer.OrdinalIgnoreCase);
                bool present = live.Contains(plugin.Value.Path);
                bool initialized = character.PluginStatuses.TryGetValue(plugin.Value.Path, out PluginStatusMessage status) &&
                    status.State == PluginLoadState.Initialized;
                if ((!load && !present) || (load && present && initialized))
                {
                    changed |= ApplyPluginChoice(activity, character, plugin.Key, load);
                    outcomes.Add(character.Name.Trim() + ": already " + (load ? "loaded" : "unloaded"));
                    continue;
                }
                if (load) live.Add(plugin.Value.Path);
                else live.Remove(plugin.Value.Path);
                // Preserve the running order; append only a newly loaded DLL.
                string[] paths = character.LivePluginPaths.Where(path => live.Contains(path)).ToArray();
                if (load && !present) paths = paths.Concat(new[] { plugin.Value.Path }).ToArray();
                string success = plugin.Value.ShortName + (load ? " loaded" : " unloaded") + " in " + activity.Name + ".";
                if (TryQueueHandover(activity, character, paths, requester, true, success, out string reason))
                {
                    changed |= ApplyPluginChoice(activity, character, plugin.Key, load);
                    queued++;
                }
                else outcomes.Add(character.Name.Trim() + ": " + reason);
            }
            // Save synchronously before draining; don't make the player dismiss a launcher dialog.
            Exception saveError = changed ? TrySaveConfiguration() : null;
            if (changed)
            {
                LibraryGrid.Items.Refresh();
                RefreshActivityPlugins();
            }
            string summary = "Queued " + queued + " " + verb + " request(s) for " + plugin.Value.ShortName + "." +
                (outcomes.Count == 0 ? "" : " " + string.Join("; ", outcomes)) +
                (saveError == null ? "" : " Choices could not be saved; use Retry in the launcher.");
            SetupMessage = HandoverSummary = summary;
            _ = requester.SendGameTextAsync(EscapeChat("AO#: " + summary));
            if (!_drainingSwitchQueue) _ = DrainSwitchQueueAsync();
        }
    }
}
