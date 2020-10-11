using QTBot.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;

namespace QTBot.Core
{
    public class QTCore
    {
        #region Singleton
        private static QTCore instance = null;

        public static QTCore Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new QTCore();
                }
                return instance;
            }
            private set
            {
                if (instance != value)
                {
                    Debug.WriteLine("Error: Trying to create a second instance of TwitchLibWrapper");
                    return;
                }
                instance = value;
            }
        }
        #endregion Singleton

        private TwitchClient client = null;
        private TwitchPubSub pubSubClient = null;
        private TwitchAPI apiClient = null;
        private string channelId = null;
        private string userId = null;

        private JoinedChannel currentChannel = null;

        private QTChatManager chatManager = null;

        private ConfigModel mainConfig = null;
        private TwitchOptions twitchOptions = null;

        public TwitchClient Client => this.client;
        public JoinedChannel CurrentChannel => this.currentChannel;
        public string BotUserName => this.mainConfig?.BotChannelName ?? "<Invalid Value>";
        public string CurrentChannelName => this.mainConfig?.StreamerChannelName ?? "<Invalid Value>";

        public bool IsConfigured => mainConfig?.IsConfigured ?? false;
        public TwitchOptions TwitchOptions => this.twitchOptions;

        public event EventHandler OnConnected;
        public event EventHandler OnDisonnected;

        public QTCore()
        {
            LoadConfigs();
        }

        public void LoadConfigs()
        {
            this.mainConfig = ConfigManager.ReadConfig();
            this.twitchOptions = new TwitchOptions(ConfigManager.ReadTwitchOptionsConfigs());
        }

        public async Task Setup()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(this.mainConfig.BotChannelName, this.mainConfig.BotOAuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            // Setup Twitch client
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            this.client = new TwitchClient(customClient);
            this.client.Initialize(credentials, this.mainConfig.StreamerChannelName);

            this.client.OnLog += Client_OnLog;
            this.client.OnJoinedChannel += Client_OnJoinedChannel;
            this.client.OnMessageReceived += Client_OnMessageReceived;
            this.client.OnNewSubscriber += Client_OnNewSubscriber;
            this.client.OnConnected += Client_OnConnected;
            this.client.OnDisconnected += Client_OnDisconnected;

            this.client.Connect();

            // Setup QT chat manager
            this.chatManager = new QTChatManager(this.client);

            // Setup API client
            this.apiClient = new TwitchAPI();
            this.apiClient.Settings.ClientId = this.mainConfig.StreamerChannelClientId;
            this.apiClient.Settings.AccessToken = this.mainConfig.StreamerChannelAccessToken;
            var authChannel = await this.apiClient.V5.Channels.GetChannelAsync(this.apiClient.Settings.AccessToken);
            this.channelId = authChannel.Id;
            var authUser = await this.apiClient.V5.Users.GetUserAsync(this.apiClient.Settings.AccessToken);
            this.userId = authUser.Id;

            // Setup PubSub client
            this.pubSubClient = new TwitchPubSub();
            this.pubSubClient.OnPubSubServiceConnected += PubSubClient_OnPubSubServiceConnected;
            this.pubSubClient.OnListenResponse += PubSubClient_OnListenResponse;
            this.pubSubClient.OnEmoteOnly += PubSubClient_OnEmoteOnly;
            this.pubSubClient.OnEmoteOnlyOff += PubSubClient_OnEmoteOnlyOff;
            this.pubSubClient.OnRewardRedeemed += PubSubClient_OnRewardRedeemed;
            this.pubSubClient.OnStreamUp += PubSubClient_OnStreamUp;
            this.pubSubClient.OnStreamDown += PubSubClient_OnStreamDown;
            this.pubSubClient.OnBitsReceived += PubSubClient_OnBitsReceived;
            this.pubSubClient.OnChannelSubscription += PubSubClient_OnChannelSubscription;
            this.pubSubClient.OnHost += PubSubClient_OnHost;

            this.pubSubClient.ListenToRewards(this.channelId);
            this.pubSubClient.ListenToFollows(this.channelId);
            this.pubSubClient.ListenToBitsEvents(this.channelId);
            this.pubSubClient.ListenToChatModeratorActions(this.userId, this.channelId);
            this.pubSubClient.ListenToRaid(this.channelId);
            this.pubSubClient.ListenToSubscriptions(this.channelId);

            this.pubSubClient.Connect();
        }

        public void Disconnect()
        {
            this.pubSubClient.Disconnect();
            this.client.Disconnect();
        }

        public void SetupTwitchOptions(TwitchOptions options)
        {
            this.twitchOptions = options;
            ConfigManager.SaveTwitchOptionsConfigs(this.twitchOptions);
        }

        #region Client Events
        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            this.chatManager.ToggleChat(true);
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            this.chatManager.ToggleChat(false);
            this.OnDisonnected?.Invoke(sender, null);
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {

        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {

        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            this.currentChannel = new JoinedChannel(e.Channel);
            this.OnConnected?.Invoke(sender, null);
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Debug.WriteLine("Client log " + e.Data);
        }
      
        #endregion Client Events

        #region PubSub Events
        private void PubSubClient_OnHost(object sender, TwitchLib.PubSub.Events.OnHostArgs e)
        {
        }

        private void PubSubClient_OnChannelSubscription(object sender, TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
        }

        private void PubSubClient_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
        }

        private void PubSubClient_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {

        }

        private void PubSubClient_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {

        }

        private void PubSubClient_OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            this.chatManager.QueueRedeemAlert(e.RewardTitle, e.DisplayName);
        }

        private void PubSubClient_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (!e.Successful)
            {

            }
        }

        private void PubSubClient_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            this.pubSubClient.SendTopics(this.apiClient.Settings.AccessToken);
        }

        private void PubSubClient_OnEmoteOnlyOff(object sender, TwitchLib.PubSub.Events.OnEmoteOnlyOffArgs e)
        {

        }

        private void PubSubClient_OnEmoteOnly(object sender, TwitchLib.PubSub.Events.OnEmoteOnlyArgs e)
        {

        }
        #endregion PubSub Events

        public void TestMessage()
        {
            this.client.SendMessage(this.currentChannel, "This is a test message!");
        }

        public void TestRedemption1()
        {
            this.chatManager.QueueRedeemAlert("FakeRedeem1", "SomeFakeUser1");
        }

        public void TestRedemption2()
        {
            this.chatManager.QueueRedeemAlert("FakeRedeem2", "SomeFakeUser2");
        }
    }
}
