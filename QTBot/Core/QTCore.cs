using QTBot.Core;
using QTBot.CustomDLLIntegration;
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
using Microsoft.Extensions.Logging;

namespace QTBot
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

        // Modules
        private QTCommandsManager commandsManager = null;
        private QTTimersManager timersManager = null;
        private QTEventsManager eventsManager = null;

        public QTCommandsManager CommandsManager => this.commandsManager;
        public QTTimersManager TimersManager => this.timersManager;
        public QTEventsManager EventsManager => this.eventsManager;

        // Configurations
        private ConfigModel mainConfig = null;
        private string channelId = null;
        private string userId = null;
        private JoinedChannel currentChannel = null;

        // Twitch client
        private TwitchClient client = null;
        public TwitchClient Client => this.client;
        public JoinedChannel CurrentChannel => this.currentChannel;
        public string BotUserName => this.mainConfig?.BotChannelName ?? "<Invalid Value>";
        public string CurrentChannelName => this.mainConfig?.StreamerChannelName ?? "<Invalid Value>";
        public bool IsMainConfigLoaded => mainConfig?.IsConfigured ?? false;

        // Twitch options
        private TwitchOptions twitchOptions = null;
        public TwitchOptions TwitchOptions => this.twitchOptions;
        public bool IsRedemptionAlertOn => this.TwitchOptions.IsRedemptionTagUser && !string.IsNullOrEmpty(this.TwitchOptions.RedemptionTagUser);

        // TwitchLibs stuff
        private TwitchPubSub pubSubClient = null;
        private TwitchAPI apiClient = null;

        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;

        public QTCore()
        {
            SetupLogging();
            LoadConfigs();
        }

        #region Core Functionality
        public void LoadConfigs()
        {
            this.mainConfig = ConfigManager.ReadConfig();
            this.twitchOptions = new TwitchOptions(ConfigManager.ReadTwitchOptionsConfigs());
        }

        /// <summary>
        /// Initial setup of the services
        /// </summary>
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
            this.Client.Initialize(credentials, this.mainConfig.StreamerChannelName);

            // Setup core listeners separately
            this.Client.OnConnected += Client_OnConnected;
            this.Client.OnDisconnected += Client_OnDisconnected;

            // Setup all other listeners
            SetupClientEventListeners();

            this.Client.Connect();

            // Setup QT chat manager
            QTChatManager.Instance.Initialize(this.Client);

            // Setup modules
            SetupModules();

            // Setup API client
            this.apiClient = new TwitchAPI();

            // Auth user
            this.apiClient.Settings.ClientId = this.mainConfig.StreamerChannelClientId;
            this.apiClient.Settings.AccessToken = this.mainConfig.StreamerChannelAccessToken;

            var credentialResponse = await this.apiClient.ThirdParty.AuthorizationFlow.CheckCredentialsAsync();
            // Current token not working
            if (!credentialResponse.Result)
            {
                Utilities.Log(LogLevel.Information ,credentialResponse.ResultMessage);
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

            // Setup core listeners separately
            this.pubSubClient.OnPubSubServiceClosed += PubSubClient_OnPubSubServiceClosed;
            this.pubSubClient.OnPubSubServiceError += PubSubClient_OnPubSubServiceError;
            this.pubSubClient.OnPubSubServiceConnected += PubSubClient_OnPubSubServiceConnected;

            // Setup all other listeners
            SetupPubSubListeners();

            this.pubSubClient.Connect();

            // Setup StreamElements if configured
            if (this.mainConfig.IsStreamElementsConfigured)
            {
                StreamElementsModule.Instance.Initialize(this.mainConfig);
            }

            // Check Dlls folder and DLLs settings for additional DLLIntegration and startup
            string integrationFolderPath = Path.Combine(Utilities.GetDataDirectory(), "DLLIntegration");
            IntegrationHelper.SetupIntegrationhelper(integrationFolderPath, Path.Combine(integrationFolderPath, "DLLIntegrationSetup.json"));
            IntegrationHelper.SetupDLLIntegration();
        }

        /// <summary>
        /// Disconnect Twitch services.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                RemovePubSubListeners();
                this.pubSubClient.Disconnect();
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, "QTCore.Disconnect PubSub exception: " + e.StackTrace);
            }

            try
            {
                RemoveClientEventListeners();
                this.Client.Disconnect();
            }
            catch(Exception e)
            {
                Utilities.Log(LogLevel.Error, "QTCore.Disconnect Client exception: " + e.StackTrace);
            }
            finally
            {
                this.Client.OnConnected -= Client_OnConnected;
                this.Client.OnDisconnected -= Client_OnDisconnected;

                this.pubSubClient.OnPubSubServiceClosed -= PubSubClient_OnPubSubServiceClosed;
                this.pubSubClient.OnPubSubServiceError -= PubSubClient_OnPubSubServiceError;
                this.pubSubClient.OnPubSubServiceConnected -= PubSubClient_OnPubSubServiceConnected;
            }
        }

        /// <summary>
        /// [DEPRECATED] Saves the Twitch options and shows a dialog if successful.
        /// </summary>
        public void SetupTwitchOptions(TwitchOptions options)
        {
            this.twitchOptions = options;
            if (ConfigManager.SaveTwitchOptionsConfigs(this.twitchOptions))
            {
                Utilities.ShowMessage("Twitch options saved!");
            }
        }

        private async void HandleCommand(string command, IEnumerable<string> args, OnMessageReceivedArgs messageArgs)
        {
            var result = await this.commandsManager?.ProcessCommand(command, args, messageArgs);
            if (!string.IsNullOrEmpty(result))
            {
                QTChatManager.Instance.SendInstantMessage(result);
            }
        }

        /// <summary>
        /// Sets up the logging system into log files and clean up old logs if there are more than 10 of them.
        /// </summary>
        private void SetupLogging()
        {
            try
            {
                var logPath = Path.Combine(Utilities.GetDataDirectory(), "Logs");
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

                // Delete old logs
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

            Trace.WriteLine("QT LOGS STARTING!");
        }

        private void SetupModules()
        {
            // Setup QT commands manager
            this.commandsManager = new QTCommandsManager();

            // Setup QT timers manager
            this.timersManager = new QTTimersManager();

            // Setup QT events manager
            this.eventsManager = new QTEventsManager();
        }
        #endregion Core Functionality

        #region Client Events

        private void SetupClientEventListeners()
        {
            this.Client.OnLog += Client_OnLog;
            this.Client.OnJoinedChannel += Client_OnJoinedChannel;
            this.Client.OnMessageReceived += Client_OnMessageReceived;
            this.Client.OnNewSubscriber += Client_OnNewSubscriber;
            this.Client.OnHostingStarted += Client_OnHostingStarted;
            this.Client.OnBeingHosted += Client_OnBeingHosted;
            this.Client.OnRaidNotification += Client_OnRaidNotification;
        }

        private void RemoveClientEventListeners()
        {
            this.Client.OnLog -= Client_OnLog;
            this.Client.OnJoinedChannel -= Client_OnJoinedChannel;
            this.Client.OnMessageReceived -= Client_OnMessageReceived;
            this.Client.OnNewSubscriber -= Client_OnNewSubscriber;
            this.Client.OnHostingStarted -= Client_OnHostingStarted;
            this.Client.OnBeingHosted -= Client_OnBeingHosted;
            this.Client.OnRaidNotification -= Client_OnRaidNotification;
        }

        private void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            Utilities.Log(LogLevel.Information ,"Raid notification with display name: " + e.RaidNotification.DisplayName);
            Utilities.Log(LogLevel.Information, "Raid notification with channel name: " + e.Channel);
            Utilities.Log(LogLevel.Information, "Raid notification with MsgParamDisplayName: " + e.RaidNotification.MsgParamDisplayName);
            Utilities.Log(LogLevel.Information, "Raid notification with MsgParamViewerCount: " + e.RaidNotification.MsgParamViewerCount);
            Utilities.Log(LogLevel.Information, "Raid notification with MsgParamLogin: " + e.RaidNotification.MsgParamLogin);

            if (this.TwitchOptions.IsAutoShoutOutHost)
            {
                _ = QTChatManager.Instance.SendMessage($"!so {e.RaidNotification.DisplayName}", 5000);
            }

            this.eventsManager?.OnRaidedEvent(e);            
        }

        private void Client_OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            Utilities.Log(LogLevel.Information, "BeingHosted notification with channel" + e.BeingHostedNotification.Channel);
            Utilities.Log(LogLevel.Information, "BeingHosted notification with HostedByChannel" + e.BeingHostedNotification.HostedByChannel);
            Utilities.Log(LogLevel.Information, "BeingHosted notification with Viewers" + e.BeingHostedNotification.Viewers);
            Utilities.Log(LogLevel.Information, "BeingHosted notification with BotUsername" + e.BeingHostedNotification.BotUsername);

            this.eventsManager?.OnBeingHostedResponseEvent(e);
        }

        private void Client_OnHostingStarted(object sender, OnHostingStartedArgs e)
        {
            Utilities.Log(LogLevel.Information, "HostingStarted notification with HostingChannel " + e.HostingStarted.HostingChannel);
            Utilities.Log(LogLevel.Information, "HostingStarted notification with TargetChannel " + e.HostingStarted.TargetChannel);
            Utilities.Log(LogLevel.Information, "HostingStarted notification with Viewers " + e.HostingStarted.Viewers);

            this.eventsManager?.OnHostingStartedResponseEvent(e);
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Utilities.Log(LogLevel.Information, $"ClientConnected");
            QTChatManager.Instance.ToggleChat(true);

            this.timersManager?.StartTimers();
            IntegrationHelper.ReEnableAllEnabledDLLsHandlers();
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Utilities.Log(LogLevel.Information, $"ClientDisconnected");
            QTChatManager.Instance.ToggleChat(false);

            this.timersManager?.StopTimers();

            this.OnDisconnected?.Invoke(sender, null);
            IntegrationHelper.DisableAllEnabledDLLsHandlers();
        }

        private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            Utilities.Log(LogLevel.Information, "Client_OnNewSubscriber " + e.Subscriber.DisplayName);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var msg = e.ChatMessage.Message;
            // TODO: Move this to command module
            // Is command?
            if (msg.StartsWith("!"))
            {
                var parts = msg.Split(' ');

                HandleCommand(parts[0], parts.Skip(1), e);
            }

            this.eventsManager?.OnMessageReceivedEvent(e);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            this.currentChannel = new JoinedChannel(e.Channel);
            this.OnConnected?.Invoke(sender, null);

            this.eventsManager?.OnJoinedChannelResponseEvent(e);

            // Send connected greeting message if any
            if (!string.IsNullOrWhiteSpace(this.TwitchOptions.GreetingMessage))
            {
                QTChatManager.Instance.SendInstantMessage(this.TwitchOptions.GreetingMessage);
            }
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Utilities.Log(LogLevel.Information, "Client_OnLog: " + e.Data);
        }
      
        #endregion Client Events

        #region PubSub Events
        private void SetupPubSubListeners()
        {
            RemovePubSubListeners();

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
            this.pubSubClient.OnFollow += PubSubClient_OnFollow;

            this.pubSubClient.ListenToRewards(this.channelId);
            this.pubSubClient.ListenToFollows(this.channelId);
            this.pubSubClient.ListenToBitsEvents(this.channelId);
            this.pubSubClient.ListenToChatModeratorActions(this.userId, this.channelId);
            this.pubSubClient.ListenToRaid(this.channelId);
            this.pubSubClient.ListenToSubscriptions(this.channelId);
        }

        private void RemovePubSubListeners()
        {
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
            this.pubSubClient.OnFollow -= PubSubClient_OnFollow;
        }

        private void PubSubClient_OnPubSubServiceError(object sender, TwitchLib.PubSub.Events.OnPubSubServiceErrorArgs e)
        {
            Utilities.Log($"PubSubClient_OnPubSubServiceError: {e.Exception.Message} | {e.Exception.StackTrace}");
        }

        private void PubSubClient_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            Utilities.Log("PubSubClient_OnPubSubServiceClosed");
        }

        private void PubSubClient_OnFollow(object sender, TwitchLib.PubSub.Events.OnFollowArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnFollow");
            this.eventsManager?.OnNewFollowerEvent(e);
        }

        private void PubSubClient_OnHost(object sender, TwitchLib.PubSub.Events.OnHostArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnHost");
        }

        private void PubSubClient_OnChannelSubscription(object sender, TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnChannelSubscription " + e.Subscription.DisplayName);
            this.eventsManager?.OnNewSubscriberEvent(e);
        }

        private void PubSubClient_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            Utilities.Log(LogLevel.Information, $"PubSubClient_OnBitsReceived BitsUsed: {e.BitsUsed}, TotalBitsUsed: {e.TotalBitsUsed}, message: {e.ChatMessage}");
            this.eventsManager?.OnBitsReceivedEvent(e);
        }

        private void PubSubClient_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnStreamDown");
            this.eventsManager?.OnStreamDownResponseEvent(e);
        }

        private void PubSubClient_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnStreamUp");
            this.eventsManager?.OnStreamUpResponseEvent(e);
        }

        private void PubSubClient_OnRewardRedeemed(object sender, TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            Utilities.Log(LogLevel.Information, $"PubSubClient_OnRewardRedeemed with RewardTitle: {e.RewardTitle}");
            Utilities.Log(LogLevel.Information, $"PubSubClient_OnRewardRedeemed with RewardCost: {e.RewardCost}");
            Utilities.Log(LogLevel.Information, $"PubSubClient_OnRewardRedeemed with RewardPrompt: {e.RewardPrompt}");
            Utilities.Log(LogLevel.Information, $"PubSubClient_OnRewardRedeemed with Message: {e.Message}");
            Utilities.Log(LogLevel.Information, $"PubSubClient_OnRewardRedeemed with Status: {e.Status}");

            // Only consider new redeems
            if (e.Status.Equals("UNFULFILLED"))
            {
                QTChatManager.Instance.QueueRedeemAlert(e.RewardTitle, e.DisplayName);
            }

            this.eventsManager?.OnRewardRedeemedEvent(e);
        }

        private void PubSubClient_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            Utilities.Log($"PubSubClient_OnListenResponse for {e.Topic} was successful: {e.Successful} | {e.Response.Error ?? ""}");
            this.eventsManager?.OnListenResponseEvent(e);
        }

        private void PubSubClient_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnPubSubServiceConnected");
            this.pubSubClient.SendTopics(this.apiClient.Settings.AccessToken);
        }

        private void PubSubClient_OnEmoteOnlyOff(object sender, TwitchLib.PubSub.Events.OnEmoteOnlyOffArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnEmoteOnlyOff");
            this.eventsManager?.OnEmoteOnlyOffEvent(e);
        }

        private void PubSubClient_OnEmoteOnly(object sender, TwitchLib.PubSub.Events.OnEmoteOnlyArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnEmoteOnly");
            this.eventsManager?.OnEmoteOnlyOnEvent(e);
        }

        private void PubSubClient_OnRaidUpdateV2(object sender, TwitchLib.PubSub.Events.OnRaidUpdateV2Args e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnRaidUpdateV2");
        }

        private void PubSubClient_OnRaidUpdate(object sender, TwitchLib.PubSub.Events.OnRaidUpdateArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnRaidUpdate");
        }

        private void PubSubClient_OnRaidGo(object sender, TwitchLib.PubSub.Events.OnRaidGoArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnRaidGo");
        }
        #endregion PubSub Events

        #region Test
        public void TestMessage()
        {
            this.Client.SendMessage(this.currentChannel, "Hai, I'm connected!");
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
