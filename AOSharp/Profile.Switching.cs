using System;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AOSharp.Bootstrap.IPC;
using Newtonsoft.Json;
using Serilog;

namespace AOSharp
{
    public sealed class ProfileCommandEventArgs : EventArgs
    {
        public string Arguments { get; }
        public ProfileCommandEventArgs(string arguments) { Arguments = arguments; }
    }

    public partial class Profile
    {
        [JsonIgnore] public bool SupportsProfileControl { get; private set; }
        [JsonIgnore] public bool LoaderReady { get; private set; }
        [JsonIgnore] public bool IsSwitching { get; private set; }
        [JsonIgnore] public bool RequiresRestart { get; private set; }
        [JsonIgnore] public string SwitchStatus { get; private set; }
        [JsonIgnore] public string PendingActivityId { get; private set; }
        [JsonIgnore] public bool IsRuntimeCurrent => SameProcess();
        [JsonIgnore] public bool CanReinject => IsActive && IsInjected && _ipcClient != null &&
            SupportsProfileControl && LoaderReady && !IsSwitching && !RequiresRestart && SameProcess();
        [JsonIgnore] public bool CanStartInjection => IsActive && Process != null && !IsInjected && !IsSwitching &&
            (!RequiresRestart || NewProcessSinceSession());

        public event EventHandler<ProfileCommandEventArgs> ProfileCommandRequested;
        private static long _nextOperation;
        private long _statusOperationId;
        private long _previousStatusOperationId;
        private long _pendingOperationId;
        private int _sessionProcessId;
        private long _sessionStartTicks;
        private string _pendingActivityName;
        private string[] _pendingPaths;
        private TaskCompletionSource<string> _switchCompletion;

        private bool SameProcess()
        {
            try { return Process != null && !Process.HasExited && Process.Id == _sessionProcessId &&
                    Process.StartTime.ToUniversalTime().Ticks == _sessionStartTicks; }
            catch { return false; }
        }

        private bool NewProcessSinceSession()
        {
            try { return Process != null && !Process.HasExited && (Process.Id != _sessionProcessId ||
                    Process.StartTime.ToUniversalTime().Ticks != _sessionStartTicks); }
            catch { return false; }
        }

        private static void PostToUI(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return;
            try { dispatcher.BeginInvoke(action); }
            catch (InvalidOperationException) { }
        }

        private void ControlChanged()
        {
            OnPropertyChanged(nameof(IsSwitching));
            OnPropertyChanged(nameof(PendingActivityId));
            OnPropertyChanged(nameof(CanReinject));
            OnPropertyChanged(nameof(RequiresRestart));
            OnPropertyChanged(nameof(SwitchStatus));
        }

        private void ResetControlSession()
        {
            _sessionProcessId = Process.Id;
            _sessionStartTicks = Process.StartTime.ToUniversalTime().Ticks;
            _statusOperationId = 0;
            _pendingOperationId = 0;
            SupportsProfileControl = LoaderReady = IsSwitching = RequiresRestart = false;
            SwitchStatus = null;
            PendingActivityId = null;
            ControlChanged();
        }

        private void RegisterControlCallbacks(IPCClient pipe)
        {
            pipe.RegisterCallback((byte)HookOpCode.ProfileControl, typeof(ProfileControlMessage), (sender, message) =>
                PostToUI(() => { if (_ipcClient == pipe) HandleControl((ProfileControlMessage)message); }));
        }

        private void HandleControl(ProfileControlMessage message)
        {
            if (message.Kind == ProfileControlKind.Hello)
            {
                SupportsProfileControl = message.OperationId == ProfileControlMessage.ProtocolVersion;
                ControlChanged();
                return;
            }
            if (message.Kind == ProfileControlKind.Ready)
            {
                LoaderReady = true;
                RequiresRestart = message.Stage == ProfileReloadStage.CleanupFailed;
                if (RequiresRestart) SwitchStatus = "Cleanup failed. Restart this game client.";
                ControlChanged();
                return;
            }
            if (message.Kind == ProfileControlKind.Command)
            {
                if (message.Text.Length <= 512 && SameProcess())
                    ProfileCommandRequested?.Invoke(this, new ProfileCommandEventArgs(message.Text));
                return;
            }
            if (message.Kind != ProfileControlKind.Progress || message.OperationId != _pendingOperationId ||
                _pendingOperationId == 0) return;

            SwitchStatus = message.Text;
            switch (message.Stage)
            {
                case ProfileReloadStage.Unloading:
                    break;
                case ProfileReloadStage.Loading:
                    _statusOperationId = message.OperationId;
                    SetLiveActivity(PendingActivityId, _pendingActivityName);
                    StatusActivityId = PendingActivityId;
                    PluginStatuses.Clear();
                    foreach (string path in _pendingPaths)
                        SetPluginStatus(new PluginStatusMessage { Path = path, State = PluginLoadState.Loading,
                            Detail = "Loading new selection", OperationId = _pendingOperationId });
                    break;
                case ProfileReloadStage.Rejected:
                    _statusOperationId = _previousStatusOperationId;
                    FinishSwitch(message.Text, false);
                    break;
                case ProfileReloadStage.CleanupFailed:
                    FinishSwitch(message.Text, true);
                    break;
                case ProfileReloadStage.Failed:
                    FinishSwitch(message.Text, false);
                    break;
                case ProfileReloadStage.Completed:
                    FinishSwitch(null, false);
                    break;
            }
            ControlChanged();
        }

