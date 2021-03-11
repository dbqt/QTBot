using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace QTBot.CustomDLLIntegration
{
    public abstract class IntegrationBase : DLLIntegratrionInterface
    {
        public abstract string IntegratrionName { get; }
        public abstract string IntegratrionDefinition { get; }
        public abstract string IntegratrionVersion { get; }
        public abstract SettingsUI DefaultUI { get; }

        public string DLLSettingsPath { get; }           

        public event LogMessage SendLogMessage;
        public event MessageToTwitch SendMessageToTwtichChat;

        private Thread dllLoopThread = null;
        private string DLLDirectoryPath;

        public IntegrationBase(string dllFilePath)
        {
            DLLDirectoryPath = Path.Combine(dllFilePath, IntegratrionName);
            DLLSettingsPath = Path.Combine(DLLDirectoryPath, $"{IntegratrionName}Settings.json");

            WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} created with DirectoryPath: {DLLDirectoryPath} and SettingsPath: {DLLSettingsPath}");
        }

        public bool DisableDLL()
        {
            try
            {
                if (dllLoopThread?.ThreadState != ThreadState.Suspended)
                {
                    dllLoopThread?.Abort();
                    dllLoopThread = null;
                }

                WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} has been stopped.");
                return true;
            }
            catch (Exception e)
            {
                WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} has failed to stop.");
                WriteLog(LogLevel.Error, e);
                return false;
            }            
        }

        public bool OnDLLStartup()
        {
            try
            {
                if(dllLoopThread == null)
                {
                    dllLoopThread = new Thread(this.DLLStatup);
                    dllLoopThread?.Start();

                    WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} has started correctly.");
                    return true;
                }


                WriteLog(LogLevel.Error, $"Thread for dll {IntegratrionName} failed to close properly and cannot be restarted.");
                return false;
            }
            catch (Exception e)
            {
                WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} failed to started correctly.");
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
            SendLogMessage?.Invoke(IntegratrionName, level, message);
        }

        public SettingsUI GetSettingsUI()
        {
            try
            {
                SettingsUI rtn;

                if (File.Exists(DLLSettingsPath))
                {
                    string StartupJSON = File.ReadAllText(DLLSettingsPath);
                    rtn = JsonConvert.DeserializeObject(StartupJSON) as SettingsUI;

                    if (rtn != null)
                    {
                        return rtn;
                    }

                    WriteLog(LogLevel.Warning, $"DLL: {IntegratrionName} settings file could not be deserialized. SettingsFilePath: {DLLSettingsPath}");
                }
                else
                {
                    if (Directory.Exists(DLLDirectoryPath) == false)
                    {
                        Directory.CreateDirectory(DLLDirectoryPath);
                    }

                    File.WriteAllText(DLLSettingsPath, JsonConvert.SerializeObject(DefaultUI));

                    WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} settings did not exist, creating default. SettingsFilePath: {DLLSettingsPath}");
                }   
            }
            catch (Exception e)
            {
                WriteLog(LogLevel.Error, e);
            }

            WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} failed to load settings.");
            return null;
        }

        public bool SaveSettings(SettingsUI uiValues)
        {
            if(uiValues != null)
            {
                try
                {
                    if(Directory.Exists(DLLDirectoryPath) == false)
                    {
                        Directory.CreateDirectory(DLLDirectoryPath);
                    }

                    File.WriteAllText(DLLSettingsPath, JsonConvert.SerializeObject(uiValues));

                    WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} settings saved.");
                    return true;
                }
                catch (Exception e)
                {
                    WriteLog(LogLevel.Information, $"DLL: {IntegratrionName} failed to save settings.");
                    WriteLog(LogLevel.Error, e);
                }
            }

            return false;
        }

        protected abstract void DLLStatup();        

        public abstract void OnBeingHosted(OnBeingHostedArgs e);

        public abstract void OnBitsReceived(OnBitsReceivedArgs e);

        public abstract void OnBotJoinedChannel(OnJoinedChannelArgs e);

        public abstract void OnChannelSubscription(OnChannelSubscriptionArgs e);

        public abstract void OnEmoteOnlyOff(OnEmoteOnlyOffArgs e);

        public abstract void OnEmoteOnlyOn(OnEmoteOnlyArgs e);

        public abstract void OnFollow(OnFollowArgs e);

        public abstract void OnHostingStarted(OnHostingStartedArgs e);

        public abstract void OnListenResponse(OnListenResponseArgs e);

        public abstract void OnMessageReceived(OnMessageReceivedArgs e);

        public abstract void OnRaidNotification(OnRaidNotificationArgs e);

        public abstract void OnRewardRedeemed(OnRewardRedeemedArgs e);

        public abstract void OnStreamDown(OnStreamDownArgs e);

        public abstract void OnStreamUp(OnStreamUpArgs e);        
    }
}
