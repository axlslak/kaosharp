using System;
using System.Collections.Concurrent;
using System.Linq;
using AOSharp.Bootstrap.IPC;
using Serilog;

namespace AOSharp.Bootstrap
{
    public partial class Main
    {
        private readonly ConcurrentQueue<ProfileControlMessage> _controlInputs = new ConcurrentQueue<ProfileControlMessage>();
        private readonly ConcurrentQueue<string> _controlReplies = new ConcurrentQueue<string>();
        private long _operationId;
        private ProfileControlMessage _activeReload;
        private bool _initialRequestReceived;
        private bool _readyAnnounced;
        private bool _cleanupFailed;

        private void OnProfileControl(object sender, IPCMessage message)
        {
            var control = (ProfileControlMessage)message;
            if (control.Kind == ProfileControlKind.Reply)
            {
                if (_controlReplies.Count < 64) _controlReplies.Enqueue(control.Text);
            }
            else if (control.Kind == ProfileControlKind.Reload && _controlInputs.Count < 16)
                _controlInputs.Enqueue(control);
        }

        private bool TryLauncherCommand(string input)
        {
            string command = (input ?? "").Trim();
            if (!command.StartsWith("/")) return false;
            command = command.Substring(1);
            int split = command.IndexOfAny(new[] { ' ', '\t' });
            string verb = split < 0 ? command : command.Substring(0, split);
            bool profileCommand = verb.Equals("ao#", StringComparison.OrdinalIgnoreCase);
            bool pluginCommand = verb.Equals("load", StringComparison.OrdinalIgnoreCase) ||
                verb.Equals("unload", StringComparison.OrdinalIgnoreCase);
            if (!profileCommand && !pluginCommand) return false;
            string args = split < 0 ? "" : command.Substring(split + 1).Trim();
            string requestText = profileCommand ? args : verb.ToLowerInvariant() + " " + args;
            if (requestText.Length > 512)
            {
                if (_controlReplies.Count < 64) _controlReplies.Enqueue("AO#: command is too long.");
            }
            else if (_statusQueue.Count < 512)
                _statusQueue.Enqueue(new ProfileControlMessage { Kind = profileCommand ? ProfileControlKind.Command :
                    ProfileControlKind.PluginCommand, Text = requestText });
            return true;
        }

        private void Progress(ProfileControlMessage request, ProfileReloadStage stage, string detail)
        {
            _statusQueue.Enqueue(new ProfileControlMessage { Kind = ProfileControlKind.Progress,
                OperationId = request.OperationId, Stage = stage, Text = detail });
        }

        private void UpdateProfileControl()
        {
            // Only the engine thread owns managed-domain teardown and startup.
            while (_controlInputs.TryDequeue(out ProfileControlMessage request))
            {
                if (request.OperationId <= _operationId || !_readyAnnounced || _activeReload != null ||
                    _retryDelay.IsRunning || _cleanupFailed ||
                    request.Assemblies.Any(string.IsNullOrWhiteSpace))
                {
                    Progress(request, ProfileReloadStage.Rejected, "Loader is not ready for a handover.");
                    continue;
                }
                if (_environmentReady && _pluginProxy != null && !_pluginProxy.IsGameReady())
                {
                    Progress(request, ProfileReloadStage.Rejected, "Wait until this character has finished zoning.");
                    continue;
                }

                _activeReload = request;
                Progress(request, ProfileReloadStage.Unloading, "Unloading the old plugins...");
                if (!UnloadPluginEnvironment())
                {
                    CompleteProfileControl();
                    continue;
                }

                // This acknowledgment is the ownership boundary. Nothing new loads before cleanup succeeds.
                _operationId = request.OperationId;
                _assemblies = request.Assemblies.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                _retryDelay.Reset();
                _retryUsed = false;
                Progress(request, ProfileReloadStage.Loading, "Old plugins unloaded. Loading the new selection...");
                LoadPluginEnvironment();
            }
        }

        private void CompleteProfileControl()
        {
            if (!_initialRequestReceived || !_resultsReported || _retryDelay.IsRunning) return;
            if (_activeReload != null)
            {
                bool success = !_cleanupFailed && _environmentReady && _pluginProxy != null &&
                    _pluginProxy.GetLoadResults().Length == _assemblies.Length &&
                    _pluginProxy.GetLoadResults().All(result => result.State == PluginLoadState.Initialized);
                Progress(_activeReload, _cleanupFailed ? ProfileReloadStage.CleanupFailed :
                    success ? ProfileReloadStage.Completed : ProfileReloadStage.Failed,
                    _cleanupFailed ? "Cleanup could not be confirmed. Restart this game client before injecting again." :
                    success ? "Profile handover complete." : "Some plugins failed to initialize. See their error details in the launcher.");
                _activeReload = null;
            }
            if (!_readyAnnounced)
            {
                _readyAnnounced = true;
                _statusQueue.Enqueue(new ProfileControlMessage { Kind = ProfileControlKind.Ready,
                    Stage = _cleanupFailed ? ProfileReloadStage.CleanupFailed : ProfileReloadStage.Completed });
            }
        }

        private void FlushControlReplies()
        {
            if (!_environmentReady || _pluginProxy == null) return;
            while (_controlReplies.TryDequeue(out string text))
            {
                try { _pluginProxy.WriteControlText(text); }
                catch (Exception ex) { Log.Error(ex, "Could not display AO# command reply"); }
            }
        }
    }
}
