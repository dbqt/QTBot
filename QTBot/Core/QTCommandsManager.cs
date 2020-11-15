using QTBot.Helpers;
using QTBot.Models;
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
        private enum PointStatus
        {
            Valid,
            Negative,
            NotEnough
        }

        private CommandsModel rawCommands = null;

        private Dictionary<string, CommandModel> commandsLookup = new Dictionary<string, CommandModel>();

        public QTCommandsManager()
        {
            this.rawCommands = ConfigManager.ReadCommands();
            // Populate commands loop up with aliases
            foreach (var command in this.rawCommands.Commands)
            {
                this.commandsLookup.Add(command.Keyword, command);
                foreach (var alias in command.Aliases)
                {
                    this.commandsLookup.Add(alias, command);
                }
            }
        }

        public async Task<string> ProcessCommand(string command, IEnumerable<string> args, string username)
        {
            string message = "";
            Trace.WriteLine($"From {username}, command is {command}, args are :");
            foreach (var arg in args)
            {
                Trace.WriteLine($"{arg}");
            }

            // Early exit if the command is not found
            if (!this.commandsLookup.ContainsKey(command))
            {
                return null;
            }

            var currentCommand = this.commandsLookup[command];

            // Command has a custom cost, we need at least one argument
            if (currentCommand.IsStreamElementsCustomCost && args.Count() > 0)
            {
                int points = await StreamElementsModule.Instance.GetPoints(username);

                // Is contributing all
                if (args.FirstOrDefault().Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var amount = points;
                    await StreamElementsModule.Instance.UpdatePoints(username, -amount);
                    message = string.Format(currentCommand.Response, args.ToArray());
                }
                // Is contributing an amount
                else if (int.TryParse(args.FirstOrDefault(), out int amount))
                {
                    var validity = HasValidPointsForCommand(points, amount);
                    if (validity == PointStatus.Negative)
                    {
                        message = $"! @{username}, you entered a negative value!";
                    }
                    else if (validity == PointStatus.NotEnough)
                    {
                        message = $"! @{username}, you don't have enough for this!";
                    }
                    else
                    {
                        if (amount != 0)
                        {
                            await StreamElementsModule.Instance.UpdatePoints(username, -amount);
                        }
                        var messageFormat = ReplaceKeywords(currentCommand.Response, username);
                        message = string.Format(messageFormat, args.ToArray());
                    }
                }
                else
                {
                    message = $"! @{username}, you didn't specify how much you want to use!";
                }
            }
            // Fixed cost
            else
            {
                int points = await StreamElementsModule.Instance.GetPoints(username);
                int amount = currentCommand.StreamElementsFixedCost;
                var validity = HasValidPointsForCommand(points, amount);
                if (validity == PointStatus.NotEnough)
                {
                    message = $"! @{username}, you don't have enough for this!";
                }
                else if (validity == PointStatus.Valid)
                {
                    if (amount != 0)
                    {
                        await StreamElementsModule.Instance.UpdatePoints(username, -amount);
                    }
                    var messageFormat = ReplaceKeywords(currentCommand.Response, username);
                    message = string.Format(messageFormat, args.ToArray());
                }
            }

            return message;
        }

        private PointStatus HasValidPointsForCommand(int userTotalPoints, int cost)
        {
            // Amount is positive
            if (cost >= 0)
            {
                // And user actually has that amount
                if (cost <= userTotalPoints)
                {
                    return PointStatus.Valid;
                }
                else
                {
                    return PointStatus.NotEnough;
                }
            }
            else
            {
                return PointStatus.Negative;
            }
        }

        private string ReplaceKeywords(string stringToModify, string username)
        {
            return stringToModify.Replace("{{user}}", username);
        }
    }
}
