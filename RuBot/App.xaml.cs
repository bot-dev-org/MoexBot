using System.Windows.Threading;
using RuBot.Utils;

namespace RuBot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        private void ApplicationDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Logger.LogException(e.Exception);
            e.Handled = true;
        }
    }
}