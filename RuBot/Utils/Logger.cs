using RuBot.Properties;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RuBot.Utils
{
    public class Logger
    {
        private static readonly object LockLogFile = new object();

        private static int _logCount;
        private static DateTime _logTime;
        private static string telegramApiKey = Settings.Default.tg_api_key;
        private static string telegramCritApiKey = Settings.Default.tg_crit_api_key;
        private static string telegramChatId = @"371924007";
        public static string MessageTitle = GetLocalIPAddress();

        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            LogDebug("No network adapters with an IPv4 address in the system!");
            return String.Empty;
        }
        public static void LogDebug(string toDebug)
        {
            toDebug = toDebug.TrimEnd('\r', '\n');
            lock (LockLogFile)
            {
                File.AppendAllText(DateTime.Today.ToShortDateString() + ".txt",
                    $"{DateTime.Now}: {toDebug}\n",
                                   Encoding.UTF8);
            }
        }

        public static void LogException(Exception exception)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                var exp = exception;
                var errorMessage = exp.Message + "\n" + "Source: " + exp.Source + "\n" + "StackTrace: \n" + exp.StackTrace;

                exp = exp.InnerException;
                while (exp != null)
                {
                    errorMessage += "InnerException: \n" +
                                    "  Message: " + exp.Message + "\n" +
                                    "  Source: " + exp.Source + "\n" +
                                    "  StackTrace: \n" + exp.StackTrace;
                    exp = exp.InnerException;
                }
                Log(errorMessage);
            });
        }

        public static void Log(string toLogging)
        {
            if (_logCount == 0)
            {
                _logTime = DateTime.Now;
            }
            else
            {
                if (DateTime.Now - _logTime > TimeSpan.FromMinutes(1))
                {
                    _logCount = 0;
                    _logTime = DateTime.Now;
                }
                else if (_logCount > 10)
                    return;
            }
            _logCount++;
            var thread = new Thread(()=>
                {
                    try
                    {
                        var innerThread = new Thread(()=>
                        {
                            try
                            {
                                LogDebug(toLogging);
                                SendTelegramMessage(toLogging);
                            }
                            catch (Exception exp)
                            {
                                LogDebug(exp.Message + "\n" + "Source: " + exp.Source + "\n" + "StackTrace: \n" + exp.StackTrace);
                            }
                            LogDebug(toLogging);
                        });
                        innerThread.Start();
                        if (!innerThread.Join(TimeSpan.FromMinutes(2)))
                            innerThread.Abort();
                    }
                    catch (Exception exp)
                    {
                        LogDebug(exp.Message + "\n" + "Source: " + exp.Source + "\n" + "StackTrace: \n" + exp.StackTrace);
                    }
                });
            thread.Start();
        }

        public static void SendTelegramMessage(string message)
        {
            var urlString = $"https://api.telegram.org/bot{telegramApiKey}/sendMessage?chat_id={telegramChatId}&text={message}";
            try
            {
                using (var webclient = new WebClient())
                {
                    webclient.DownloadString(urlString);
                }
            }
            catch (Exception exp)
            {
                LogDebug(urlString + "\n" + exp.Message + "\n" + "Source: " + exp.Source + "\n" + "StackTrace: \n" + exp.StackTrace);
            }
        }
        public static void SendCritTelegramMessage(string message)
        {
            var urlString = $"https://api.telegram.org/bot{telegramCritApiKey}/sendMessage?chat_id={telegramChatId}&text={message}";
            try
            {
                using (var webclient = new WebClient())
                {
                    webclient.DownloadString(urlString);
                }
            }
            catch (Exception exp)
            {
                LogDebug(urlString + "\n" + exp.Message + "\n" + "Source: " + exp.Source + "\n" + "StackTrace: \n" + exp.StackTrace);
            }
            Log(message);
        }
    }
}