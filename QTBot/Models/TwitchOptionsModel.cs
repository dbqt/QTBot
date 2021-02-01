using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTBot.Models
{
    public class TwitchOptionsModel
    {
        public bool IsRedemptionInChat { get; set; } = false;
        public bool IsRedemptionTagUser { get; set; } = false;
        public string RedemptionTagUser { get; set; } = "";
        public bool IsAutoShoutOutHost { get; set; } = false;
        public string GreetingMessage { get; set; } = "Hai hai, I'm connected and ready to go!";
    }
}
