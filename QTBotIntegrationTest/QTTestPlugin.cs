using Microsoft.Extensions.Logging;
using QTBot.CustomDLLIntegration;
using System.Collections.Generic;

namespace QTBotIntegrationTest
{
    public class QTTestPlugin : IntegrationBase
    {
        public override string IntegrationName => "QTTestPlugin";

        public override string IntegrationDefinition => "Something";

        public override string IntegrationVersion => "0.1";

        public override SettingsUI DefaultUI
        {
            get
            {
                var sections = new List<UISection>();
                var section1 = new UISection("One");
                var s1CheckBox = new UICheckbox("0s1", "someBool", 0, "Some bool");
                var s1RadialButton = new UIRadialButton("1s1", "radial", 1, 
                    new List<KeyValuePair<string, object>>() {
                        new KeyValuePair<string, object>("a", "a value"),
                        new KeyValuePair<string, object>("b", "b value") 
                    },
                    "Some radial");
                var s1UITextBox = new UITextBox("2s1", "someText", 2, "text?");
                var s1UISlider = new UISlider("3s1", "someSlider", 3, "text?", 10, 0, 5, 1);
                section1.SectionElements.Add(s1CheckBox);
                section1.SectionElements.Add(s1RadialButton);
                section1.SectionElements.Add(s1UITextBox);
                section1.SectionElements.Add(s1UISlider);
                sections.Add(section1);

                var section2 = new UISection("Two");
                var s2UISelectionDropdown = new UISelectionDropdown("0s2", "someDropdown", 0,
                    new List<KeyValuePair<string, object>>() {
                        new KeyValuePair<string, object>("a", "a value"),
                        new KeyValuePair<string, object>("b", "b value"),
                        new KeyValuePair<string, object>("c", "c value")
                    }, 
                    "Some dropdown");
                var s2UIEditableDropdown = new UIEditableDropdown("1s2", "someEditdableDropdown",
                    new List<KeyValuePair<string, object>>() {
                        new KeyValuePair<string, object>("a", "a value"),
                        new KeyValuePair<string, object>("b", "b value"),
                        new KeyValuePair<string, object>("c", "c value")
                    }, 1, "Some editable dropdown", "Some editable value");

                section2.SectionElements.Add(s2UISelectionDropdown);
                section2.SectionElements.Add(s2UIEditableDropdown);
                sections.Add(section2);

                return new SettingsUI(sections);
            }
        }

        public override void OnBeingHosted(object sender, global::TwitchLib.Client.Events.OnBeingHostedArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] OnBeingHosted: {e.BeingHostedNotification.HostedByChannel}");
        }

        public override void OnBitsReceived(object sender, global::TwitchLib.PubSub.Events.OnBitsReceivedArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] OnBitsReceived: {e.Username} with {e.BitsUsed}");
        }

        public override void OnBotJoinedChannel(object sender, global::TwitchLib.Client.Events.OnJoinedChannelArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] OnBotJoinedChannel: {e.Channel}");
        }

        public override void OnChannelSubscription(object sender, global::TwitchLib.PubSub.Events.OnChannelSubscriptionArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnChannelSubscription: {e.Subscription.DisplayName}");
        }

        public override void OnEmoteOnlyOff(object sender, global::TwitchLib.PubSub.Events.OnEmoteOnlyOffArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnEmoteOnlyOff");
        }

        public override void OnEmoteOnlyOn(object sender, global::TwitchLib.PubSub.Events.OnEmoteOnlyArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnEmoteOnlyOn");
        }

        public override void OnFollow(object sender, global::TwitchLib.PubSub.Events.OnFollowArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnFollow: {e.DisplayName}");
        }

        public override void OnHostingStarted(object sender, global::TwitchLib.Client.Events.OnHostingStartedArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnHostingStarted: {e.HostingStarted.HostingChannel}");
        }

        public override void OnListenResponse(object sender, global::TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnListenResponse: {e.Topic} listened {e.Successful}");
        }

        public override void OnMessageReceived(object sender, global::TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnMessageReceived: {e.ChatMessage}");
        }

        public override void OnRaidNotification(object sender, global::TwitchLib.Client.Events.OnRaidNotificationArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnRaidNotification: {e.RaidNotification.DisplayName}");
        }

        public override void OnRewardRedeemed(object sender, global::TwitchLib.PubSub.Events.OnRewardRedeemedArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnRewardRedeemed: {e.RewardTitle}");
        }

        public override void OnStreamDown(object sender, global::TwitchLib.PubSub.Events.OnStreamDownArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnStreamDown");
        }

        public override void OnStreamUp(object sender, global::TwitchLib.PubSub.Events.OnStreamUpArgs e)
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - OnStreamUp");
        }

        protected override void DLLStartup()
        {
            WriteLog(LogLevel.Information, $"[{this.IntegrationName}] - DLLStartup");
        }
    }
}
