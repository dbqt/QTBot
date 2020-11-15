using QTBot.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTBot.Core
{
    public class QTCommandsManager
    {
        private const bool ContributeOn = true;

        public async Task<string> ProcessCommand(string command, IEnumerable<string> args, string username)
        {
            string message = "";
            Debug.WriteLine($"From {username}, command is {command}, args are :");
            foreach (var arg in args)
            {
                Debug.WriteLine($"{arg}");
            }

            // Contribute command
            if (ContributeOn && string.Equals(command, "!contribute", StringComparison.OrdinalIgnoreCase))
            {
                // Has argument
                if (args.FirstOrDefault() != null)
                {
                    int points = await StreamElementsModule.Instance.GetPoints(username);
                    int counter = await StreamElementsModule.Instance.GetCounter("squats");

                    // Is contributing all
                    if (args.FirstOrDefault().Equals("all", StringComparison.OrdinalIgnoreCase))
                    {
                        var amount = points;
                        points = await StreamElementsModule.Instance.UpdatePoints(username, -amount);
                        QTChatManager.Instance.SendInstantMessage($"!updatecount squats {counter + amount}");
                        message = $"! @{username} contributed {amount} to the goal, which is all they had!";
                    }
                    // Is contributing an amount
                    else if (int.TryParse(args.FirstOrDefault(), out int amount))
                    {
                        // Amount is positive 
                        if (amount >= 0)
                        {
                            // And user actually has that amount
                            if (amount <= points)
                            {
                                points = await StreamElementsModule.Instance.UpdatePoints(username, -amount);
                                QTChatManager.Instance.SendInstantMessage($"!updatecount squats {counter + amount}");
                                message = $"! @{username} contributed {amount} to the goal and now has {points} left";
                            }
                            else
                            {
                                message = $"! Poor you @{username}, you only have {points}, can't contribute {amount}! Math pls.";
                            }
                        }
                        else
                        {
                            message = $"! You can't fool me, don't try to contribute a negative amount! @{username}";
                        }
                    }   
                    else
                    {
                        message = $"! @{username}, you didn't specify how much you want to contribute";
                    }
                }
                else
                {
                    message = $"! @{username}, you didn't specify how much you want to contribute";
                }
            }

            return message;
        }
    }
}
