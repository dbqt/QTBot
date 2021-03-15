using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace QTBot.CustomDLLIntegration
{
    public interface DLLIntegrationInterface
    {
        event LogMessage SendLogMessage;
        event MessageToTwitch SendMessageToTwitchChat;
        string IntegrationName { get; }
        string IntegrationDefinition { get; }
        string IntegrationVersion { get; }
        string DLLSettingsFileName { get; }
        SettingsUI DefaultUI { get; }
        SettingsUI CurrentSettingsUI { get; set; }

        bool DisableDLL();
        bool OnDLLStartup();
        void OnMessageReceived(object sender, OnMessageReceivedArgs e);

        //These event will be fired from pubsub
        void OnStreamUp(object sender, OnStreamUpArgs e);
        void OnStreamDown(object sender, OnStreamDownArgs e);

        void OnListenResponse(object sender, OnListenResponseArgs e);
        void OnEmoteOnlyOn(object sender, OnEmoteOnlyArgs e);
        void OnEmoteOnlyOff(object sender, OnEmoteOnlyOffArgs e);
        void OnRewardRedeemed(object sender, OnRewardRedeemedArgs e);
        void OnBitsReceived(object sender, OnBitsReceivedArgs e);
        void OnChannelSubscription(object sender, OnChannelSubscriptionArgs e);
        void OnFollow(object sender, OnFollowArgs e);

        //These Events will be fired from the client
        void OnBotJoinedChannel(object sender, OnJoinedChannelArgs e);

        void OnHostingStarted(object sender, OnHostingStartedArgs e);
        void OnBeingHosted(object sender, OnBeingHostedArgs e);
        void OnRaidNotification(object sender, OnRaidNotificationArgs e);
    }
}
