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

        public static CommandsModel ReadCommands()
        {
            var commands = ReadConfig<CommandsModel>("commands.json", new CommandsModel());
            if (commands == null)
            {
                commands = new CommandsModel();
            }
            return commands;
        }

        public static bool SaveCommands(CommandsModel commands)
        {
            return SaveConfig<CommandsModel>("commands.json", commands);
        }

        public static TimersModel ReadTimers()
        {
            var timers = ReadConfig<TimersModel>("timers.json", new TimersModel());
            if (timers == null)
            {
                timers = new TimersModel();
            }
            return timers;
        }

        public static bool SaveTimers(TimersModel timers)
        {
            return SaveConfig<TimersModel>("timers.json", timers);
        }

        public static string GetConfigDirectory()
        {
            var current = Utilities.GetDataDirectory();
            var configsPath = Path.Combine(current, ConfigsFolderName);
            Directory.CreateDirectory(configsPath);
            return configsPath;
        }

        /// <summary>
        /// Reads and deserializes the specified file to build the object. If it fails, the fallback object will return instead.
        /// </summary>
        /// <param name="fileName">File name to read, do not include path.</param>
        /// <param name="fallback">Fallback object to use in case reading fails.</param>
        private static T ReadConfig<T>(string fileName, T fallback)
        {
            var filePath = Path.Combine(GetConfigDirectory(), fileName);

            T config = fallback;

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
                config = fallback;
            }

            return config;
        }

        /// <summary>
        /// Serializes the json object to the specified file in the data directory. This will create the file if it doesn't already exist.
        /// </summary>
        /// <param name="fileName">File name only, do not include path.</param>
        /// <param name="model">OBject to save, must be JSON serializable.</param>
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
