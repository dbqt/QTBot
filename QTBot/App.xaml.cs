using Microsoft.Extensions.Logging;
using QTBot.Helpers;
using System.Windows;
using System.Windows.Threading;

namespace QTBot
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            base.DispatcherUnhandledException += AppDispatcherUnhandledException;
        }

        ~App()
        {
            base.DispatcherUnhandledException -= AppDispatcherUnhandledException;
        }

        private void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Utilities.Log(LogLevel.Error, $"Unhandled Exception: {e.Exception.Message} - {e.Exception.StackTrace}");

            e.Handled = true;

            var errorDialog = new Utilities.DialogBoxOptions()
            {
                Title = "Sorry, I crashed :(",
                Message = $"Tell Dbqt about this: {e.Exception.Message} - {e.Exception.StackTrace}",
                MainButton = new Utilities.DialogBoxOptions.DialogBoxButtonOptions()
                {
                    Label = "Okai... :(",
                    Callback = () => { Application.Current.Shutdown(); }
                }
            };

            Utilities.ShowDialog(errorDialog);
        }
    }
}
