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
            try
            {
                RemoteHooking.Inject(Process.Id, "AOSharp.Bootstrap.dll", string.Empty, Process.Id.ToString(CultureInfo.InvariantCulture));

                IPCClient pipe = new IPCClient(Process.Id.ToString());
                pipe.Connect();

                pipe.Send(new LoadAssemblyMessage()
                {
                    Assemblies = plugins
                });

                pipe.OnDisconnected += (e) =>
                {
                    _ipcClient = null;
                    IsInjected = false;
                };

                _ipcClient = pipe;
                IsInjected = true;

                return true;
            }
            catch (Exception e)
            {
                Log.Error($"Failed to inject bootloader. \n\n{e.Message}");
                return false;
            }
        }

        public void Eject()
        {
            if (_ipcClient == null)
                return;

            //Breaking the pipe will cause the bootstrapper to unload itself and any loaded plugins
            _ipcClient.Disconnect();
        }
    }
}
