using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QTBot.Core.QTCommandsManager;

namespace QTBot.Models
{
    public class CommandModel
    {
        public string Keyword { get; set; } = "";
        public List<string> Aliases { get; set; } = new List<string>();
        public List<string> Arguments { get; set; } = new List<string>();
        public string Response { get; set; } = "";
        public int StreamElementsFixedCost { get; set; } = 0;
        public bool IsStreamElementsCustomCost { get; set; } = false;
        public int CooldownSeconds { get; set; } = 0;
        public Permission Permission { get; set; } = Permission.Everyone;
    }

    public class CommandsModel
    {
        public List<CommandModel> Commands { get; set; } = new List<CommandModel>();
    }
}
