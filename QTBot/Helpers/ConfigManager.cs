using Newtonsoft.Json;
using QTBot.Core;
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
    class ConfigManager
    {
        public static ConfigModel ReadConfig()
        {
            var config = ReadConfig<ConfigModel>("config.json");
            if (config == null)
            {
                config = new ConfigModel();
            }
            return config;
        }

        public static TwitchOptionsModel ReadTwitchOptionsConfigs()
        {
            var options = ReadConfig<TwitchOptionsModel>("TwitchOptionsConfigs.json");
            if (options == null)
            {
                options = new TwitchOptionsModel();
            }
            return options;
        }

        public static void SaveTwitchOptionsConfigs(TwitchOptions options)
        {
            SaveConfig<TwitchOptionsModel>("TwitchOptionsConfigs.json", options.GetModel());
        }

        private static T ReadConfig<T>(string fileName)
        {
            var enviroment = Environment.CurrentDirectory;
            var filePath = Path.Combine(enviroment, fileName);

            T config = default(T);

            try
            {
                // Create default if doesn't exist
                if (!File.Exists(filePath))
                {
                    var fileStream = File.Create(filePath);
                    fileStream.Close();
                    config = default(T);
                    string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                    File.WriteAllText(filePath, json);
                }
                else
                {
                    string json = File.ReadAllText(filePath);
                    config = JsonConvert.DeserializeObject<T>(json);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Reading failed for: " + fileName + " with " + e.Message);
                config = default(T);
            }

            return config;
        }

        private static void SaveConfig<T>(string fileName, T model)
        {
            var enviroment = Environment.CurrentDirectory;
            var filePath = Path.Combine(enviroment, fileName);

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
            }
            catch (Exception e)
            {
                Debug.WriteLine("Writing failed for: " + fileName);
            }
        }
    }

    

    public class ConfigModel
    {
        public string StreamerChannelName { get; set; } = "";
        public string StreamerChannelAccessToken { get; set; } = "";
        public string StreamerChannelRefreshToken { get; set; } = "";
        public string StreamerChannelClientId { get; set; } = "";

        public string BotChannelName { get; set; } = "";
        public string BotOAuthToken { get; set; } = "";

        [JsonIgnore]
        public bool IsConfigured =>
            !string.IsNullOrEmpty(StreamerChannelName) &&
            !string.IsNullOrEmpty(StreamerChannelAccessToken) &&
            !string.IsNullOrEmpty(StreamerChannelRefreshToken) &&
            !string.IsNullOrEmpty(StreamerChannelClientId) &&
            !string.IsNullOrEmpty(BotChannelName) &&
            !string.IsNullOrEmpty(BotOAuthToken);

    }

    public class TwitchOptionsModel
    {
        public bool IsRedemptionInChat { get; set; } = false;
        public bool IsRedemptionTagUser { get; set; } = false;
        public string RedemptionTagUser { get; set; } = "";
    }
}
