using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace QTBot.CustomDLLIntegration
{
    public interface DLLIntegratrionInterface
    {
        event LogMessage SendLogMessage;
        event MessageToTwitch SendMessageToTwtichChat;
        string IntegrationName { get; }
        string IntegrationDefinition { get; }
        string IntegrationVersion { get; }
        string DLLSettingsFileName { get; }
        SettingsUI DefaultUI { get; }

        bool DisableDLL();
        bool OnDLLStartup();        
        void OnMessageReceived(object sender, OnMessageReceivedArgs e);

        //These event will be fired from pubsub
        void OnStreamUp(OnStreamUpArgs e);
        void OnStreamDown(OnStreamDownArgs e);

        void OnListenResponse(OnListenResponseArgs e);
        void OnEmoteOnlyOn(object sender, OnEmoteOnlyArgs e);
        void OnEmoteOnlyOff(object sender, OnEmoteOnlyOffArgs e);
        void OnRewardRedeemed(object sender, OnRewardRedeemedArgs e);        
        void OnBitsReceived(object sender, OnBitsReceivedArgs e);
        void OnChannelSubscription(object sender, OnChannelSubscriptionArgs e);
        void OnFollow(object sender, OnFollowArgs e);

        //These Events will be fired from the client
        void OnBotJoinedChannel(OnJoinedChannelArgs e);

        void OnHostingStarted(OnHostingStartedArgs e);
        void OnBeingHosted(OnBeingHostedArgs e);
        void OnRaidNotification(object sender, OnRaidNotificationArgs e);        
    }
}
