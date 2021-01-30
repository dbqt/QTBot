using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTBot.Models
{
    public class TimerModel
    {
        public string Name { get; set; } = "";
        public string Message { get; set; } = "";
        public int DelayMin { get; set; } = -1;
        public int OffsetMin { get; set; } = -1;
        public bool Active { get; set; } = true;
    }

    public class TimersModel
    {
        public List<TimerModel> Timers { get; set; } = new List<TimerModel>();
    }
}
