using Microsoft.Extensions.Logging;
using QTBot.Helpers;
using QTBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public event EventHandler<OnListenResponseArgs> OnListenResponse;
        public event EventHandler<OnStreamUpArgs> OnStreamUpResponse;
        public event EventHandler<OnStreamDownArgs> OnStreamDownResponse;

        public event EventHandler<OnJoinedChannelArgs> OnJoinedChannelResponse;
        public event EventHandler<OnBeingHostedArgs> OnBeingHostResponse;
        public event EventHandler<OnHostingStartedArgs> OnHostingStartedResponse;

        private EventsModel rawEventsModel = null;
        private Dictionary<EventType, List<EventModel>> events = null;

        private List<string> greetedUsers = null;

        public QTEventsManager()
        {
            rawEventsModel = ConfigManager.ReadEvents();
            if (rawEventsModel != null)
            {
                events = new Dictionary<EventType, List<EventModel>>();
                foreach (var eventItem in rawEventsModel.Events.Where(item => item.Active))
                {
                    // Initialize list of events if it's the first of that type
                    if (!events.ContainsKey(eventItem.Type))
                    {
                        events.Add(eventItem.Type, new List<EventModel>());
                    }

                    events[eventItem.Type].Add(eventItem);
                    Utilities.Log(LogLevel.Information, $"QTEventsManager - Registered event for {eventItem.Type}, message: {eventItem.Message}, option: {eventItem.Option}");
                }
            }
            else
            {
                Utilities.Log(LogLevel.Information, $"QTEventsManager - Could not read events!");
            }

            greetedUsers = new List<string>();
        }

        #region Core Events
        public void OnMessageReceivedEvent(OnMessageReceivedArgs args)
        {
            OnMessageReceived?.Invoke(this, args);

            CheckGreetings(args.ChatMessage.DisplayName);
        }

        public void OnNewSubscriberEvent(OnChannelSubscriptionArgs args)
        {
            OnChannelSubscription?.Invoke(this, args);

            if (!events.ContainsKey(EventType.Subscription))
            {
                return;
            }

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

            foreach (var subEvent in events[EventType.Subscription])
            {
                SendEventMessageInChat(subEvent, tokenReplacements);
            }
        }

        public void OnRaidedEvent(OnRaidNotificationArgs args)
        {
            OnRaid?.Invoke(this, args);

            if (!events.ContainsKey(EventType.Raid))
            {
                return;
            }

            var tokenReplacements = new List<KeyValuePair<string, string>>();
            tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.RaidNotification.MsgParamDisplayName));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{count}}", args.RaidNotification.MsgParamViewerCount));

            foreach (var raidEvent in events[EventType.Raid])
            {
                SendEventMessageInChat(raidEvent, tokenReplacements);
            }
        }

        public void OnBitsReceivedEvent(OnBitsReceivedArgs args)
        {
            OnBitsReceived?.Invoke(this, args);

            if (!events.ContainsKey(EventType.Bits))
            {
                return;
            }

            var tokenReplacements = new List<KeyValuePair<string, string>>();
            tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.Username));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{bits}}", args.BitsUsed.ToString()));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{total_bits}}", args.TotalBitsUsed.ToString()));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{message}}", args.ChatMessage));

            foreach (var bitEvent in events[EventType.Bits])
            {
                SendEventMessageInChat(bitEvent, tokenReplacements);
            }
        }

        public void OnRewardRedeemedEvent(OnRewardRedeemedArgs args)
        {
            OnRewardRedeemed?.Invoke(this, args);

            if (!events.ContainsKey(EventType.Redeem))
            {
                return;
            }

            var tokenReplacements = new List<KeyValuePair<string, string>>();
            tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.DisplayName));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{reward}}", args.RewardTitle));
            tokenReplacements.Add(new KeyValuePair<string, string>("{{message}}", args.Message));

            // Filter only redeems with matching title
            foreach (var redeemEvent in events[EventType.Redeem].Where(redeem => redeem.Option.ToLowerInvariant() == args.RewardTitle.ToLowerInvariant()))
            {
                SendEventMessageInChat(redeemEvent, tokenReplacements);
            }
        }

        public void OnNewFollowerEvent(OnFollowArgs args)
        {
            OnFollow?.Invoke(this, args);

            if (!events.ContainsKey(EventType.Follow))
            {
                return;
            }

            var tokenReplacements = new List<KeyValuePair<string, string>>();
            tokenReplacements.Add(new KeyValuePair<string, string>("{{user}}", args.DisplayName));

            foreach (var followEvent in events[EventType.Follow])
            {
                SendEventMessageInChat(followEvent, tokenReplacements);
            }
        }

        public void OnEmoteOnlyOnEvent(OnEmoteOnlyArgs args)
        {
            OnEmoteOnly?.Invoke(this, args);
        }

        public void OnEmoteOnlyOffEvent(OnEmoteOnlyOffArgs args)
        {
            OnEmoteOnlyOff?.Invoke(this, args);
        }

        public void OnListenResponseEvent(OnListenResponseArgs args)
        {
            OnListenResponse?.Invoke(this, args);
        }

        public void OnStreamUpResponseEvent(OnStreamUpArgs args)
        {
            OnStreamUpResponse?.Invoke(this, args);
        }

        public void OnStreamDownResponseEvent(OnStreamDownArgs args)
        {
            OnStreamDownResponse?.Invoke(this, args);
        }

        public void OnJoinedChannelResponseEvent(OnJoinedChannelArgs args)
        {
            OnJoinedChannelResponse?.Invoke(this, args);
        }

        public void OnBeingHostedResponseEvent(OnBeingHostedArgs args)
        {
            OnBeingHostResponse?.Invoke(this, args);
        }

        public void OnHostingStartedResponseEvent(OnHostingStartedArgs args)
        {
            OnHostingStartedResponse?.Invoke(this, args);
        }

        #endregion Core Events

        private void CheckGreetings(string username)
        {
            if (!events.ContainsKey(EventType.Greeting))
            {
                return;
            }

            // Get greeting only if it was setup for this specific user
            var greetEvents = events[EventType.Greeting].Where(item => item.Option.ToLowerInvariant() == username.ToLowerInvariant());
            if (greetEvents.Count() > 0)
            {
                // But only if the user was never greeted
                if (!greetedUsers.Contains(username))
                {
                    // Add the user to the list so we don't greet them again
                    greetedUsers.Add(username);

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
            Utilities.Log(LogLevel.Information, $"QTEventsMananger - sending: {message}");
            _ = QTChatManager.Instance.SendMessage(message);
        }
    }
}
