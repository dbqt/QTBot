using QTBot.Helpers;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client;

namespace QTBot.Core
{
    class QTChatManager : Singleton<QTChatManager>
    {
        private bool isActive = false;

        private const int CycleDelay = 10000;

        private Dictionary<string, List<string>> redemptionsCollection = new Dictionary<string, List<string>>();
        private bool redemptionsCollectionDirty = false;

        private readonly object redeemLock = new object();

        private TwitchClient client;

        public void Initialize(TwitchClient client)
        {
            this.client = client;
        }

        /// <summary>
        /// Toggle the chat module and starts the redeem loop if going active
        /// </summary>
        public void ToggleChat(bool active)
        {
            if (isActive == active)
            {
                // Early return because there is no change in state
                return;
            }

            isActive = active;
            if (isActive)
            {
                _ = InitiateRedeemsLoop();
            }
        }

        /// <summary>
        /// Sends the <paramref name="message"/> in chat after <paramref name="delayMs"/>
        /// </summary>
        public async Task SendMessage(string message, int delayMs = 0)
        {
            await Task.Delay(delayMs);
            SendInstantMessage(message);
        }

        /// <summary>
        /// Sends the <paramref name="message"/> in chat
        /// </summary>
        public void SendInstantMessage(string message)
        {
            client?.SendMessage(QTCore.Instance.CurrentChannel, message);
        }

        /// <summary>
        /// Queues a redeem with <paramref name="title"/> from <paramref name="user"/> for a delayed alert in chat.
        /// </summary>
        public void QueueRedeemAlert(string title, string user)
        {
            lock (redeemLock)
            {
                if (!redemptionsCollection.ContainsKey(title))
                {
                    redemptionsCollection.Add(title, new List<string>());
                }

                redemptionsCollection[title].Add(user);
                redemptionsCollectionDirty = true;
            }
        }

        /// <summary>
        /// Groups redeems into a cleaner message after long enough has been waited
        /// </summary>
        private async Task InitiateRedeemsLoop()
        {
            while (isActive)
            {
                redemptionsCollectionDirty = false;
                await Task.Delay(CycleDelay);

                // If the redemption collection got dirty, that means a new redeem came in during the delay
                if (redemptionsCollectionDirty)
                {
                    continue;
                }

                lock (redeemLock)
                {
                    GenerateRedeemsAlertMessage();
                }
            }
        }

        private void GenerateRedeemsAlertMessage()
        {
            if (redemptionsCollection.Count == 0)
            {
                return;
            }

            Dictionary<string, int> redeemCounter = new Dictionary<string, int>();
            HashSet<string> names = new HashSet<string>();

            foreach (var redemption in redemptionsCollection)
            {
                // Add redeem with number
                int count = redemption.Value.Count;
                string redeemStr = redemption.Key;
                if (count > 1)
                {
                    redeemStr += "s";
                }

                redeemCounter.Add(redeemStr, count);

                // Add username to unique list
                foreach (var name in redemption.Value)
                {
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }

                redemption.Value.Clear();
            }

            redemptionsCollection.Clear();

            // Create message
            string message = string.Empty;
            foreach (var redeem in redeemCounter)
            {
                // If not the first
                if (!string.IsNullOrEmpty(message))
                {
                    message += ", ";
                }
                message += $"{redeem.Value} {redeem.Key}";
            }

            // Add names
            message += $" redeemed by {string.Join(", ", names)}";

            // Add end tag
            if (QTCore.Instance.TwitchOptions.IsRedemptionTagUser && !string.IsNullOrEmpty(QTCore.Instance.TwitchOptions.RedemptionTagUser))
            {
                message += $" @{QTCore.Instance.TwitchOptions.RedemptionTagUser}";
            }

            SendInstantMessage(message);
        }
    }
}
