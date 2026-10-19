using System;
using System.Collections.ObjectModel;
using System.IO;
using AOSharp.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AOSharp
{
    public class Config
    {
        public ObservableDictionary<string, PluginModel> Plugins { get; set; } =
            new ObservableDictionary<string, PluginModel>();

        public ObservableCollection<Activity> Activities { get; set; } =
            new ObservableCollection<Activity>();

        // Persisted on the next normal save, including an intentional Remove All.
        public bool PluginLibraryInitialized { get; set; }

        [JsonIgnore]
        public string LibraryRecoveryError { get; private set; }

        private string _path;

        public static Config Load(string path)
        {
            var config = File.Exists(path)
                ? JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)) ?? new Config()
                : new Config();
            config._path = path;
            config.Plugins = config.Plugins ?? new ObservableDictionary<string, PluginModel>();
            config.Activities = config.Activities ?? new ObservableCollection<Activity>();
            if (!config.PluginLibraryInitialized)
            {
                try
                {
                    string registryPath = Path.Combine(Path.GetDirectoryName(path), "config.json");
                    if (File.Exists(registryPath))
                    {
                        // Recover only registered DLLs. Legacy character/profile settings are not imported.
                        JToken plugins = JObject.Parse(File.ReadAllText(registryPath))
                            .GetValue(nameof(Plugins), StringComparison.OrdinalIgnoreCase);
                        if (plugins != null && plugins.Type != JTokenType.Null)
                        {
                            var registered = plugins.ToObject<ObservableDictionary<string, PluginModel>>();
                            foreach (var plugin in registered)
                                if (plugin.Value != null && !config.Plugins.ContainsKey(plugin.Key))
                                    config.Plugins.Add(plugin.Key, plugin.Value);
                        }
                    }
                    config.PluginLibraryInitialized = true;
                }
                catch (Exception ex)
                {
                    config.LibraryRecoveryError = "Could not restore the registered plugin list from config.json. " + ex.Message;
                }
            }
            return config;
        }

        public void Save()
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
