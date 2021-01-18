using QTBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public void ToggleChat(bool active)
        {
            if (this.isActive == active)
            {
                // Early return because there is no change in state
                return;
            }

            this.isActive = active;
            if (this.isActive)
            {
                _ = InitiateRedeemsLoop();
            }
        }

        public async Task SendMessage(string message, int delayMs = 0)
        {
            await Task.Delay(delayMs);
            SendInstantMessage(message);
        }

        public void SendInstantMessage(string message)
        {
            this.client.SendMessage(QTCore.Instance.CurrentChannel, message);
        }

        public void QueueRedeemAlert(string title, string user)
        {
            lock (redeemLock)
            {
                if (!this.redemptionsCollection.ContainsKey(title))
                {
                    this.redemptionsCollection.Add(title, new List<string>());
                }

                this.redemptionsCollection[title].Add(user);
                this.redemptionsCollectionDirty = true;
            }
        }

        /// <summary>
        /// Groups redeems into a cleaner message
        /// </summary>
        /// <returns></returns>
        private async Task InitiateRedeemsLoop()
        {
            while(this.isActive)
            {
                this.redemptionsCollectionDirty = false;
                await Task.Delay(CycleDelay);

                // If the redemption collection got dirty, that means a new redeem came in during the delay
                if (this.redemptionsCollectionDirty)
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
            if (this.redemptionsCollection.Count == 0)
            {
                return;
            }

            Dictionary<string, int> redeemCounter = new Dictionary<string, int>();
            HashSet<string> names = new HashSet<string>();

            foreach (var redemption in this.redemptionsCollection)
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

            this.redemptionsCollection.Clear();

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
