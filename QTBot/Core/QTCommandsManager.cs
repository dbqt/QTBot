﻿using QTBot.Helpers;
using QTBot.Models;
using QTBot.Modules;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace QTBot.Core
{
    public class QTCommandsManager
    {
        public enum Permission
        {
            Everyone = 0,
            //Follower = 1,
            VIP = 2,
            Subscriber = 3,
            Moderator = 4,
            Broadcaster = 5
        }

        private enum PointStatus
        {
            Valid,
            Negative,
            NotEnough
        }

        private CommandsModel rawCommands = null;

        private Dictionary<string, CommandModel> commandsLookup = new Dictionary<string, CommandModel>();

        private Dictionary<string, DateTime> commandsCooldownLookup = new Dictionary<string, DateTime>();

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

        public async Task<string> ProcessCommand(string command, IEnumerable<string> args, OnMessageReceivedArgs messageArgs)
        {
            string username = messageArgs.ChatMessage.DisplayName;
            // Early exit if empty command
            if (string.IsNullOrWhiteSpace(command))
            {
                return null;
            }

            string message = "";
            Utilities.Log($"From {username}, command is {command}, args are :");
            foreach (var arg in args)
            {
                Utilities.Log($"{arg}");
            }

            // Early exit if the command is not found
            if (!this.commandsLookup.ContainsKey(command))
            {
                return null;
            }

            var currentCommand = this.commandsLookup[command];

            // Early exit if the command is on cooldown
            if (this.commandsCooldownLookup.ContainsKey(command))
            {
                var timeDelta = DateTime.Now - this.commandsCooldownLookup[command];
                // Still on cooldown
                if (timeDelta.TotalSeconds < currentCommand.CooldownSeconds)
                {
                    return null;
                }
                // Cooldown is done
                else
                {
                    this.commandsCooldownLookup.Remove(command);
                }
            }

            // Early exit if the user doesn't have permission to use command
            if (GetPermissions(messageArgs.ChatMessage) < currentCommand.Permission)
            {
                return null;
            }

            // Command has a custom cost, we need at least one argument
            if (currentCommand.IsStreamElementsCustomCost && args.Count() > 0)
            {
                int points = await StreamElementsModule.Instance.GetPoints(username);

                // Is contributing all
                if (args.FirstOrDefault().Equals("all", StringComparison.OrdinalIgnoreCase))
                {
                    var amount = points;
                    await StreamElementsModule.Instance.UpdatePoints(username, -amount);

                    // Replace the first argument with actual amount of points
                    var argumentArray = args.ToArray();
                    argumentArray[0] = amount.ToString();

                    message = ReplaceArguments(currentCommand.Response, argumentArray);
                    this.commandsCooldownLookup[command] = DateTime.Now;
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
                        var messageFormat = ReplaceUsername(currentCommand.Response, username);
                        message = ReplaceArguments(messageFormat, args.ToArray());
                        this.commandsCooldownLookup[command] = DateTime.Now;
                    }
                }
                else
                {
                    message = $"! @{username}, you didn't specify how much you want to use!";
                }
            }
            // Fixed cost
            else if (currentCommand.StreamElementsFixedCost > 0)
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
                    var messageFormat = ReplaceUsername(currentCommand.Response, username);
                    message = ReplaceArguments(messageFormat, args.ToArray());
                    this.commandsCooldownLookup[command] = DateTime.Now;
                }
            }
            // No cost
            else
            {
                var messageFormat = ReplaceUsername(currentCommand.Response, username);
                message = ReplaceArguments(messageFormat, args.ToArray());
                this.commandsCooldownLookup[command] = DateTime.Now;
            }

            return message;
        }

        /// <summary>
        /// Checks if the the cost is positive and lower than user's points.
        /// </summary>
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

        private string ReplaceUsername(string stringToModify, string username)
        {
            return Utilities.ReplaceKeywords(stringToModify, new List<KeyValuePair<string, string>>{ new KeyValuePair<string, string>("{{user}}", username)});
        }

        private string ReplaceArguments(string stringToModify, string[] args)
        {
            string result;
            try
            {
                result = string.Format(stringToModify, args);
            }
            catch (Exception e)
            {
                Utilities.Log($"QTCommandsManager - Failed to convert {stringToModify} with {args} because of {e.Message}");
                result = stringToModify;
            }

            return result;
        }

        /// <summary>
        /// Converts permission level of the user that sent the <paramref name="message"/> to <see cref="Permission"/>.
        /// </summary>
        private Permission GetPermissions(ChatMessage message)
        {
            if (message.IsBroadcaster)
            {
                return Permission.Broadcaster;
            }
            else if (message.IsModerator)
            {
                return Permission.Moderator;
            }
            else if (message.IsSubscriber)
            {
                return Permission.Subscriber;
            }
            else if (message.IsVip)
            {
                return Permission.VIP;
            }

            return Permission.Everyone;
        }
    }
}
