using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTBot.Models
{
    public class ConfigModel
    {
        // Channel to watch
        public string StreamerChannelName { get; set; } = "";
        public string StreamerChannelAccessToken { get; set; } = "";
        public string StreamerChannelRefreshToken { get; set; } = "";
        public string StreamerChannelClientId { get; set; } = "";

        // Channel to use as bot
        public string BotChannelName { get; set; } = "";
        public string BotOAuthToken { get; set; } = "";

        // StreamElementsAccess
        public string StreamElementsChannelId { get; set; } = "";
        public string StreamElementsJWTToken { get; set; } = "";

        [JsonIgnore]
        public bool IsConfigured =>
            !string.IsNullOrEmpty(StreamerChannelName) &&
            !string.IsNullOrEmpty(StreamerChannelAccessToken) &&
            !string.IsNullOrEmpty(StreamerChannelRefreshToken) &&
            !string.IsNullOrEmpty(StreamerChannelClientId) &&
            !string.IsNullOrEmpty(BotChannelName) &&
            !string.IsNullOrEmpty(BotOAuthToken);

        [JsonIgnore]
        public bool IsStreamElementsConfigured =>
            !string.IsNullOrEmpty(StreamElementsChannelId) &&
            !string.IsNullOrEmpty(StreamElementsJWTToken);
    }
}
