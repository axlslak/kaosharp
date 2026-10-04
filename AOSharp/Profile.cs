using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Globalization;
using Newtonsoft.Json;
using AOSharp.Bootstrap.IPC;
using EasyHook;
using Serilog;
using System.Linq;
using System.Windows;

namespace AOSharp
{
    public class Profile : INotifyPropertyChanged
    {
        public string Name { get; set; }

        public ObservableCollection<string> EnabledPlugins { get; set; }

        [JsonIgnore]
        public bool _isActive;

        [JsonIgnore]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        [JsonIgnore]
        public bool _isInjected;

        [JsonIgnore]
        public bool IsInjected
        {
            get => _isInjected;
            set
            {
                _isInjected = value;
                OnPropertyChanged("IsInjected");
            }
        }

        [JsonIgnore]
        public Process Process { get; set; }

        [JsonIgnore]
        private IPCClient _ipcClient;

        [JsonIgnore]
        public Dictionary<string, PluginStatusMessage> PluginStatuses { get; } =
            new Dictionary<string, PluginStatusMessage>(StringComparer.OrdinalIgnoreCase);

        public event EventHandler PluginStatusesChanged;

        private void SetPluginStatus(PluginStatusMessage status)
        {
            PluginStatuses[status.Path] = status;
            PluginStatusesChanged?.Invoke(this, EventArgs.Empty);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public Profile()
        {
            IsActive = false;
            EnabledPlugins = new ObservableCollection<string>();
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool Inject(IEnumerable<string> plugins)
        {
            string[] paths = plugins.ToArray();
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
                        _ipcClient = null;
                        IsInjected = false;
                        foreach (string path in paths)
                            SetPluginStatus(new PluginStatusMessage { Path = path, State = PluginLoadState.Failed, Detail = "Loader disconnected" });
                    }));
                };
                pipe.Connect();
                pipe.Send(new LoadAssemblyMessage { Assemblies = paths });
                IsInjected = true;
                return true;
            }
            catch (Exception e)
            {
                _ipcClient = null;
                IsInjected = false;
                try { pipe?.Disconnect(); } catch { }
                Log.Error(e, "Failed to inject bootloader");
                foreach (string path in paths)
                    SetPluginStatus(new PluginStatusMessage { Path = path, State = PluginLoadState.Failed, Detail = "Bootloader injection failed: " + e.Message });
                return false;
            }
        }

        public void Eject()
        {
            if (_ipcClient == null)
                return;

            //Breaking the pipe will cause the bootstrapper to unload itself and any loaded plugins
            IPCClient pipe = _ipcClient;
            _ipcClient = null;
            IsInjected = false;
            PluginStatuses.Clear();
            PluginStatusesChanged?.Invoke(this, EventArgs.Empty);
            pipe.Disconnect();
        }
    }
}
