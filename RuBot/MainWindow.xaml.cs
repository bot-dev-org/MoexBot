using Newtonsoft.Json.Linq;
using RuBot.Models.Indicators;
using RuBot.Models.Terminal;
using RuBot.Properties;
using RuBot.Utils;
using RuBot.ViewModels;
using RuBot.ViewModels.Strategies;
using RuBot.Views;
using RuBot.Views.Strategy;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace RuBot
{
    public partial class MainWindow
    {
        private QuikTerminalModel _terminalModel;

        private DataManager _SiDataManager;
        private DataManager _EuDataManager;
        private DataManager _SbrfDataManager;
        private DataManager _GazrDataManager;
        private DataManager _VtbrDataManager;
        private DataManager _BrDataManager;
        private DataManager _SilvDataManager;

        private Dispatcher _guiDispatcher;

        private Dictionary<string, string> TicsDirectoryMap = new Dictionary<string, string>{
            { "gazr", Settings.Default.GazrTics },
            { "eu", Settings.Default.EuTics },
            { "sbrf", Settings.Default.SbrfTics },
            { "br", Settings.Default.BrTics },
            { "silv", Settings.Default.SilvTics },
            { "si", Settings.Default.SiTics },
            { "vtbr", Settings.Default.VtbrTics }
        };

        private Dictionary<string, string> CandlesDirectoryMap = new Dictionary<string, string>{
            { "gazr", Settings.Default.GazrCandles },
            { "eu", Settings.Default.EuCandles },
            { "sbrf", Settings.Default.SbrfCandles },
            { "br", Settings.Default.BrCandles },
            { "silv", Settings.Default.SilvCandles },
            { "si", Settings.Default.SiCandles },
            { "vtbr", Settings.Default.VtbrCandles }
        };

        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            InitializeComponent();

            BaseStrategy.Password = Encoding.ASCII.GetBytes(@"xANF25u{-|63'?C+&3.J~!}8oSa.8#}l");
            
            // изменяет текущий формат, чтобы нецелое числа интерпретировалось как разделенное точкой.
            var cci = new CultureInfo(Thread.CurrentThread.CurrentCulture.Name)
                          {NumberFormat = {NumberDecimalSeparator = "."}};
            Thread.CurrentThread.CurrentCulture = cci;
        }

        void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Logger.LogException(e.Exception);
            _guiDispatcher.BeginInvoke(new Action(() => _terminalModel.Errors.Add(e.Exception.Message)));
        }

        void CurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exp = (Exception) e.ExceptionObject;
            Logger.LogException(exp);
            _guiDispatcher.BeginInvoke(new Action(() => _terminalModel.Errors.Add(exp.Message)));
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            _terminalModel?._quik?.StopService();
            _terminalModel?.Close();
            _SiDataManager?.Close();
            _EuDataManager?.Close();
            _SbrfDataManager?.Close();
            _GazrDataManager?.Close();
            _VtbrDataManager?.Close();
            _BrDataManager?.Close();
            _SilvDataManager?.Close();
            base.OnClosing(e);
        }
        private void InitializeLSTMStrategies(string pipeName)
        {
            var lstmClient = new LstmClient(pipeName);
            var metadata = new Dictionary<string, Dictionary<int, List<double>>>();
            var a = lstmClient.GetMetadata();
            var o = JObject.Parse(a);
            var r = o.SelectToken("$");
            foreach (var t in r)
            {
                var property = (JProperty)t;
                if (!metadata.ContainsKey(property.Name))
                {
                    metadata.Add(property.Name, new Dictionary<int, List<double>>());
                }
                var tfDict = metadata[property.Name];
                var tf = property.Value.SelectToken("$");
                foreach (var tt in tf)
                {
                    property = (JProperty)tt;
                    var timeframe = int.Parse(property.Name);
                    if (!tfDict.ContainsKey(timeframe))
                    {
                        tfDict.Add(timeframe, new List<double>());
                    }
                    var scDict = tfDict[timeframe];
                    var sc = property.Value.SelectToken("$");
                    foreach (var ttt in sc)
                    {
                        property = (JProperty)ttt;
                        var skipCoeff = double.Parse(property.Name);
                        scDict.Add(skipCoeff);
                    }
                }
            }

            foreach (var ticker in metadata.Keys)
            {
                foreach (var tf in metadata[ticker].Keys)
                {
                    foreach (var sc in metadata[ticker][tf])
                    {
                        var strategy = new LSTMStrategy(ticker, tf, sc, lstmClient, TicsDirectoryMap[ticker]);
                        Tabs.Items.Add(new TabItem { Header = strategy.Name, Content = new ReverseStrategyView(strategy) });
                        if (ticker == "si")
                            _SiDataManager.RegisterStrategy(strategy);
                        else if (ticker == "gazr")
                            _GazrDataManager.RegisterStrategy(strategy);
                        else if (ticker == "eu")
                            _EuDataManager.RegisterStrategy(strategy);
                        else if (ticker == "sbrf")
                            _SbrfDataManager.RegisterStrategy(strategy);
                        else if (ticker == "vtbr")
                            _VtbrDataManager.RegisterStrategy(strategy);
                        else if (ticker == "silv")
                            _SilvDataManager.RegisterStrategy(strategy);
                        else if (ticker == "br")
                            _BrDataManager.RegisterStrategy(strategy);
                    }
                }
            }

        }
        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            _guiDispatcher = Dispatcher;
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            _terminalModel = new QuikTerminalModel(Settings.Default.QuikPort, Settings.Default.ClassCode);
            Logger.LogDebug("Terminal model is created");
            var cvm = new ControlViewModel(_terminalModel);
            Tabs.Items.Add(new TabItem{Header = "Control", Content = new ControlView(cvm)});
            Tabs.SelectedItem = Tabs.Items[0];
            _terminalModel.OnStarted += TerminalModelOnStarted;
            _terminalModel.OnConnected += _terminalModel_OnConnected;

            Logger.LogDebug("Creating Si data manager");
            _SiDataManager = new DataManager(CandlesDirectoryMap["si"], TicsDirectoryMap["si"], "Si");

            Logger.LogDebug("Creating Eu data manager");
            _EuDataManager = new DataManager(CandlesDirectoryMap["eu"], TicsDirectoryMap["eu"], "Eu");

            Logger.LogDebug("Creating Sbrf data manager");
            _SbrfDataManager = new DataManager(CandlesDirectoryMap["sbrf"], TicsDirectoryMap["sbrf"], "Sbrf");

            Logger.LogDebug("Creating Gazr data manager");
            _GazrDataManager = new DataManager(CandlesDirectoryMap["gazr"], TicsDirectoryMap["gazr"], "Gazr");

            Logger.LogDebug("Creating Vtbr data manager");
            _VtbrDataManager = new DataManager(CandlesDirectoryMap["vtbr"], TicsDirectoryMap["vtbr"], "Vtbr");

            Logger.LogDebug("Creating Br data manager");
            _BrDataManager = new DataManager(CandlesDirectoryMap["br"], TicsDirectoryMap["br"], "Br");

            Logger.LogDebug("Creating Silv data manager");
            _SilvDataManager = new DataManager(CandlesDirectoryMap["silv"], TicsDirectoryMap["silv"], "Silv");

            InitializeLSTMStrategies("LSTMServer");

            _terminalModel.Strategies.AddRange(_SiDataManager.Strategies);
            _terminalModel.Strategies.AddRange(_EuDataManager.Strategies);
            _terminalModel.Strategies.AddRange(_SbrfDataManager.Strategies);
            _terminalModel.Strategies.AddRange(_GazrDataManager.Strategies);
            _terminalModel.Strategies.AddRange(_VtbrDataManager.Strategies);
            _terminalModel.Strategies.AddRange(_BrDataManager.Strategies);
            _terminalModel.Strategies.AddRange(_SilvDataManager.Strategies);
        }

        private void _terminalModel_OnConnected()
        {
            var orderHandler = new NewOrderHandler(_terminalModel.SiSecurity, _terminalModel, Settings.Default.ACCID);
            _SiDataManager.Strategies.ForEach(orderHandler.RegisterStrategy);
            
            orderHandler = new NewOrderHandler(_terminalModel.EuSecurity, _terminalModel, Settings.Default.ACCID);
            _EuDataManager.Strategies.ForEach(orderHandler.RegisterStrategy);

            orderHandler = new NewOrderHandler(_terminalModel.SbrfSecurity, _terminalModel, Settings.Default.ACCID);
            _SbrfDataManager.Strategies.ForEach(orderHandler.RegisterStrategy);

            orderHandler = new NewOrderHandler(_terminalModel.GazrSecurity, _terminalModel, Settings.Default.ACCID);
            _GazrDataManager.Strategies.ForEach(orderHandler.RegisterStrategy);

            orderHandler = new NewOrderHandler(_terminalModel.VtbrSecurity, _terminalModel, Settings.Default.ACCID);
            _VtbrDataManager.Strategies.ForEach(orderHandler.RegisterStrategy);

            orderHandler = new NewOrderHandler(_terminalModel.BrSecurity, _terminalModel, Settings.Default.ACCID);
            _BrDataManager.Strategies.ForEach(orderHandler.RegisterStrategy);

            orderHandler = new NewOrderHandler(_terminalModel.SilvSecurity, _terminalModel, Settings.Default.ACCID);
            _SilvDataManager.Strategies.ForEach(orderHandler.RegisterStrategy);

            _terminalModel.Strategies.ForEach(s =>
            {
                s.MakeDeals = true;
            });
            _SiDataManager.Strategies.ForEach(s => s.Security = _terminalModel.SiSecurity);
            _EuDataManager.Strategies.ForEach(s => s.Security = _terminalModel.EuSecurity);
            _SbrfDataManager.Strategies.ForEach(s => s.Security = _terminalModel.SbrfSecurity);
            _GazrDataManager.Strategies.ForEach(s => s.Security = _terminalModel.GazrSecurity);
            _VtbrDataManager.Strategies.ForEach(s => s.Security = _terminalModel.VtbrSecurity);
            _BrDataManager.Strategies.ForEach(s => s.Security = _terminalModel.BrSecurity);
            _SilvDataManager.Strategies.ForEach(s => s.Security = _terminalModel.SilvSecurity);

            _terminalModel.SiManager.OnTic += t => _SiDataManager.ProcessTrade(t);
            _terminalModel.EuManager.OnTic += t => _EuDataManager.ProcessTrade(t);
            _terminalModel.SbrfManager.OnTic += t => _SbrfDataManager.ProcessTrade(t);
            _terminalModel.GazrManager.OnTic += t => _GazrDataManager.ProcessTrade(t);
            _terminalModel.VtbrManager.OnTic += t => _VtbrDataManager.ProcessTrade(t);
            _terminalModel.BrManager.OnTic += t => _BrDataManager.ProcessTrade(t);
            _terminalModel.SilvManager.OnTic += t => _SilvDataManager.ProcessTrade(t);

            _terminalModel.SiManager.OnSecurityStopped += _SiDataManager.ResetSecurity;
            _terminalModel.SbrfManager.OnSecurityStopped += _SbrfDataManager.ResetSecurity;
            _terminalModel.EuManager.OnSecurityStopped += _EuDataManager.ResetSecurity;
            _terminalModel.GazrManager.OnSecurityStopped += _GazrDataManager.ResetSecurity;
            _terminalModel.VtbrManager.OnSecurityStopped += _VtbrDataManager.ResetSecurity;
            _terminalModel.BrManager.OnSecurityStopped += _BrDataManager.ResetSecurity;
            _terminalModel.SilvManager.OnSecurityStopped += _SilvDataManager.ResetSecurity;

            _terminalModel.SiManager.Strategies = _SiDataManager.Strategies;
            _terminalModel.EuManager.Strategies = _EuDataManager.Strategies;
            _terminalModel.SbrfManager.Strategies = _SbrfDataManager.Strategies;
            _terminalModel.GazrManager.Strategies = _GazrDataManager.Strategies;
            _terminalModel.VtbrManager.Strategies = _VtbrDataManager.Strategies;
            _terminalModel.BrManager.Strategies = _BrDataManager.Strategies;
            _terminalModel.SilvManager.Strategies = _SilvDataManager.Strategies;
        }

        private void TerminalModelOnStarted()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                _terminalModel.Strategies.ForEach(s =>
                    {
                        if (s.DeltaPrice < 0)
                            ThreadPool.QueueUserWorkItem(delegate { s.UpdateQuoteParams(); });
                    });
            });
        }
    }
}