using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace QTBot.CustomDLLIntegration
{
    public abstract class IntegrationBase : DLLIntegrationInterface
    {
        public abstract string IntegrationName { get; }
        public abstract string IntegrationDefinition { get; }
        public abstract string IntegrationVersion { get; }
        public abstract SettingsUI DefaultUI { get; }

        public SettingsUI CurrentSettingsUI { get; set; }

        public string DLLSettingsFileName { get; }

        public event LogMessage SendLogMessage;
        public event MessageToTwitch SendMessageToTwitchChat;

        private Thread dllLoopThread = null;

        public IntegrationBase()
        {
            DLLSettingsFileName = $"{IntegrationName}Settings.json";
            WriteLog(LogLevel.Information, $"DLL: {IntegrationName} created with SettingsPath: {DLLSettingsFileName}");
        }

        public bool DisableDLL()
        {
            try
            {
                if (dllLoopThread?.ThreadState != ThreadState.Suspended)
                {
                    dllLoopThread?.Abort();
                }

                for (int i = 0; i < 5; i++)
                {
                    if (dllLoopThread?.ThreadState == ThreadState.Aborted)
                    {
                        dllLoopThread = null;

                        WriteLog(LogLevel.Information, $"DLL: {IntegrationName} has been stopped.");
                        return true;
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception e)
            {
                WriteLog(LogLevel.Information, $"DLL: {IntegrationName} has failed to stop.");
                WriteLog(LogLevel.Error, e);
            }

            return false;
        }

        public bool OnDLLStartup()
        {
            try
            {
                if (dllLoopThread == null)
                {
                    dllLoopThread = new Thread(DLLStartup);
                    dllLoopThread?.Start();

                    WriteLog(LogLevel.Information, $"DLL: {IntegrationName} has started correctly.");
                    return true;
                }


                WriteLog(LogLevel.Error, $"Thread for dll {IntegrationName} failed to close properly and cannot be restarted.");
                return false;
            }
            catch (Exception e)
            {
                WriteLog(LogLevel.Information, $"DLL: {IntegrationName} failed to started correctly.");
                WriteLog(LogLevel.Error, e);
                return false;
            }
        }

        protected void WriteLog(LogLevel level, Exception e)
        {
            WriteLog(level, $"Error: {e.Message}, Stack: {e.StackTrace}");
        }

        protected void WriteLog(LogLevel level, string message)
        {
            SendLogMessage?.Invoke(IntegrationName, level, message);
        }

        protected abstract void DLLStartup();

        public abstract void OnBeingHosted(object sender, OnBeingHostedArgs e);

        public abstract void OnBitsReceived(object sender, OnBitsReceivedArgs e);

        public abstract void OnBotJoinedChannel(object sender, OnJoinedChannelArgs e);

        public abstract void OnChannelSubscription(object sender, OnChannelSubscriptionArgs e);

        public abstract void OnEmoteOnlyOff(object sender, OnEmoteOnlyOffArgs e);

        public abstract void OnEmoteOnlyOn(object sender, OnEmoteOnlyArgs e);

        public abstract void OnFollow(object sender, OnFollowArgs e);

        public abstract void OnHostingStarted(object sender, OnHostingStartedArgs e);

        public abstract void OnListenResponse(object sender, OnListenResponseArgs e);

        public abstract void OnMessageReceived(object sender, OnMessageReceivedArgs e);

        public abstract void OnRaidNotification(object sender, OnRaidNotificationArgs e);

        public abstract void OnRewardRedeemed(object sender, OnRewardRedeemedArgs e);

        public abstract void OnStreamDown(object sender, OnStreamDownArgs e);

        public abstract void OnStreamUp(object sender, OnStreamUpArgs e);
    }
}
