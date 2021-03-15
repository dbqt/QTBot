using QTBot.Models;

namespace QTBot.Core
{
    public class TwitchOptions
    {
        public bool IsRedemptionInChat;
        public bool IsRedemptionTagUser;
        public string RedemptionTagUser;

        public bool IsAutoShoutOutHost;

        public string GreetingMessage;

        public TwitchOptions() { }

        public TwitchOptions(TwitchOptionsModel model)
        {
            IsRedemptionInChat = model.IsRedemptionInChat;
            IsRedemptionTagUser = model.IsRedemptionTagUser;
            RedemptionTagUser = model.RedemptionTagUser;
            IsAutoShoutOutHost = model.IsAutoShoutOutHost;
            GreetingMessage = model.GreetingMessage;
        }

        public TwitchOptionsModel GetModel()
        {
            return new TwitchOptionsModel()
            {
                IsRedemptionInChat = IsRedemptionInChat,
                IsRedemptionTagUser = IsRedemptionTagUser,
                RedemptionTagUser = RedemptionTagUser,
                IsAutoShoutOutHost = IsAutoShoutOutHost,
                GreetingMessage = GreetingMessage
            };
        }
    }
}
