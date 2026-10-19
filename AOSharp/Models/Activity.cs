using System;
using System.Collections.Generic;

namespace AOSharp.Models
{
    public class Activity
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public string Name { get; set; }
        public List<string> PluginKeys { get; set; } = new List<string>();
        public Dictionary<string, List<string>> CharacterPlugins { get; set; } =
            new Dictionary<string, List<string>>();

        public bool IsEnabled(string character, string key)
        {
            return CharacterPlugins.TryGetValue(character, out var selected) && selected.Contains(key);
        }

        public void SetEnabled(string character, string key, bool enabled)
        {
            if (!CharacterPlugins.TryGetValue(character, out var selected))
            {
                if (!enabled) return;
                CharacterPlugins[character] = selected = new List<string>();
            }

            if (enabled)
            {
                if (!selected.Contains(key)) selected.Add(key);
            }
            else
                selected.Remove(key);
        }

        public void RemovePlugin(string key)
        {
            PluginKeys.Remove(key);
            foreach (var selected in CharacterPlugins.Values)
                selected.Remove(key);
        }
    }
}
