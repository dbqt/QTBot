using Newtonsoft.Json;
using QTBot.Core;
using QTBot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTBot.Helpers
{
    public class ConfigManager
    {
        private const string ConfigsFolderName = "Configs";

        public static ConfigModel ReadConfig()
        {
            var config = ReadConfig<ConfigModel>("config.json", new ConfigModel());
            if (config == null)
            {
                config = new ConfigModel();
            }
            return config;
        }

        public static bool SaveConfig(ConfigModel config)
        {
            return SaveConfig<ConfigModel>("config.json", config);
        }

        public static TwitchOptionsModel ReadTwitchOptionsConfigs()
        {
            var options = ReadConfig<TwitchOptionsModel>("TwitchOptionsConfigs.json", new TwitchOptionsModel());
            if (options == null)
            {
                options = new TwitchOptionsModel();
            }
            return options;
        }

        public static bool SaveTwitchOptionsConfigs(TwitchOptions options)
        {
            return SaveConfig<TwitchOptionsModel>("TwitchOptionsConfigs.json", options.GetModel());
        }

        public static string GetConfigDirectory()
        {
            var current = Environment.CurrentDirectory;
            var configsPath = Path.Combine(current, ConfigsFolderName);
            Directory.CreateDirectory(configsPath);
            return configsPath;
        }

        private static T ReadConfig<T>(string fileName, T defaultObject)
        {
            var filePath = Path.Combine(GetConfigDirectory(), fileName);

            T config = defaultObject;

            try
            {
                // Create default if doesn't exist
                if (!File.Exists(filePath))
                {
                    var fileStream = File.Create(filePath);
                    fileStream.Close();
                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
                else
                {
                    string json = File.ReadAllText(filePath);
                    config = JsonConvert.DeserializeObject<T>(json);

                    // Update file in case format is different
                    if (config != null)
                    {
                        json = JsonConvert.SerializeObject(config, Formatting.Indented);
                        File.WriteAllText(filePath, json);
                    }
                }
            }
            catch (Exception e)
            {
                Utilities.ShowMessage("Reading failed for: " + fileName + " with " + e.Message);
                config = defaultObject;
            }

            return config;
        }

        private static bool SaveConfig<T>(string fileName, T model)
        {
            var filePath = Path.Combine(GetConfigDirectory(), fileName);

            try
            {
                // Create default if doesn't exist
                if (!File.Exists(filePath))
                {
                    var fileStream = File.Create(filePath);
                    fileStream.Close();                
                }

                string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception e)
            {
                Utilities.ShowMessage("Save failed for: " + fileName + " with " + e.Message);
            }

            return false;
        }
    }
}
