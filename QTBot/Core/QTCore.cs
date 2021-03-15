using Microsoft.Extensions.Logging;
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
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;

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

        public QTCommandsManager CommandsManager => commandsManager;
        public QTTimersManager TimersManager => timersManager;
        public QTEventsManager EventsManager => eventsManager;

        // Configurations
        private ConfigModel mainConfig = null;
        private string channelId = null;
        private string userId = null;
        private JoinedChannel currentChannel = null;

        // Twitch client
        private TwitchClient client = null;
        public TwitchClient Client => client;
        public JoinedChannel CurrentChannel => currentChannel;
        public string BotUserName => mainConfig?.BotChannelName ?? "<Invalid Value>";
        public string CurrentChannelName => mainConfig?.StreamerChannelName ?? "<Invalid Value>";
        public bool IsMainConfigLoaded => mainConfig?.IsConfigured ?? false;

        // Twitch options
        private TwitchOptions twitchOptions = null;
        public TwitchOptions TwitchOptions => twitchOptions;
        public bool IsRedemptionAlertOn => TwitchOptions.IsRedemptionTagUser && !string.IsNullOrEmpty(TwitchOptions.RedemptionTagUser);

        // TwitchLibs stuff
        private TwitchPubSub pubSubClient = null;
        private TwitchAPI apiClient = null;

        public event EventHandler OnConnected;
        public event EventHandler OnDisconnected;

        public QTCore()
        {
            SetupLogging();
            LoadConfigs();

            // Check Dlls folder and DLLs settings for additional DLLIntegration
            string integrationFolderPath = Path.Combine(Utilities.GetDataDirectory(), "DLLIntegration");
            IntegrationHelper.SetupIntegrationhelper(integrationFolderPath, Path.Combine(integrationFolderPath, "DLLIntegrationSetup.json"));
            IntegrationHelper.SetupDLLIntegration();
        }

        #region Core Functionality
        public void LoadConfigs()
        {
            mainConfig = ConfigManager.ReadConfig();
            twitchOptions = new TwitchOptions(ConfigManager.ReadTwitchOptionsConfigs());
        }

        /// <summary>
        /// Initial setup of the services
        /// </summary>
        public async Task Setup()
        {
            ConnectionCredentials credentials = new ConnectionCredentials(mainConfig.BotChannelName, mainConfig.BotOAuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            // Setup Twitch client
            WebSocketClient customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(customClient);
            Client.Initialize(credentials, mainConfig.StreamerChannelName);

            // Setup core listeners separately
            Client.OnConnected += Client_OnConnected;
            Client.OnDisconnected += Client_OnDisconnected;

            // Setup all other listeners
            SetupClientEventListeners();

            Client.Connect();

            // Setup QT chat manager
            QTChatManager.Instance.Initialize(Client);

            // Setup modules
            SetupModules();

            // Setup API client
            apiClient = new TwitchAPI();

            // Auth user
            apiClient.Settings.ClientId = mainConfig.StreamerChannelClientId;
            apiClient.Settings.AccessToken = mainConfig.StreamerChannelAccessToken;

            var credentialResponse = await apiClient.ThirdParty.AuthorizationFlow.CheckCredentialsAsync();
            // Current token not working
            if (!credentialResponse.Result)
            {
                Utilities.Log(LogLevel.Information, credentialResponse.ResultMessage);
                // Try refresh
                var refreshResponse = apiClient.ThirdParty.AuthorizationFlow.RefreshToken(mainConfig.StreamerChannelRefreshToken);
                if (!string.IsNullOrEmpty(refreshResponse.Token) && !string.IsNullOrEmpty(refreshResponse.Refresh))
                {
                    // Update to new tokens from the refresh
                    mainConfig.StreamerChannelAccessToken = refreshResponse.Token;
                    mainConfig.StreamerChannelRefreshToken = refreshResponse.Refresh;
                    ConfigManager.SaveConfig(mainConfig);
                    apiClient.Settings.ClientId = mainConfig.StreamerChannelClientId;
                    apiClient.Settings.AccessToken = mainConfig.StreamerChannelAccessToken;

                    // Check with refreshed tokens
                    credentialResponse = await apiClient.ThirdParty.AuthorizationFlow.CheckCredentialsAsync();
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
            var authChannel = await apiClient.V5.Channels.GetChannelAsync(apiClient.Settings.AccessToken);
            channelId = authChannel.Id;
            var authUser = await apiClient.V5.Users.GetUserAsync(apiClient.Settings.AccessToken);
            userId = authUser.Id;

            // Setup PubSub client
            pubSubClient = new TwitchPubSub();

            // Setup core listeners separately
            pubSubClient.OnPubSubServiceClosed += PubSubClient_OnPubSubServiceClosed;
            pubSubClient.OnPubSubServiceError += PubSubClient_OnPubSubServiceError;
            pubSubClient.OnPubSubServiceConnected += PubSubClient_OnPubSubServiceConnected;

            // Setup all other listeners
            SetupPubSubListeners();

            pubSubClient.Connect();

            // Setup StreamElements if configured
            if (mainConfig.IsStreamElementsConfigured)
            {
                StreamElementsModule.Instance.Initialize(mainConfig);
            }

            // Start integrations
            IntegrationHelper.StartDLLIntegration();
        }

        /// <summary>
        /// Disconnect Twitch services.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                RemovePubSubListeners();
                pubSubClient.Disconnect();
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, "QTCore.Disconnect PubSub exception: " + e.StackTrace);
            }

            try
            {
                RemoveClientEventListeners();
                Client.Disconnect();
            }
            catch (Exception e)
            {
                Utilities.Log(LogLevel.Error, "QTCore.Disconnect Client exception: " + e.StackTrace);
            }
            finally
            {
                Client.OnConnected -= Client_OnConnected;
                Client.OnDisconnected -= Client_OnDisconnected;

                pubSubClient.OnPubSubServiceClosed -= PubSubClient_OnPubSubServiceClosed;
                pubSubClient.OnPubSubServiceError -= PubSubClient_OnPubSubServiceError;
                pubSubClient.OnPubSubServiceConnected -= PubSubClient_OnPubSubServiceConnected;
            }
        }

        /// <summary>
        /// [DEPRECATED] Saves the Twitch options and shows a dialog if successful.
        /// </summary>
        public void SetupTwitchOptions(TwitchOptions options)
        {
            twitchOptions = options;
            if (ConfigManager.SaveTwitchOptionsConfigs(twitchOptions))
            {
                Utilities.ShowMessage("Twitch options saved!");
            }
        }

        private async void HandleCommand(string command, IEnumerable<string> args, OnMessageReceivedArgs messageArgs)
        {
            var result = await commandsManager?.ProcessCommand(command, args, messageArgs);
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
            commandsManager = new QTCommandsManager();

            // Setup QT timers manager
            timersManager = new QTTimersManager();

            // Setup QT events manager
            eventsManager = new QTEventsManager();
        }
        #endregion Core Functionality

        #region Client Events

        private void SetupClientEventListeners()
        {
            if (Client != null)
            {
                Client.OnLog += Client_OnLog;
                Client.OnJoinedChannel += Client_OnJoinedChannel;
                Client.OnMessageReceived += Client_OnMessageReceived;
                Client.OnNewSubscriber += Client_OnNewSubscriber;
                Client.OnHostingStarted += Client_OnHostingStarted;
                Client.OnBeingHosted += Client_OnBeingHosted;
                Client.OnRaidNotification += Client_OnRaidNotification;
            }
        }

        private void RemoveClientEventListeners()
        {
            if (Client != null)
            {
                Client.OnLog -= Client_OnLog;
                Client.OnJoinedChannel -= Client_OnJoinedChannel;
                Client.OnMessageReceived -= Client_OnMessageReceived;
                Client.OnNewSubscriber -= Client_OnNewSubscriber;
                Client.OnHostingStarted -= Client_OnHostingStarted;
                Client.OnBeingHosted -= Client_OnBeingHosted;
                Client.OnRaidNotification -= Client_OnRaidNotification;
            }
        }

        private void Client_OnRaidNotification(object sender, OnRaidNotificationArgs e)
        {
            Utilities.Log(LogLevel.Information, "Raid notification with display name: " + e.RaidNotification.DisplayName);
            Utilities.Log(LogLevel.Information, "Raid notification with channel name: " + e.Channel);
            Utilities.Log(LogLevel.Information, "Raid notification with MsgParamDisplayName: " + e.RaidNotification.MsgParamDisplayName);
            Utilities.Log(LogLevel.Information, "Raid notification with MsgParamViewerCount: " + e.RaidNotification.MsgParamViewerCount);
            Utilities.Log(LogLevel.Information, "Raid notification with MsgParamLogin: " + e.RaidNotification.MsgParamLogin);

            if (TwitchOptions.IsAutoShoutOutHost)
            {
                _ = QTChatManager.Instance.SendMessage($"!so {e.RaidNotification.DisplayName}", 5000);
            }

            eventsManager?.OnRaidedEvent(e);
        }

        private void Client_OnBeingHosted(object sender, OnBeingHostedArgs e)
        {
            Utilities.Log(LogLevel.Information, "BeingHosted notification with channel" + e.BeingHostedNotification.Channel);
            Utilities.Log(LogLevel.Information, "BeingHosted notification with HostedByChannel" + e.BeingHostedNotification.HostedByChannel);
            Utilities.Log(LogLevel.Information, "BeingHosted notification with Viewers" + e.BeingHostedNotification.Viewers);
            Utilities.Log(LogLevel.Information, "BeingHosted notification with BotUsername" + e.BeingHostedNotification.BotUsername);

            eventsManager?.OnBeingHostedResponseEvent(e);
        }

        private void Client_OnHostingStarted(object sender, OnHostingStartedArgs e)
        {
            Utilities.Log(LogLevel.Information, "HostingStarted notification with HostingChannel " + e.HostingStarted.HostingChannel);
            Utilities.Log(LogLevel.Information, "HostingStarted notification with TargetChannel " + e.HostingStarted.TargetChannel);
            Utilities.Log(LogLevel.Information, "HostingStarted notification with Viewers " + e.HostingStarted.Viewers);

            eventsManager?.OnHostingStartedResponseEvent(e);
        }

        private void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Utilities.Log(LogLevel.Information, $"ClientConnected");
            QTChatManager.Instance.ToggleChat(true);

            timersManager?.StartTimers();
            IntegrationHelper.ReEnableAllEnabledDLLsHandlers();
        }

        private void Client_OnDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Utilities.Log(LogLevel.Information, $"ClientDisconnected");
            QTChatManager.Instance.ToggleChat(false);

            timersManager?.StopTimers();

            OnDisconnected?.Invoke(sender, null);
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

            eventsManager?.OnMessageReceivedEvent(e);
        }

        private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            currentChannel = new JoinedChannel(e.Channel);
            OnConnected?.Invoke(sender, null);

            eventsManager?.OnJoinedChannelResponseEvent(e);

            // Send connected greeting message if any
            if (!string.IsNullOrWhiteSpace(TwitchOptions.GreetingMessage))
            {
                QTChatManager.Instance.SendInstantMessage(TwitchOptions.GreetingMessage);
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

            if (pubSubClient != null)
            {
                pubSubClient.OnListenResponse += PubSubClient_OnListenResponse;
                pubSubClient.OnEmoteOnly += PubSubClient_OnEmoteOnly;
                pubSubClient.OnEmoteOnlyOff += PubSubClient_OnEmoteOnlyOff;
                pubSubClient.OnRewardRedeemed += PubSubClient_OnRewardRedeemed;
                pubSubClient.OnStreamUp += PubSubClient_OnStreamUp;
                pubSubClient.OnStreamDown += PubSubClient_OnStreamDown;
                pubSubClient.OnBitsReceived += PubSubClient_OnBitsReceived;
                pubSubClient.OnChannelSubscription += PubSubClient_OnChannelSubscription;
                pubSubClient.OnHost += PubSubClient_OnHost;
                pubSubClient.OnRaidGo += PubSubClient_OnRaidGo;
                pubSubClient.OnRaidUpdate += PubSubClient_OnRaidUpdate;
                pubSubClient.OnRaidUpdateV2 += PubSubClient_OnRaidUpdateV2;
                pubSubClient.OnFollow += PubSubClient_OnFollow;

                pubSubClient.ListenToRewards(channelId);
                pubSubClient.ListenToFollows(channelId);
                pubSubClient.ListenToBitsEvents(channelId);
                pubSubClient.ListenToChatModeratorActions(userId, channelId);
                pubSubClient.ListenToRaid(channelId);
                pubSubClient.ListenToSubscriptions(channelId);
            }
        }

        private void RemovePubSubListeners()
        {
            if (pubSubClient != null)
            {
                pubSubClient.OnListenResponse -= PubSubClient_OnListenResponse;
                pubSubClient.OnEmoteOnly -= PubSubClient_OnEmoteOnly;
                pubSubClient.OnEmoteOnlyOff -= PubSubClient_OnEmoteOnlyOff;
                pubSubClient.OnRewardRedeemed -= PubSubClient_OnRewardRedeemed;
                pubSubClient.OnStreamUp -= PubSubClient_OnStreamUp;
                pubSubClient.OnStreamDown -= PubSubClient_OnStreamDown;
                pubSubClient.OnBitsReceived -= PubSubClient_OnBitsReceived;
                pubSubClient.OnChannelSubscription -= PubSubClient_OnChannelSubscription;
                pubSubClient.OnHost -= PubSubClient_OnHost;
                pubSubClient.OnRaidGo -= PubSubClient_OnRaidGo;
                pubSubClient.OnRaidUpdate -= PubSubClient_OnRaidUpdate;
                pubSubClient.OnRaidUpdateV2 -= PubSubClient_OnRaidUpdateV2;
                pubSubClient.OnFollow -= PubSubClient_OnFollow;
            }
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
            eventsManager?.OnNewFollowerEvent(e);
        }

        private void PubSubClient_OnHost(object sender, TwitchLib.PubSub.Events.OnHostArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnHost");
        }

        private void PubSubClient_OnChannelSubscription(object sender, TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnChannelSubscription " + e.Subscription.DisplayName);
            eventsManager?.OnNewSubscriberEvent(e);
        }

        private void PubSubClient_OnBitsReceived(object sender, TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            Utilities.Log(LogLevel.Information, $"PubSubClient_OnBitsReceived BitsUsed: {e.BitsUsed}, TotalBitsUsed: {e.TotalBitsUsed}, message: {e.ChatMessage}");
            eventsManager?.OnBitsReceivedEvent(e);
        }

        private void PubSubClient_OnStreamDown(object sender, TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnStreamDown");
            eventsManager?.OnStreamDownResponseEvent(e);
        }

        private void PubSubClient_OnStreamUp(object sender, TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnStreamUp");
            eventsManager?.OnStreamUpResponseEvent(e);
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

            eventsManager?.OnRewardRedeemedEvent(e);
        }

        private void PubSubClient_OnListenResponse(object sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            Utilities.Log($"PubSubClient_OnListenResponse for {e.Topic} was successful: {e.Successful} | {e.Response.Error ?? ""}");
            eventsManager?.OnListenResponseEvent(e);
        }

        private void PubSubClient_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnPubSubServiceConnected");
            pubSubClient.SendTopics(apiClient.Settings.AccessToken);
        }

        private void PubSubClient_OnEmoteOnlyOff(object sender, TwitchLib.PubSub.Events.OnEmoteOnlyOffArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnEmoteOnlyOff");
            eventsManager?.OnEmoteOnlyOffEvent(e);
        }

        private void PubSubClient_OnEmoteOnly(object sender, TwitchLib.PubSub.Events.OnEmoteOnlyArgs e)
        {
            Utilities.Log(LogLevel.Information, "PubSubClient_OnEmoteOnly");
            eventsManager?.OnEmoteOnlyOnEvent(e);
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
            Client.SendMessage(currentChannel, "Hai, I'm connected!");
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