        public bool TryQueueSwitch(string activityId, string activityName, string[] paths, out long operationId)
        {
            operationId = 0;
            if (!CanReinject) return false;
            operationId = Interlocked.Increment(ref _nextOperation);
            _pendingOperationId = operationId;
            _previousStatusOperationId = _statusOperationId;
            PendingActivityId = activityId;
            _pendingActivityName = activityName;
            _pendingPaths = paths.ToArray();
            _switchCompletion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            IsSwitching = true;
            SwitchStatus = "Queued for " + activityName + ".";
            ControlChanged();
            return true;
        }

        public async Task<string> RunQueuedSwitchAsync(long operationId, CancellationToken cancellation)
        {
            if (_pendingOperationId != operationId || !IsSwitching || _ipcClient == null)
                return "Queued handover is no longer available.";
            if (!SameProcess())
            {
                FinishSwitch("Game client changed before handover started.", true);
                return SwitchStatus;
            }
            if (_pendingPaths.Any(path => !File.Exists(path)))
            {
                FinishSwitch("A selected plugin file disappeared before handover started. The old selection was kept.", false);
                return SwitchStatus;
            }
            IPCClient pipe = _ipcClient;
            TaskCompletionSource<string> completion = _switchCompletion;
            SwitchStatus = "Requesting handover to " + _pendingActivityName + "...";
            ControlChanged();
            string[] paths = _pendingPaths;
            Task send = Task.Run(() => pipe.Send(new ProfileControlMessage { Kind = ProfileControlKind.Reload,
                OperationId = operationId, Assemblies = paths }));
            // Observe a write that finishes after a timeout as well.
            _ = send.ContinueWith(task => Log.Error(task.Exception, "Could not send profile handover"),
                TaskContinuationOptions.OnlyOnFaulted);
            Stopwatch elapsed = Stopwatch.StartNew();
            try
            {
                while (!completion.Task.IsCompleted)
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (send.IsFaulted) await send;
                    if (!SameProcess() || elapsed.Elapsed >= TimeSpan.FromSeconds(30))
                        throw new TimeoutException("Handover could not be confirmed. Restart this game client before injecting again.");
                    await Task.WhenAny(completion.Task, Task.Delay(200, cancellation));
                }
                return await completion.Task;
            }
            catch (Exception ex)
            {
                // Keep the reservation and generation: a late acknowledgment can still resolve this request.
                RequiresRestart = true;
                IsSwitching = false;
                SwitchStatus = ex is OperationCanceledException ? "Launcher is closing; handover interrupted." : ex.Message;
                ControlChanged();
                return SwitchStatus;
            }
        }

        private void FinishSwitch(string error, bool restart)
        {
            RequiresRestart = restart;
            IsSwitching = false;
            PendingActivityId = null;
            _pendingOperationId = 0;
            SwitchStatus = error ?? "Profile handover complete.";
            _switchCompletion?.TrySetResult(error);
            ControlChanged();
        }

        private void ControlDisconnected()
        {
            SupportsProfileControl = LoaderReady = false;
            if (IsSwitching || _pendingOperationId != 0)
                FinishSwitch("Loader disconnected during handover. Restart this game client.", true);
            else
            {
                RequiresRestart = true;
                SwitchStatus = "Loader connection lost. Restart this game client before injecting again.";
            }
            ControlChanged();
        }

        public async Task SendGameTextAsync(string text)
        {
            IPCClient pipe = _ipcClient;
            if (pipe == null || !SupportsProfileControl) return;
            Task send = Task.Run(() => pipe.Send(new ProfileControlMessage { Kind = ProfileControlKind.Reply,
                Text = text.Length > 3000 ? text.Substring(0, 3000) : text }));
            _ = send.ContinueWith(task => Log.Error(task.Exception, "Could not reply to AO# command"),
                TaskContinuationOptions.OnlyOnFaulted);
            if (await Task.WhenAny(send, Task.Delay(2000)) == send)
            {
                try { await send; }
                catch (Exception ex) { Log.Error(ex, "Could not reply to AO# command"); }
            }
        }
    }
}
