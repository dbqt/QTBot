using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QTBot.Helpers
{
    public class Utilities
    {
        public static void ShowMessage(string message, string title = "")
        {
            MessageBox.Show(message, title);
        }
    }
}
