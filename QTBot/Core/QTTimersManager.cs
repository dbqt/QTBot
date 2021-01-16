using QTBot.Helpers;
using QTBot.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QTBot.Core
{
    public class QTTimersManager
    {
        private const int MinToMilliseconds = 1000 * 60;
        private TimersModel rawTimers;

        private Dictionary<Task, CancellationTokenSource> timerTasks = new Dictionary<Task, CancellationTokenSource>();

        public QTTimersManager()
        {
            // nothing
        }

        public void StartTimers()
        {
            ResetTimers();
        }

        public void StopTimers()
        {
            foreach (var timer in this.timerTasks)
            {
                timer.Value.Cancel();
            }
        }

        private void ResetTimers()
        {
            this.rawTimers = ConfigManager.ReadTimers();

            this.timerTasks.Clear();

            if (this.rawTimers != null)
            {
                foreach (var rawTimer in this.rawTimers.Timers)
                {
                    var cancellationToken = new CancellationTokenSource();
                    Utilities.Log($"QTTimersManager [{rawTimer.Name}] - Registered!");
                    this.timerTasks.Add(TimerMessage(rawTimer.Name, rawTimer.OffsetMin, rawTimer.DelayMin, rawTimer.Message, cancellationToken), cancellationToken);
                }
            }
        }

        private async Task TimerMessage(string name, int startDelay, int cycleDelay, string message, CancellationTokenSource token)
        {
            Utilities.Log($"QTTimersManager [{name}] - Start delayed by {startDelay} min");
            await Task.Delay(startDelay * MinToMilliseconds);

            while (true)
            {
                if (token.IsCancellationRequested)
                {
                    Utilities.Log($"QTTimersManager [{name}] - Cancelled");
                    return;
                }

                Utilities.Log($"QTTimersManager [{name}] - Sending message: {message}");
                QTChatManager.Instance.SendInstantMessage(message);

                Utilities.Log($"QTTimersManager [{name}] - Waiting for next cycle in {cycleDelay} min");
                await Task.Delay(cycleDelay * MinToMilliseconds);
            }
        }
    }
}
