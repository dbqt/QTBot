using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace QTBot.Helpers
{
    public class Utilities
    {
        /// <summary>
        /// Show a message box to the user.
        /// </summary>
        public static void ShowMessage(string message, string title = "")
        {
            MessageBox.Show(message, title);
        }

        public static string GetDataDirectory()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string installAppData = Path.Combine(localAppData, "QTBot");
            string userFilePath = Path.Combine(installAppData, "UserData");

            if (!Directory.Exists(userFilePath))
            {
                Directory.CreateDirectory(userFilePath);
            }

            return userFilePath;
        }

        public static void Log(string message)
        {
            Trace.WriteLine($"[{DateTime.Now.ToString()}] {message}");
        }
    }
}
