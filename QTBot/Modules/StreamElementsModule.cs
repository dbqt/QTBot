using Newtonsoft.Json;
using QTBot.Helpers;
using QTBot.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace QTBot.Modules
{
    public class StreamElementsModule : Singleton<StreamElementsModule>
    {
        private const string BaseUrl = "https://api.streamelements.com/kappa/v2";

        private HttpClient httpClient;

        public bool IsSetup => Config.IsStreamElementsConfigured;

        public ConfigModel Config { get; set; }

        public void Initialize(ConfigModel config)
        {
            Config = config;
            if (IsSetup)
            {
                httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Config.StreamElementsJWTToken); ;
            }
        }

        public async Task<int> GetPoints(string username)
        {
            int points = -1;
            if (IsSetup)
            {
                string url = $"{BaseUrl}/points/{Config.StreamElementsChannelId}/{username}";
                var response = await httpClient.GetAsync(url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var pointsResponse = JsonConvert.DeserializeObject<GetPointsResponse>(json);
                    if (pointsResponse != null)
                    {
                        points = pointsResponse.Points;
                    }
                }
            }

            return points;
        }

        public async Task<int> UpdatePoints(string username, int points)
        {
            int finalPoints = -1;
            if (IsSetup)
            {
                string url = $"{BaseUrl}/points/{Config.StreamElementsChannelId}/{username}/{points}";
                var response = await httpClient.PutAsync(url, null);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var pointsResponse = JsonConvert.DeserializeObject<UpdatePointsResponse>(json);
                    if (pointsResponse != null)
                    {
                        finalPoints = pointsResponse.NewAmount;
                    }
                }
            }

            return finalPoints;
        }

        public async Task<int> GetCounter(string counterName)
        {
            int counter = -1;
            if (IsSetup)
            {
                string url = $"{BaseUrl}/bot/{Config.StreamElementsChannelId}/counters/{counterName}";
                var response = await httpClient.GetAsync(url);
                if (response != null && response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var counterResponse = JsonConvert.DeserializeObject<GetCounterResponse>(json);
                    if (counterResponse != null)
                    {
                        counter = counterResponse.Value;
                    }
                }
            }

            return counter;
        }

        private class GetPointsResponse
        {
            [JsonProperty("channel")]
            public string Channel { get; set; } = "";

            [JsonProperty("username")]
            public string Username { get; set; } = "";

            [JsonProperty("points")]
            public int Points { get; set; } = -1;
        }

        private class UpdatePointsResponse
        {
            [JsonProperty("channel")]
            public string Channel { get; set; } = "";

            [JsonProperty("username")]
            public string Username { get; set; } = "";

            [JsonProperty("amount")]
            public int Amount { get; set; } = -1;

            [JsonProperty("newAmount")]
            public int NewAmount { get; set; } = -1;
        }

        private class GetCounterResponse
        {
            [JsonProperty("counter")]
            public string Counter { get; set; } = "";

            [JsonProperty("value")]
            public int Value { get; set; } = -1;
        }
    }
}
