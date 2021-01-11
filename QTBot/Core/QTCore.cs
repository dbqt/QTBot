using QTBot.Helpers;
using QTBot.Models;
using QTBot.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using TwitchLib.Api;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;

namespace QTBot.Core
{
    public class QTCore : Singleton<QTCore>
    {
        private readonly AuthScopes[] scopes = new AuthScopes[]
        {
            AuthScopes.Channel_Read,
            AuthScopes.Channel_Stream,
            AuthScopes.Channel_Subscriptions,
            AuthScopes.Channel_Check_Subscription,
            AuthScopes.Chat_Login,
            AuthScopes.Viewing_Activity_Read,
            AuthScopes.User_Read,
            AuthScopes.User_Subscriptions
        };

        private TwitchClient client = null;
        private TwitchPubSub pubSubClient = null;
        private TwitchAPI apiClient = null;
        private string channelId = null;
        private string userId = null;

        private JoinedChannel currentChannel = null;

        private QTCommandsManager commandsManager = null;
        private QTTimersManager timersManager = null;

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
            try
            {
                var logPath = Path.Combine(Environment.CurrentDirectory, "Logs");
                Directory.CreateDirectory(logPath);
                var logFilePath = Path.Combine(logPath, "logs-" + DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss-tt") + ".txt");
                var fileStream = File.Create(logFilePath);
                fileStream.Close();

                TextWriterTraceListener[] listeners = new TextWriterTraceListener[]
                {
                    new TextWriterTraceListener(logFilePath),
                    new TextWriterTraceListener(Console.Out)
                };

                Trace.Listeners.AddRange(listeners);
                Trace.AutoFlush = true;

                while (Directory.GetFiles(logPath).Length > 10)
                {
                    FileSystemInfo fileInfo = new DirectoryInfo(logPath).GetFileSystemInfos().OrderBy(fi => fi.CreationTime).First();
                    Trace.WriteLine("Deleting old log: " + fileInfo.FullName);
                    File.Delete(fileInfo.FullName);
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.StackTrace);
            }

            Trace.WriteLine("QT LOGS STARTING");

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
            SetupClientListeners();

            this.client.Connect();

            // Setup QT chat manager
            QTChatManager.Instance.Initialize(this.client);

            // Setup QT commands manager
            this.commandsManager = new QTCommandsManager();

            // Setup QT timers manager
            this.timersManager = new QTTimersManager();

            // Setup API client
            this.apiClient = new TwitchAPI();

            // Auth user
            this.apiClient.Settings.ClientId = this.mainConfig.StreamerChannelClientId;
            this.apiClient.Settings.AccessToken = this.mainConfig.StreamerChannelAccessToken;

            var credentialResponse = await this.apiClient.ThirdParty.AuthorizationFlow.CheckCredentialsAsync();
            // Current token not working
            if (!credentialResponse.Result)
            {
                Trace.WriteLine(credentialResponse.ResultMessage);
                // Try refresh
                var refreshResponse = this.apiClient.ThirdParty.AuthorizationFlow.RefreshToken(this.mainConfig.StreamerChannelRefreshToken);
                if (!string.IsNullOrEmpty(refreshResponse.Token) && !string.IsNullOrEmpty(refreshResponse.Refresh))
                {
                    // Update to new tokens from the refresh
                    this.mainConfig.StreamerChannelAccessToken = refreshResponse.Token;
                    this.mainConfig.StreamerChannelRefreshToken = refreshResponse.Refresh;
                    ConfigManager.SaveConfig(this.mainConfig);
                    this.apiClient.Settings.ClientId = this.mainConfig.StreamerChannelClientId;
                    this.apiClient.Settings.AccessToken = this.mainConfig.StreamerChannelAccessToken;

                    // Check with refreshed tokens
                    credentialResponse = await this.apiClient.ThirdParty.AuthorizationFlow.CheckCredentialsAsync();
                    if (!credentialResponse.Result)
                    {
                        Utilities.ShowMessage("QTBot couldn't authenticate.\nYou'll need to https://twitchtokengenerator.com/ \nGet new streamer tokens, add them to the config file and reload the configuration files.", "Error authenticating");
                        return;
                    }
                }
                else
                {
                    Utilities.ShowMessage("QTBot couldn't authenticate.\nYou'll need to https://twitchtokengenerator.com/ \nGet new streamer tokens, add them to the config file and reload the configuration files.", "Error authenticating");
                    return;
                }
            }

            // Grab channel info
            var authChannel = await this.apiClient.V5.Channels.GetChannelAsync(this.apiClient.Settings.AccessToken);
            this.channelId = authChannel.Id;
            var authUser = await this.apiClient.V5.Users.GetUserAsync(this.apiClient.Settings.AccessToken);
            this.userId = authUser.Id;

            // Setup PubSub client
            this.pubSubClient = new TwitchPubSub();
            SetupPubSubListeners();

            this.pubSubClient.Connect();

            // Setup StreamElements
            if (this.mainConfig.IsStreamElementsConfigured)
            {
                StreamElementsModule.Instance.Initialize(this.mainConfig);
            }
        }

