using System.Collections.ObjectModel;
using System.IO;
using AOSharp.Models;
using Newtonsoft.Json;

namespace AOSharp
{
    public class Config
    {
        public ObservableDictionary<string, PluginModel> Plugins { get; set; } =
            new ObservableDictionary<string, PluginModel>();

        public ObservableCollection<Activity> Activities { get; set; } =
            new ObservableCollection<Activity>();

        private string _path;

        public static Config Load(string path)
        {
            var config = File.Exists(path)
                ? JsonConvert.DeserializeObject<Config>(File.ReadAllText(path)) ?? new Config()
                : new Config();
            config._path = path;
            return config;
        }

        public void Save()
        {
            File.WriteAllText(_path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}
