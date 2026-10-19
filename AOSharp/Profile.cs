using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using AOSharp.Bootstrap.IPC;
using EasyHook;
using Newtonsoft.Json;
using Serilog;

namespace AOSharp
{
    // One runtime object per character, shared by every activity tab.
    public class Profile : INotifyPropertyChanged
    {
        public string Name { get; set; }

        private bool _isActive;
        [JsonIgnore]
        public bool IsActive
        {
            get => _isActive;
            set { _isActive = value; OnPropertyChanged(nameof(IsActive)); }
        }

        private bool _isInjected;
        [JsonIgnore]
        public bool IsInjected
        {
            get => _isInjected;
            private set { _isInjected = value; OnPropertyChanged(nameof(IsInjected)); }
        }

        [JsonIgnore]
        public Process Process { get; set; }
        [JsonIgnore]
        public string LiveActivityId { get; private set; }
        [JsonIgnore]
        public string LiveActivityName { get; private set; }
        [JsonIgnore]
        public string StatusActivityId { get; private set; }
        [JsonIgnore]
        public bool LoaderDisconnected { get; private set; }

        private IPCClient _ipcClient;
        [JsonIgnore]
        public Dictionary<string, PluginStatusMessage> PluginStatuses { get; } =
            new Dictionary<string, PluginStatusMessage>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler PluginStatusesChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        private void SetPluginStatus(PluginStatusMessage status)
        {
            PluginStatuses[status.Path] = status;
            PluginStatusesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SetLiveActivity(string id, string name)
        {
            LiveActivityId = id;
            LiveActivityName = name;
            OnPropertyChanged(nameof(LiveActivityId));
            OnPropertyChanged(nameof(LiveActivityName));
        }

        public void UpdateActivityName(string activityId, string name)
        {
            // Rename the display label only; do not switch or reload a live injection.
            if (!string.IsNullOrEmpty(activityId) && LiveActivityId == activityId)
            {
                LiveActivityName = name;
                OnPropertyChanged(nameof(LiveActivityName));
            }
        }

        private void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public bool Inject(IEnumerable<string> plugins, string activityId, string activityName)
        {
            // All tabs share this guard; staged selections never change a live injection.
            if (!IsActive || Process == null || IsInjected || _ipcClient != null)
                return false;

            string[] paths = plugins.ToArray();
            if (paths.Length == 0) return false;
            StatusActivityId = activityId;
            LoaderDisconnected = false;
            PluginStatuses.Clear();
            foreach (string path in paths)
                SetPluginStatus(new PluginStatusMessage { Path = path, State = PluginLoadState.Loading, Detail = "Waiting for loader" });
            IPCClient pipe = null;
            try
            {
                RemoteHooking.Inject(Process.Id, "AOSharp.Bootstrap.dll", string.Empty, Process.Id.ToString(CultureInfo.InvariantCulture));
                pipe = new IPCClient(Process.Id.ToString());
                _ipcClient = pipe;
                pipe.RegisterCallback((byte)HookOpCode.PluginStatus, typeof(PluginStatusMessage), (sender, message) =>
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_ipcClient == pipe) SetPluginStatus((PluginStatusMessage)message);
                    }));
                });
                pipe.OnDisconnected += disconnected =>
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (_ipcClient != pipe) return;
                        LoaderDisconnected = true;
                        _ipcClient = null;
                        SetLiveActivity(null, null);
                        IsInjected = false;
                        foreach (string path in paths)
                            SetPluginStatus(new PluginStatusMessage { Path = path, State = PluginLoadState.Failed, Detail = "Loader disconnected" });
                    }));
                };
                pipe.Connect();
                pipe.Send(new LoadAssemblyMessage { Assemblies = paths });
                SetLiveActivity(activityId, activityName);
                IsInjected = true;
                return true;
            }
            catch (Exception e)
            {
                _ipcClient = null;
                SetLiveActivity(null, null);
                IsInjected = false;
                try { pipe?.Disconnect(); } catch { }
                Log.Error(e, "Failed to inject bootloader");
                foreach (string path in paths)
                    SetPluginStatus(new PluginStatusMessage { Path = path, State = PluginLoadState.Failed, Detail = "Bootloader injection failed: " + e.Message });
                return false;
            }
        }

        public void Eject(string activityId)
        {
            if (_ipcClient == null || LiveActivityId != activityId)
                return;

            // Breaking the pipe unloads the same bootstrap/plugin domain as before.
            IPCClient pipe = _ipcClient;
            _ipcClient = null;
            SetLiveActivity(null, null);
            IsInjected = false;
            StatusActivityId = null;
            LoaderDisconnected = false;
            PluginStatuses.Clear();
            PluginStatusesChanged?.Invoke(this, EventArgs.Empty);
            pipe.Disconnect();
        }
    }
}
