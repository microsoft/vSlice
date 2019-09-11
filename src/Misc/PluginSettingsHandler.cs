using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSlice
{
    public class PluginSettingsHandler
    {
        // --------------------------------------------------------------------------------------
        /// <summary>
        /// Get the settings for all plugins
        /// </summary>
        // --------------------------------------------------------------------------------------
        private Dictionary<string, string> GetPluginSettings()
        {
            var pluginSettings = JsonConvert
                .DeserializeObject<Dictionary<string, string>>(
                    Properties.Settings.Default.PluginSettings);

            if (pluginSettings == null) pluginSettings = new Dictionary<string, string>();
            return pluginSettings;
        }

        // --------------------------------------------------------------------------------------
        /// <summary>
        /// Get my plugin settings from the app settings
        /// </summary>
        // --------------------------------------------------------------------------------------
        public T GetSettings<T>(string name) where T : class
        {
            if (GetPluginSettings().TryGetValue(name, out var rawSettings))
            {
                return JsonConvert.DeserializeObject<T>(rawSettings);
            }
            else return null;
        }

        // --------------------------------------------------------------------------------------
        /// <summary>
        /// Save settings object with the app settings
        /// </summary>
        // --------------------------------------------------------------------------------------
        public void SaveSettings(string name, object settings)
        {
            var pluginSettings = GetPluginSettings();
            pluginSettings[name] = JsonConvert.SerializeObject(settings);
            Properties.Settings.Default.PluginSettings = JsonConvert.SerializeObject(pluginSettings);
            Properties.Settings.Default.Save();
        }
    }
}
