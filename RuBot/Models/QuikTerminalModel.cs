﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using RuBot.Utils;
using RuBot.ViewModels.Strategies;
using QuikSharp;
using QuikSharp.DataStructures;
using QuikSharp.DataStructures.Transaction;
using System.Security;
using System.Text;
using System.IO;

namespace RuBot.Models.Terminal
{
    public class QuikTerminalModel
    {
        public Quik _quik;
        private readonly string _classCode;
        public long OpenSiPosition;
        public long OpenEuPosition;
        public long OpenSbrfPosition;
        public long OpenBrPosition;
        public long OpenSilvPosition;
        public long OpenGazrPosition;
        public long OpenVtbrPosition;
        public SecurityInfo SiSecurity => SiManager?.CurrentSecurity;
        public SecurityInfo EuSecurity => EuManager?.CurrentSecurity;
        public SecurityInfo SbrfSecurity => SbrfManager?.CurrentSecurity;
        public SecurityInfo GazrSecurity => GazrManager?.CurrentSecurity;
        public SecurityInfo BrSecurity => BrManager?.CurrentSecurity;
        public SecurityInfo SilvSecurity => SilvManager?.CurrentSecurity;
        public SecurityInfo VtbrSecurity => VtbrManager?.CurrentSecurity;
        public SecurityManager SiManager;
        public SecurityManager EuManager;
        public SecurityManager SbrfManager;
        public SecurityManager BrManager;
        public SecurityManager SilvManager;
        public SecurityManager GazrManager;
        public SecurityManager VtbrManager;
        private List<SecurityManager> securityManagers= new List<SecurityManager>();
        public List<BaseStrategy> Strategies = new List<BaseStrategy>();
        public ObservableCollection<string> Errors = new ObservableCollection<string>();
        public event Action OnStarted;
        public event Action OnConnected;
        public event Action OnFuturesClientHolding;
        public event Action<string> OnProcessDataError;
        private readonly ConcurrentQueue<AllTrade> _allTradesQueue = new ConcurrentQueue<AllTrade>();
        private long _allTradeFlag;
        private bool isConnected = false;

        private readonly int _quikPort;

        public QuikTerminalModel(int quikPort, string classCode)
        {
            _classCode = classCode;
            _quikPort = quikPort;
            var connectionMonitorThread = new Thread(() => 
            {
                while (true)
                {
                    Thread.Sleep(60000);
                    var now = DateTime.Now;
                    if (now.DayOfWeek == DayOfWeek.Sunday || now.DayOfWeek == DayOfWeek.Saturday)
                        continue;
                    if (now.Hour < 7)
                        continue;
                    if (!_quik.Service.IsConnected().Result)
                    {
                        Logger.Log("Terminal is disconnected");
                    }
                }
            });
        }

        public void Connect()
        {
            var thread = new Thread(() =>
            {
                _quik = new Quik(_quikPort);
                var secs = (int)(DateTime.Now - DateTime.Today).TotalMinutes;
                _quik.Service.InitializeCorrelationId(secs);
                var events = _quik.Events;
                events.OnClose += Events_OnClose;
                events.OnConnected += Events_OnConnected;
                events.OnAllTradesLoaded += Events_OnConnectedToQuik;
                events.OnDisconnected += Events_OnDisconnected;
                events.OnFuturesClientHolding += Events_OnFuturesClientHolding; 
                events.OnAllTrade += ProcessTic;
                events.OnTransReply += Events_OnTransReply;
                events.OnTrade += t => Strategies.ForEach(s => s.NewTrade(t));
                Logger.LogDebug("Starting quikService");
                _quik.Service.QuikService.Start();
            });
            thread.Start();
        }

        private void Events_OnConnectedToQuik()
        {
            var thread = new Thread(() =>
            {
                Logger.LogDebug($"Connected to Quik");
                if (SiManager != null)
                    return;
                var cf = _quik.Class;
                SiManager = new SecurityManager("Si", cf, _classCode, _quik.Trading);
                EuManager = new SecurityManager("Eu", cf, _classCode, _quik.Trading);
                SbrfManager = new SecurityManager("SR", cf, _classCode, _quik.Trading);
                BrManager = new SecurityManager("BR", cf, _classCode, _quik.Trading);
                SilvManager = new SecurityManager("SV", cf, _classCode, _quik.Trading);
                GazrManager = new SecurityManager("GZ", cf, _classCode, _quik.Trading);
                VtbrManager = new SecurityManager("VB", cf, _classCode, _quik.Trading);
                securityManagers.AddRange(new [] { SiManager, EuManager, SbrfManager, BrManager, GazrManager, VtbrManager, SilvManager});
                Logger.LogDebug("QuikTerminalModel connected");
                OnConnected?.Invoke();
                securityManagers.ForEach(sm => sm.Work = true);
                isConnected = true;
            });
            thread.Start();
        }

