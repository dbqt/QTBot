using QTBot.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static QTBot.Core.QTEventsManager;

namespace QTBot.Models
{
    public class EventModel
    {
        public EventType Type { get; set; } = EventType.None;
        public string Message { get; set; } = "";
        public string Option { get; set; } = "";
        public bool Active { get; set; } = true;
    }

    public class EventsModel
    {
        public List<EventModel> Events { get; set; } = new List<EventModel>();
    }
}