        private void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            Trace.WriteLine("Raid notification with display name: " + e.RaidNotification.DisplayName);
            Trace.WriteLine("Raid notification with channel name: " + e.Channel);
            Trace.WriteLine("Raid notification with MsgParamDisplayName: " + e.RaidNotification.MsgParamDisplayName);
            Trace.WriteLine("Raid notification with MsgParamViewerCount: " + e.RaidNotification.MsgParamViewerCount);
            Trace.WriteLine("Raid notification with MsgParamLogin: " + e.RaidNotification.MsgParamLogin);

            if (this.TwitchOptions.IsAutoShoutOutHost)
            {
                QTChatManager.Instance.SendInstantMessage($"!so {e.RaidNotification.DisplayName}");
            }
        }

        private void Client_OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            Trace.WriteLine("BeingHosted notification with channel" + e.BeingHostedNotification.Channel);
            Trace.WriteLine("BeingHosted notification with HostedByChannel" + e.BeingHostedNotification.HostedByChannel);
            Trace.WriteLine("BeingHosted notification with Viewers" + e.BeingHostedNotification.Viewers);
            Trace.WriteLine("BeingHosted notification with BotUsername" + e.BeingHostedNotification.BotUsername);
        }

        private void Client_OnHostingStarted(object sender, OnHostingStartedArgs e)
        {
            Trace.WriteLine("HostingStarted notification with HostingChannel " + e.HostingStarted.HostingChannel);
            Trace.WriteLine("HostingStarted notification with TargetChannel " + e.HostingStarted.TargetChannel);
            Trace.WriteLine("HostingStarted notification with Viewers " + e.HostingStarted.Viewers);
        }

        public void Disconnect()
        {
            RemovePubSubListeners();
            RemoveClientListeners();
            try
            {
                this.pubSubClient.Disconnect();
                this.client.Disconnect();
            }
            catch (Exception e)
            {
                Trace.WriteLine("QTCore.Disconnect exception: " + e.StackTrace);
            }
        }

        public void SetupTwitchOptions(TwitchOptions options)
        {
            this.twitchOptions = options;
            if (ConfigManager.SaveTwitchOptionsConfigs(this.twitchOptions))
            {
                Utilities.ShowMessage("Twitch options saved!");
            }
        }

        #region Core Functionality

        private async void HandleCommand(string command, IEnumerable<string> args, string username)
        {
            var result = await this.commandsManager.ProcessCommand(command, args, username);
            if (!string.IsNullOrEmpty(result))
            {
                QTChatManager.Instance.SendInstantMessage(result);
            }
        }
        #endregion Core Functionality

        #region Client Events
        private void SetupClientListeners()
        {
            this.client.OnLog += Client_OnLog;
            this.client.OnJoinedChannel += Client_OnJoinedChannel;
            this.client.OnMessageReceived += Client_OnMessageReceived;
            this.client.OnNewSubscriber += Client_OnNewSubscriber;
            this.client.OnConnected += Client_OnConnected;
            this.client.OnDisconnected += Client_OnDisconnected;
            this.client.OnHostingStarted += Client_OnHostingStarted;
            this.client.OnBeingHosted += Client_OnBeingHosted;
            this.client.OnRaidNotification += Client_OnRaidNotification;
        }

        private void RemoveClientListeners()
        {
            this.client.OnLog -= Client_OnLog;
            this.client.OnJoinedChannel -= Client_OnJoinedChannel;
            this.client.OnMessageReceived -= Client_OnMessageReceived;
            this.client.OnNewSubscriber -= Client_OnNewSubscriber;
            this.client.OnConnected -= Client_OnConnected;
            this.client.OnDisconnected -= Client_OnDisconnected;
            this.client.OnHostingStarted -= Client_OnHostingStarted;
            this.client.OnBeingHosted -= Client_OnBeingHosted;
            this.client.OnRaidNotification -= Client_OnRaidNotification;
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            QTChatManager.Instance.ToggleChat(true);

            this.timersManager.StartTimers();
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            QTChatManager.Instance.ToggleChat(false);

            this.timersManager.StopTimers();

            this.OnDisonnected?.Invoke(sender, null);
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            Trace.WriteLine("Client_OnNewSubscriber " + e.Subscriber.DisplayName);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var msg = e.ChatMessage.Message;
            // Is command?
            if (msg.StartsWith("!"))
            {
                var parts = msg.Split(' ');
                HandleCommand(parts[0], parts.Skip(1), e.ChatMessage.Username);
            }
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            this.currentChannel = new JoinedChannel(e.Channel);
            this.OnConnected?.Invoke(sender, null);
            QTChatManager.Instance.SendInstantMessage("hai hai, I am ready to go!");
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Trace.WriteLine("Client_OnLog: " + e.Data);
        }
      
        #endregion Client Events

        #region PubSub Events
        private void SetupPubSubListeners()
        {
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
            this.pubSubClient.OnRaidGo += PubSubClient_OnRaidGo;
            this.pubSubClient.OnRaidUpdate += PubSubClient_OnRaidUpdate;
            this.pubSubClient.OnRaidUpdateV2 += PubSubClient_OnRaidUpdateV2;

            this.pubSubClient.ListenToRewards(this.channelId);
            this.pubSubClient.ListenToFollows(this.channelId);
            this.pubSubClient.ListenToBitsEvents(this.channelId);
            this.pubSubClient.ListenToChatModeratorActions(this.userId, this.channelId);
            this.pubSubClient.ListenToRaid(this.channelId);
            this.pubSubClient.ListenToSubscriptions(this.channelId);
        }

        private void RemovePubSubListeners()
        {
            this.pubSubClient.OnPubSubServiceConnected -= PubSubClient_OnPubSubServiceConnected;
            this.pubSubClient.OnListenResponse -= PubSubClient_OnListenResponse;
            this.pubSubClient.OnEmoteOnly -= PubSubClient_OnEmoteOnly;
            this.pubSubClient.OnEmoteOnlyOff -= PubSubClient_OnEmoteOnlyOff;
            this.pubSubClient.OnRewardRedeemed -= PubSubClient_OnRewardRedeemed;
            this.pubSubClient.OnStreamUp -= PubSubClient_OnStreamUp;
            this.pubSubClient.OnStreamDown -= PubSubClient_OnStreamDown;
            this.pubSubClient.OnBitsReceived -= PubSubClient_OnBitsReceived;
            this.pubSubClient.OnChannelSubscription -= PubSubClient_OnChannelSubscription;
            this.pubSubClient.OnHost -= PubSubClient_OnHost;
            this.pubSubClient.OnRaidGo -= PubSubClient_OnRaidGo;
            this.pubSubClient.OnRaidUpdate -= PubSubClient_OnRaidUpdate;
            this.pubSubClient.OnRaidUpdateV2 -= PubSubClient_OnRaidUpdateV2;
        }

        private void PubSubClient_OnHost(object sender, TwitchLib.PubSub.Events.OnHostArgs e)
        {
            Trace.WriteLine("PubSubClient_OnHost");
        }

        private void PubSubClient_OnChannelSubscription(object sender, TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
            Trace.WriteLine("PubSubClient_OnChannelSubscription " + e.Subscription.DisplayName);
        }

        private void PubSubClient_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            Trace.WriteLine($"PubSubClient_OnBitsReceived BitsUsed: {e.BitsUsed}, TotalBitsUsed: {e.TotalBitsUsed}, message: {e.ChatMessage}");
        }

        private void PubSubClient_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            Trace.WriteLine("PubSubClient_OnStreamDown");
        }

        private void PubSubClient_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            Trace.WriteLine("PubSubClient_OnStreamUp");
        }

        private void PubSubClient_OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            Trace.WriteLine($"PubSubClient_OnRewardRedeemed with RewardTitle: {e.RewardTitle}");
            Trace.WriteLine($"PubSubClient_OnRewardRedeemed with RewardCost: {e.RewardCost}");
            Trace.WriteLine($"PubSubClient_OnRewardRedeemed with RewardPrompt: {e.RewardPrompt}");
            Trace.WriteLine($"PubSubClient_OnRewardRedeemed with Message: {e.Message}");
            Trace.WriteLine($"PubSubClient_OnRewardRedeemed with Status: {e.Status}");

            if (e.Status.Equals("UNFULFILLED")) // FULFILLED
            {
                QTChatManager.Instance.QueueRedeemAlert(e.RewardTitle, e.DisplayName);
            }
        }

        private void PubSubClient_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            Trace.WriteLine("PubSubClient_OnListenResponse was successful: " + e.Successful);
        }

        private void PubSubClient_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            Trace.WriteLine("PubSubClient_OnPubSubServiceConnected");
            this.pubSubClient.SendTopics(this.apiClient.Settings.AccessToken);
        }

        private void PubSubClient_OnEmoteOnlyOff(object sender, TwitchLib.PubSub.Events.OnEmoteOnlyOffArgs e)
        {
            Trace.WriteLine("PubSubClient_OnEmoteOnlyOff");
        }

        private void PubSubClient_OnEmoteOnly(object sender, TwitchLib.PubSub.Events.OnEmoteOnlyArgs e)
        {
            Trace.WriteLine("PubSubClient_OnEmoteOnly");
        }

        private void PubSubClient_OnRaidUpdateV2(object sender, TwitchLib.PubSub.Events.OnRaidUpdateV2Args e)
        {
            Trace.WriteLine("PubSubClient_OnRaidUpdateV2");
        }

        private void PubSubClient_OnRaidUpdate(object sender, TwitchLib.PubSub.Events.OnRaidUpdateArgs e)
        {
            Trace.WriteLine("PubSubClient_OnRaidUpdate");
        }

        private void PubSubClient_OnRaidGo(object sender, TwitchLib.PubSub.Events.OnRaidGoArgs e)
        {
            Trace.WriteLine("PubSubClient_OnRaidGo");
        }
        #endregion PubSub Events

        #region Test
        public void TestMessage()
        {
            this.client.SendMessage(this.currentChannel, "Hai, I'm connected!");
        }

        public void TestRedemption1()
        {
            QTChatManager.Instance.QueueRedeemAlert("FakeRedeem1", "SomeFakeUser1");
        }

        public void TestRedemption2()
        {
            QTChatManager.Instance.QueueRedeemAlert("FakeRedeem2", "SomeFakeUser2");
        }
        #endregion Test
    }
}
