using Microsoft.Extensions.Logging;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace QTBot.CustomDLLIntegration
{
    public interface DLLIntegratrionInterface
    {
        event LogMessage SendLogMessage;
        event MessageToTwitch SendMessageToTwtichChat;
        string IntegratrionName { get; }
        string IntegratrionDefinition { get; }
        string IntegratrionVersion { get; }

        void WriteLog(string integrationName, LogLevel level, string message);
        string GetDLLSettingsPath();
        string GetDLLName();
        string GetDLLVersion();
        string GetDLLDescription();
        SettingsUI GetSettingsUI();

        void OnDLLStartup(StartupData data);        
        void OnMessageReceived(OnMessageReceivedArgs e);

        //These event will be fired from pubsub
        void OnStreamUp(OnStreamUpArgs e);
        void OnStreamDown(OnStreamDownArgs e);

        void OnListenResponse(OnListenResponseArgs e);
        void OnEmoteOnlyOn(OnEmoteOnlyArgs e);
        void OnEmoteOnlyOff(OnEmoteOnlyOffArgs e);
        void OnRewardRedeemed(OnRewardRedeemedArgs e);        
        void OnBitsReceived(OnBitsReceivedArgs e);
        void OnChannelSubscription(OnChannelSubscriptionArgs e);
        void OnFollow(OnFollowArgs e);

        //These Events will be fired from the client
        void OnBotJoinedChannel(OnJoinedChannelArgs e);

        void OnHostingStarted(OnHostingStartedArgs e);
        void OnBeingHosted(OnBeingHostedArgs e);
        void OnRaidNotification(OnRaidNotificationArgs e);

        
    }
}
