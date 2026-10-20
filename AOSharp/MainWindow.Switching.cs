using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AOSharp.Bootstrap.IPC;
using Activity = AOSharp.Models.Activity;

namespace AOSharp
{
    public partial class MainWindow
    {
        private sealed class ProfileSwitchJob
        {
            public Profile Character;
            public Activity Target;
            public long OperationId;
            public Profile ReplyTo;
            public string SuccessMessage;
        }

        private readonly Queue<ProfileSwitchJob> _switchQueue = new Queue<ProfileSwitchJob>();
        private readonly CancellationTokenSource _switchLifetime = new CancellationTokenSource();
        private bool _drainingSwitchQueue;

        private string _handoverSummary = "";
        public string HandoverSummary
        {
            get => _handoverSummary;
            private set { _handoverSummary = value; OnPropertyChanged(nameof(HandoverSummary)); }
        }

        public bool CanReinject => HasProfileSelected && _activeProfile.CanReinject;
        public bool CanReinjectAll => _activeActivity != null && Profiles.Any(profile => profile.CanReinject);

        private void ReinjectButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (CanReinject) QueueSwitches(_activeActivity, new[] { _activeProfile });
        }

        private void ReinjectAllButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (_activeActivity != null)
                QueueSwitches(_activeActivity, Profiles.Where(profile => profile.IsActive && profile.IsInjected).ToArray());
        }

        private static string EscapeChat(string text) => (text ?? "").Replace("&", "&amp;")
            .Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&apos;");

        private async void Profile_CommandRequested(object sender, ProfileCommandEventArgs e)
        {
            if (_closed) return;
            var requester = (Profile)sender;
            if (e.Verb == "load" || e.Verb == "unload")
            {
                HandlePluginCommand(requester, e.Verb, e.Arguments);
                return;
            }
            string args = e.Arguments.Trim();
            if (args.Length == 0)
            {
                await requester.SendGameTextAsync("AO#: running " + EscapeChat(requester.LiveActivityName ?? "unknown") +
                    ". Choose a profile below, or use /ao# name. Add 'all' to switch connected, injected characters.");
                foreach (Activity activity in Config.Activities.ToArray())
                {
                    if (_closed) return;
                    // Stable IDs keep a displayed link valid if a profile is renamed before it is clicked.
                    string label = EscapeChat(activity.Name.Length > 128 ? activity.Name.Substring(0, 128) + "..." : activity.Name);
                    string link = Guid.TryParse(activity.Id, out Guid id) && activity.Id == id.ToString("N") ?
                        "<a href='chatcmd:///ao# @" + activity.Id + "'>" + label + "</a>" : label;
                    await requester.SendGameTextAsync(link);
                }
                return;
            }

            bool all = false;
            Activity target = FindActivity(args);
            if (target == null && args.EndsWith(" all", StringComparison.OrdinalIgnoreCase))
            {
                all = true;
                target = FindActivity(args.Substring(0, args.Length - 4).TrimEnd());
            }
            if (target == null)
            {
                await requester.SendGameTextAsync("AO#: profile not found. Use /ao# to list profiles.");
                return;
            }
            Profile[] characters = all ? Profiles.Where(profile => profile.IsActive && profile.IsInjected).ToArray() :
                new[] { requester };
            QueueSwitches(target, characters, requester);
        }

        private Activity FindActivity(string name)
        {
            return Config.Activities.FirstOrDefault(activity => activity.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ??
                Config.Activities.FirstOrDefault(activity => ("@" + activity.Id).Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private void QueueSwitches(Activity target, IEnumerable<Profile> characters, Profile replyTo = null)
        {
            if (_closed || target == null || !Config.Activities.Contains(target)) return;
            int queued = 0;
            var skipped = new List<string>();
            foreach (Profile character in characters.Distinct())
            {
                string[] paths = SelectedPaths(target, character);
                if (TryQueueHandover(target, character, paths, replyTo, false, null, out string reason))
                    queued++;
                else skipped.Add(character.Name.Trim() + ": " + reason);
            }
            SetupMessage = "Queued " + queued + " handover(s) to " + target.Name + "." +
                (skipped.Count == 0 ? "" : " Skipped: " + string.Join("; ", skipped));
            HandoverSummary = SetupMessage;
            if (replyTo != null) _ = replyTo.SendGameTextAsync(EscapeChat("AO#: " + SetupMessage));
            RefreshRuntimeControls();
            if (!_drainingSwitchQueue) _ = DrainSwitchQueueAsync();
        }

        private bool TryQueueHandover(Activity target, Profile character, string[] paths, Profile replyTo,
            bool allowEmpty, string successMessage, out string reason)
        {
            reason = null;
            if (character.RequiresRestart) reason = "restart this game client";
            else if (character.IsSwitching) reason = "handover already queued";
            else if (!character.SupportsProfileControl) reason = "matching launcher/bootstrap and fresh injection required";
            else if (!character.LoaderReady) reason = "plugin startup is still running";
            else if (!allowEmpty && paths.Length == 0) reason = "no plugins selected";
            else if (paths.Any(path => !File.Exists(path))) reason = "selected plugin file is missing";
            else
            {
                try
                {
                    if (paths.Length > 1024 || new ProfileControlMessage { Kind = ProfileControlKind.Reload,
                            Assemblies = paths }.Serialize().Length > ushort.MaxValue)
                        reason = "plugin selection exceeds the loader message limit";
                }
                catch (Exception ex) { reason = ex.Message; }
            }
            if (reason != null) return false;
            if (!character.TryQueueSwitch(target.Id, target.Name, paths, out long operation))
            {
                reason = "loader is unavailable";
                return false;
            }
            _switchQueue.Enqueue(new ProfileSwitchJob { Character = character, Target = target,
                OperationId = operation, ReplyTo = replyTo, SuccessMessage = successMessage });
            return true;
        }

        private async Task DrainSwitchQueueAsync()
        {
            _drainingSwitchQueue = true;
            try
            {
                while (!_closed && _switchQueue.Count > 0)
                {
                    ProfileSwitchJob job = _switchQueue.Dequeue();
                    string error = await job.Character.RunQueuedSwitchAsync(job.OperationId, _switchLifetime.Token);
                    if (_closed) return;
                    string result = job.Character.Name.Trim() + ": " +
                        (error ?? job.SuccessMessage ?? "now running " + job.Target.Name + ".");
                    SetupMessage = result;
                    HandoverSummary = result;
                    if (job.ReplyTo != null) await job.ReplyTo.SendGameTextAsync(EscapeChat("AO#: " + result));
                    RefreshPluginStatuses(null, EventArgs.Empty);
                    QueueTabStatusRefresh();
                }
            }
            finally { _drainingSwitchQueue = false; }
        }

        private void CancelWaitingSwitches()
        {
            // No queued work is sent after the launcher closes. Closing its pipes performs normal bootstrap teardown.
            _switchQueue.Clear();
        }
    }
}
