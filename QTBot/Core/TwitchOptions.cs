﻿using QTBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QTBot.Core
{
    public class TwitchOptions
    {
        public bool IsRedemptionInChat;
        public bool IsRedemptionTagUser;
        public string RedemptionTagUser;

        public TwitchOptions() { }

        public TwitchOptions(TwitchOptionsModel model)
        {
            IsRedemptionInChat = model.IsRedemptionInChat;
            IsRedemptionTagUser = model.IsRedemptionTagUser;
            RedemptionTagUser = model.RedemptionTagUser;
        }

        public TwitchOptionsModel GetModel()
        {
            return new TwitchOptionsModel()
            {
                IsRedemptionInChat = this.IsRedemptionInChat,
                IsRedemptionTagUser = this.IsRedemptionTagUser,
                RedemptionTagUser = this.RedemptionTagUser
            };
        }
    }
}