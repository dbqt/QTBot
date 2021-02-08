using QTBot.Helpers;
using QTBot.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace QTBot.Core
{
    public class QTEventsManager
    {
        public enum EventType
        {
            None,
            Redeem,
            Greeting,
            Follow,
            Subscription,
            Bits,
            Raid
        }

        public event EventHandler<OnMessageReceivedArgs> OnMessageReceived;
        public event EventHandler<OnEmoteOnlyArgs> OnEmoteOnly;
        public event EventHandler<OnEmoteOnlyOffArgs> OnEmoteOnlyOff;
        public event EventHandler<OnBitsReceivedArgs> OnBitsReceived;
        public event EventHandler<OnFollowArgs> OnFollow;
        public event EventHandler<OnRewardRedeemedArgs> OnRewardRedeemed;
        public event EventHandler<OnRaidNotificationArgs> OnRaid;
        public event EventHandler<OnChannelSubscriptionArgs> OnChannelSubscription;

        private EventsModel rawEventsModel = null;
        private Dictionary<EventType, List<EventModel>> events = null;

        private List<string> greetedUsers = null;

        public QTEventsManager()
        {
            this.rawEventsModel = ConfigManager.ReadEvents();
            if (this.rawEventsModel != null)
            {
                events = new Dictionary<EventType, List<EventModel>>();
                foreach (var eventItem in this.rawEventsModel.Events.Where(item => item.Active))
                {
                    // Initialize list of events if it's the first of that type
                    if (!this.events.ContainsKey(eventItem.Type))
                    {
                        this.events.Add(eventItem.Type, new List<EventModel>());
                    }

                    this.events[eventItem.Type].Add(eventItem);
                    Utilities.Log($"QTEventsManager - Registered event for {eventItem.Type}, message: {eventItem.Message}, option: {eventItem.Option}");
                }
            }
            else
            {
                Utilities.Log($"QTEventsManager - Could not read events!");
            }

            this.greetedUsers = new List<string>();
        }

        #region Core Events
        public void OnMessageReceivedEvent(OnMessageReceivedArgs args)
        {
            CheckGreetings(args.ChatMessage.DisplayName);
            
            this.OnMessageReceived?.Invoke(this, args);
        }

        public void OnNewSubscriberEvent(OnChannelSubscriptionArgs args)
        {
            var tokenReplacements = new List<KeyValuePair<string, string>>();
            // Username of subscriber or gifter
            if (string.IsNullOrWhiteSpace(args.Subscription.RecipientDisplayName))
            {
                // Regular sub
                tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.Subscription.DisplayName));
            }
            else
            {
                // gifted sub
                tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.Subscription.RecipientDisplayName));
            }

            // Cumulative months count
            if (args.Subscription.CumulativeMonths.HasValue)
            {
                tokenReplacements.Add(new KeyValuePair<string, string>("{{month}}", args.Subscription.CumulativeMonths.Value.ToString()));
            }

            // Sub tier
            switch (args.Subscription.SubscriptionPlan)
            {
                case TwitchLib.PubSub.Enums.SubscriptionPlan.Prime:
                    tokenReplacements.Add(new KeyValuePair<string, string>("{{tier}}", "Prime"));
                    break;
                case TwitchLib.PubSub.Enums.SubscriptionPlan.Tier1:
                    tokenReplacements.Add(new KeyValuePair<string, string>("{{tier}}", "Tier 1"));
                    break;
                case TwitchLib.PubSub.Enums.SubscriptionPlan.Tier2:
                    tokenReplacements.Add(new KeyValuePair<string, string>("{{tier}}", "Tier 2"));
                    break;
                case TwitchLib.PubSub.Enums.SubscriptionPlan.Tier3:
                    tokenReplacements.Add(new KeyValuePair<string, string>("{{tier}}", "Tier 3"));
                    break;
                default:
                    break;
            }

            foreach (var subEvent in this.events[EventType.Subscription])
            {
                SendEventMessageInChat(subEvent, tokenReplacements);
            }

            this.OnChannelSubscription?.Invoke(this, args);
        }

        public void OnRaidedEvent(OnRaidNotificationArgs args)
        {
            var tokenReplacements = new List<KeyValuePair<string, string>>();
            tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.RaidNotification.MsgParamDisplayName));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{count}}", args.RaidNotification.MsgParamViewerCount));

            foreach (var raidEvent in this.events[EventType.Raid])
            {
                SendEventMessageInChat(raidEvent, tokenReplacements);
            }

            this.OnRaid?.Invoke(this, args);
        }

        public void OnBitsReceivedEvent(OnBitsReceivedArgs args)
        {
            var tokenReplacements = new List<KeyValuePair<string, string>>();
            tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.Username));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{bits}}", args.BitsUsed.ToString()));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{total_bits}}", args.TotalBitsUsed.ToString()));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{message}}", args.ChatMessage));

            foreach (var bitEvent in this.events[EventType.Bits])
            {
                SendEventMessageInChat(bitEvent, tokenReplacements);
            }

            this.OnBitsReceived?.Invoke(this, args);
        }

        public void OnRewardRedeemedEvent(OnRewardRedeemedArgs args)
        {
            var tokenReplacements = new List<KeyValuePair<string, string>>();
            tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.DisplayName));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{reward}}", args.RewardTitle));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{message}}", args.Message));

            // Filter only redeems with matching title
            foreach (var redeemEvent in this.events[EventType.Redeem].Where(redeem => redeem.Option.ToLowerInvariant() == args.RewardTitle.ToLowerInvariant()))
            {
                SendEventMessageInChat(redeemEvent, tokenReplacements);
            }

            this.OnRewardRedeemed?.Invoke(this, args);
        }

        public void OnNewFollowerEvent(OnFollowArgs args)
        {
            var tokenReplacements = new List<KeyValuePair<string, string>>();
            tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.DisplayName));

            foreach (var followEvent in this.events[EventType.Follow])
            {
                SendEventMessageInChat(followEvent, tokenReplacements);
            }

            this.OnFollow?.Invoke(this, args);
        }

        public void OnEmoteOnlyOnEvent(OnEmoteOnlyArgs args)
        {
            this.OnEmoteOnly?.Invoke(this, args);
        }

        public void OnEmoteOnlyOffEvent(OnEmoteOnlyOffArgs args)
        {
            this.OnEmoteOnlyOff?.Invoke(this, args);
        }

        #endregion Core Events

        private void CheckGreetings(string username)
        {
            // Get greeting only if it was setup for this specific user
            var greetEvents = this.events[EventType.Greeting].Where(item => item.Option.ToLowerInvariant() == username.ToLowerInvariant());
            if (greetEvents.Count() > 0)
            {
                // But only if the user was never greeted
                if (!this.greetedUsers.Contains(username))
                {
                    // Add the user to the list so we don't greet them again
                    this.greetedUsers.Add(username);

                    var tokenReplacements = new List<KeyValuePair<string, string>>();
                    tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", username));

                    foreach (var greetEvent in greetEvents)
                    {
                        SendEventMessageInChat(greetEvent, tokenReplacements);
                    }
                }
            }
        }

        private void SendEventMessageInChat(EventModel eventItem, List<KeyValuePair<string, string>> tokenReplacements)
        {
            string message = Utilities.ReplaceKeywords(eventItem.Message, tokenReplacements);
            Utilities.Log($"QTEventsMananger - sending: {message}");
            _ = QTChatManager.Instance.SendMessage(message);
        }
    }
}
