using Squirrel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace QTBot.Helpers
{
    public class Utilities
    {
        public static bool IsDebugEnabled = true;

        /// <summary>
        /// Show a message box to the user.
        /// </summary>
        public static void ShowMessage(string message, string title = "")
        {
            //MessageBox.Show(message, title);

            MainContent.Instance.ShowSimpleDialog(message, title);
        }

        /// <summary>
        /// Gets the path to the data directory of the app.
        /// </summary>
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

        /// <summary>
        /// Logs a message into the log file stating the message severity level (in accordance with microsoft)
        /// </summary>
        /// <param name="level">message severity level</param>
        /// <param name="message">log message</param>
        public static void Log(LogLevel level, string message)
        {
            string lvl = Enum.GetName(typeof(LogLevel), level);
            if (level == LogLevel.Critical || level == LogLevel.Error)
            {
                lvl = lvl.ToUpper(new CultureInfo("en-US", false));
            }
            if(IsDebugEnabled || (level != LogLevel.Information && level != LogLevel.Debug))
            {
                Trace.WriteLine($"[{DateTime.Now.ToString()}] - [{lvl}] : {message}");
            }            
        }

        /// <summary>
        /// Logs a message into the log file stating the message and stack of the exception and an Error severity level (in accordance with microsoft)
        /// </summary>
        /// <param name="e">the exception to log</param>
        public static void Log(Exception e)
        {            
            Log(LogLevel.Error, $"Message: { e.Message}, Stack: { e.StackTrace}");
        }

        /// <summary>
        /// Logs a message into the log file with a default severity level of Information(in accordance with microsoft)
        /// </summary>
        /// <param name="message">log message</param>
        public static void Log(string message)
        {
            Log(LogLevel.Information, message);
        }

        /// <summary>
        /// Invoke the speficied action on the UI thread.
        /// </summary>
        public static void ExecuteOnUIThread(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
        }

        /// <summary>
        /// Starts the auto-update process in the background.
        /// </summary>
        public static async Task UpdateApplication()
        {
            using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/dbqt/QTBot-releases"))
            {
                var updateInfo = await mgr.CheckForUpdate();
                var hasUpdate = updateInfo.CurrentlyInstalledVersion.Version != updateInfo.FutureReleaseEntry.Version;

                await mgr.UpdateApp();

                if (hasUpdate)
                {
                    Utilities.ExecuteOnUIThread(() =>
                        Utilities.ShowMessage("Update installed, please reboot me :)", "QTBot has updated")
                    );
                }
            }
        }

        /// <summary>
        /// Check the server to see if there is an update.
        /// </summary>
        /// <returns></returns>
        public static async Task<bool> CheckForUpdate()
        {
            using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/dbqt/QTBot-releases"))
            {
                var updateInfo = await mgr.CheckForUpdate();

                // We hit this when debugging
                if (updateInfo.CurrentlyInstalledVersion?.Version == null)
                {
                    return false;
                }
                
                var hasUpdate = updateInfo.CurrentlyInstalledVersion.Version != updateInfo.FutureReleaseEntry.Version;

                return hasUpdate;
            }
        }

        /// <summary>
        /// Show a dialog box with the configured <see cref="DialogBoxOptions"/>.
        /// </summary>
        public static void ShowDialog(DialogBoxOptions options)
        {
            MainContent.Instance.ShowDialog(options);
        }

        /// <summary>
        /// Dismiss the current dialog.
        /// </summary>
        public static void DismissDialog()
        {
            MainContent.Instance.DismissDialog();
        }

        /// <summary>
        /// Replaces the each token with the value from the <paramref name="tokenValuePairs"/> in the <paramref name="stringToModify"/> and returns the resulting string.
        /// </summary>
        public static string ReplaceKeywords(string stringToModify, List<KeyValuePair<string, string>> tokenValuePairs)
        {
            string finalString = stringToModify;
            foreach (var tokenValuePair in tokenValuePairs)
            {
                finalString = finalString.Replace(tokenValuePair.Key, tokenValuePair.Value);
            }
            return finalString;
        }

        /// <summary>
        /// Options for dialog box
        /// </summary>
        public class DialogBoxOptions
        {
            public string Title = null;
            public string Message = null;
            public DialogBoxButtonOptions MainButton = null;
            public DialogBoxButtonOptions SecondaryButton = null;
            public bool ShowProgressBar = false;

            public class DialogBoxButtonOptions
            {
                public string Label = null;
                public Action Callback = null;
            }
        }
    }
}