        private void Events_OnFuturesClientHolding(FuturesClientHolding futPos)
        {
            var net = (long) futPos.totalNet;
            Logger.LogDebug($"Net position {futPos.secCode}: {net}");
            if (SiSecurity != null && futPos.secCode == SiSecurity.SecCode)
            {
                if (OpenSiPosition != net)
                    OpenSiPosition = (long) futPos.totalNet;
            }
            else if (EuSecurity != null && futPos.secCode == EuSecurity.SecCode)
            {
                if (OpenEuPosition != net)
                    OpenEuPosition = (long)futPos.totalNet;
            }
            else if (SbrfSecurity != null && futPos.secCode == SbrfSecurity.SecCode)
            {
                if (OpenSbrfPosition != net)
                    OpenSbrfPosition = (long)futPos.totalNet;
            }
            else if (BrSecurity != null && futPos.secCode == BrSecurity.SecCode)
            {
                if (OpenBrPosition != net)
                    OpenBrPosition = (long)futPos.totalNet;
            }
            else if (GazrSecurity != null && futPos.secCode == GazrSecurity.SecCode)
            {
                if (OpenGazrPosition != net)
                    OpenGazrPosition = (long)futPos.totalNet;
            }
            else if (SilvSecurity != null && futPos.secCode == SilvSecurity.SecCode)
            {
                if (OpenSilvPosition != net)
                    OpenSilvPosition = (long)futPos.totalNet;
            }
            else if (VtbrSecurity != null && futPos.secCode == VtbrSecurity.SecCode)
            {
                if (OpenVtbrPosition != net)
                    OpenVtbrPosition = (long)futPos.totalNet;
            }
            OnFuturesClientHolding?.Invoke();
        }

        public long GetPositions(SecurityInfo security)
        {
            var result = _quik.Trading.GetFuturesHolding(SecurityManager.Account.Firmid, SecurityManager.Account.TrdaccId, security.SecCode, 0).Result;
            long currentPosition = 0;
            if (result != null)
            {
                currentPosition = (long)result.totalNet;
            }
            var position = Strategies.Where(s => s.Security == security)
                .Sum(strat => strat.MakeDeals ? strat.LastValue * strat.InitialVolume : 0);
            return currentPosition;
        }

        private void Events_OnDisconnected()
        {

            OnProcessDataError("Terminal is disconnected");
            isConnected = false;
            securityManagers.ForEach(sm => sm.Work = false);
        }

        private void Events_OnConnected()
        {
            OnProcessDataError("Terminal is connected");
            Strategies.ForEach(s => s.Serialize());
            isConnected = true;
            securityManagers.ForEach(sm => sm.Work = true);
        }

        private void Events_OnClose()
        {
            OnProcessDataError("Terminal is closing...");
        }

        private void Events_OnTransReply(TransactionReply transReply)
        {
            switch(transReply.Status)
            {
                /// «0» - транзакция отправлена серверу, 
                /// «1» - транзакция получена на сервер QUIK от клиента, 
                /// «3» - транзакция выполнена, 
                case 0:
                case 1:
                case 3:
                    break;
                case 2:
                    Logger.Log("Заявка не зарегистрирована: Шлюз не подключен " + transReply.ResultMsg);
                    break;
                case 4:
                    Logger.Log("Заявка не зарегистрирована: " + transReply.ResultMsg);
                    break;
                case 5:
                    Logger.LogDebug("Заявка не прошла проверку Quik: " + transReply.ResultMsg);
                    break;
                case 6:
                    Logger.Log("Заявка не прошла проверку лимитов Quik: " + transReply.ResultMsg);
                    break;
                case 10:
                    Logger.Log("Заявка не поддерживается торговой системой: " + transReply.ResultMsg);
                    break;
                case 11:
                    Logger.Log("Заявка не прошла проверку электронной подписи: " + transReply.ResultMsg);
                    break;
                case 12:
                    Logger.Log("Истек таймаут: " + transReply.ResultMsg);
                    break;
                case 13:
                    Logger.Log("Кросс сделка: " + transReply.ResultMsg);
                    break;
            }
        }
        private void ProcessTic(AllTrade trade)
        {
            _allTradesQueue.Enqueue(trade);
            if (Interlocked.Increment(ref _allTradeFlag) != 1)
                return;
            var thread = new Thread(() =>
            {
                while (!isConnected || securityManagers.Any(sm => sm.CurrentSecurity == null))
                { Thread.Sleep(1000);}
                while (_allTradesQueue.TryDequeue(out var outTrade))
                {
                    var sm = securityManagers.FirstOrDefault(s => outTrade.SecCode.StartsWith(s.Type));
                    if (sm != null)
                        sm.ProcessTic(outTrade);
                    if (_allTradesQueue.IsEmpty)
                        Thread.Sleep(100);
                }
                Interlocked.Exchange(ref _allTradeFlag, 0);
            });
            thread.Start();
        }

        public void Close()
        {
            _quik?.StopService();
        }

        public void Start()
        {
            Strategies.ForEach(s =>
                {
                    if (s.IsValid()) return;
                    MessageBox.Show("Ошибка валидации стратегий.");
                    Application.Current.Shutdown();
                });
            OnStarted?.Invoke();
            Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() => {
                OpenSiPosition = GetPositions(SiSecurity);
                OpenEuPosition = GetPositions(EuSecurity);
                OpenSbrfPosition = GetPositions(SbrfSecurity);
                OpenBrPosition = GetPositions(BrSecurity);
                OpenSilvPosition = GetPositions(SilvSecurity);
                OpenGazrPosition = GetPositions(GazrSecurity);
                OpenVtbrPosition = GetPositions(VtbrSecurity);
                OnFuturesClientHolding?.Invoke();
            }));
        }
    }
}