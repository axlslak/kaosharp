using System.ComponentModel;

namespace AOSharp.Models
{
    // A visible activity row owns its display state; the shared catalogue does not.
    public class ActivityPluginModel : INotifyPropertyChanged
    {
        public string Key { get; }
        public PluginModel Plugin { get; }
        public bool IsEnabled { get; set; }

        private string _loadStatus = "Not injected";
        private string _loadDetail = "";

        public string LoadStatus
        {
            get => _loadStatus;
            set { _loadStatus = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoadStatus))); }
        }

        public string LoadDetail
        {
            get => _loadDetail;
            set { _loadDetail = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LoadDetail))); }
        }

        public ActivityPluginModel(string key, PluginModel plugin)
        {
            Key = key;
            Plugin = plugin;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
