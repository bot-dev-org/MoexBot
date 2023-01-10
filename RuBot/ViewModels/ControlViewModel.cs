using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using RuBot.Models.Terminal;
using RuBot.Utils;
using RuBot.Views;

namespace RuBot.ViewModels
{
    public class ControlViewModel : NotifyBase
    {
        public static RoutedCommand Connect = new RoutedCommand("Connect", typeof (ControlViewModel));
        public static RoutedCommand Start = new RoutedCommand("Start", typeof (ControlViewModel));
        public static RoutedCommand SetParameter = new RoutedCommand("SetParameter", typeof(ControlViewModel));
        public static RoutedCommand Quoting = new RoutedCommand("Quoting", typeof(ControlViewModel));
        private readonly QuikTerminalModel _model;
        private bool _isStarted;
        private bool _isConnected;
        private readonly Dispatcher _dispatcher;

        public ControlViewModel(QuikTerminalModel model)
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _model = model;
            _model.OnConnected += () => {
                    _dispatcher.BeginInvoke(new Action(() => {
                        RaisePropertyChanged("OpenSiPosition");
                        RaisePropertyChanged("OpenEuPosition");
                        RaisePropertyChanged("OpenSbrfPosition");
                        RaisePropertyChanged("OpenBrPosition");
                        RaisePropertyChanged("OpenGazrPosition");
                        RaisePropertyChanged("OpenSilvPosition");
                        RaisePropertyChanged("OpenVtbrPosition");
                    }));
                };
            _model.OnFuturesClientHolding += () => {
                _dispatcher.BeginInvoke(new Action(() => {
                    RaisePropertyChanged("OpenSiPosition");
                    RaisePropertyChanged("OpenEuPosition");
                    RaisePropertyChanged("OpenSbrfPosition");
                    RaisePropertyChanged("OpenBrPosition");
                    RaisePropertyChanged("OpenGazrPosition");
                    RaisePropertyChanged("OpenSilvPosition");
                    RaisePropertyChanged("OpenVtbrPosition");
                }));
            };
        }

        public long OpenSiPosition => _model.OpenSiPosition;
        public long OpenEuPosition => _model.OpenEuPosition;
        public long OpenSbrfPosition => _model.OpenSbrfPosition;
        public long OpenBrPosition => _model.OpenBrPosition;
        public long OpenSilvPosition => _model.OpenSilvPosition;
        public long OpenGazrPosition => _model.OpenGazrPosition;
        public long OpenVtbrPosition => _model.OpenVtbrPosition;

        public void ConnectCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            _isConnected = true;
            _model.OnProcessDataError += error => _dispatcher.BeginInvoke(new Action(() =>
            {
                Logger.Log(error);
                _model.Errors.Add(error);
                RaisePropertyChanged("Errors");
            }));
            _model.Connect();
        }

        public void CanConnectCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !(_isStarted || _isConnected);
        }

        public void StartCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            _isStarted = true;
            _model.Start();
        }

        public void CanStartCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            try {
                e.CanExecute = !_isStarted && _model.GazrSecurity != null;
            }catch(AggregateException exp)
            {
                Logger.LogDebug(exp.InnerException?.Message ?? exp.Message);
                e.CanExecute = false;
            }
        }
        public void SetParameterCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            new SetParameter(_model.Strategies).ShowDialog();
        }

        public void CanSetParameterCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _model.Strategies != null && _model.Strategies.Count > 0;
        }

        public void CanQuotingCommand(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = _model.Strategies != null && _model.Strategies.Count > 0;
        }

        public void QuotingCommandHandler(object sender, ExecutedRoutedEventArgs e)
        {
            new Quoting(_model.Strategies).ShowDialog();
        }
    }
}