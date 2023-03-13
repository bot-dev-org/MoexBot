using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using RuBot.Properties;
using RuBot.Utils;
using RuBot.ViewModels.Strategies;
using QuikSharp;
using QuikSharp.DataStructures;

namespace RuBot.Models
{
    public class SecurityManager
    {
        private readonly List<SecurityInfo> _securities = new List<SecurityInfo>();
        public readonly string Type;
        private readonly string _classCode;
        private readonly IClassFunctions _classFunctions;
        private readonly ITradingFunctions _tradingFunctions;
        private Thread _checkTask;
        private readonly CultureInfo _cci = new(Thread.CurrentThread.CurrentCulture.Name)
        { NumberFormat = { NumberDecimalSeparator = "." } };
        private const string PriceMaxParamName = "PRICEMAX";
        private const string PriceMinParamName = "PRICEMIN";
        private const string StepSizeParamName = "SEC_PRICE_STEP";
        public List<BaseStrategy> Strategies = new List<BaseStrategy>();
        private readonly List<AllTrade> _nextSecTrades = new List<AllTrade>();

        public SecurityInfo CurrentSecurity;
        public event Action<AllTrade> OnTic;
        public event Action<List<AllTrade>> OnSecurityStopped;
        public static TradesAccounts Account;
        public bool Work = false;

        private TimeSpan SwitchContractTime = TimeSpan.FromDays(4);
        private DateTime WhenToSwitchContractTime => CurrentSecurity.ExpDate - SwitchContractTime;
        private DateTime WhenToCollectNextContractTradesTime => CurrentSecurity.ExpDate - SwitchContractTime - TimeSpan.FromHours(1);

        static SecurityManager(){
            var utilThread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(60 * 60 * 1000);
                    SyncDateTime();
                }
            })
            {
                IsBackground = true
            };
            utilThread.Start();
        }

        public SecurityManager(string secCode, IClassFunctions classFunctions, string classCode, ITradingFunctions trading)
        {
            _tradingFunctions = trading;
            Type = secCode;
            _classFunctions = classFunctions;
            _classCode = classCode;
            LoadSecurities();
            Account = _classFunctions.GetTradeAccounts().Result.FirstOrDefault(acc => acc.TrdaccId == Settings.Default.ACCID);
        }

        private void LoadSecurities()
        {
            Logger.LogDebug($"LoadSecurities");
            try
            {
                var secCodes = _classFunctions.GetClassSecurities(_classCode).Result;
                SecurityInfo security;
                foreach (var secCode in secCodes)
                {
                    if (secCode.StartsWith(Type))
                    {
                        security = _classFunctions.GetSecurityInfo(_classCode, secCode).Result;
                        _securities.Add(security);
                    }
                }
                try
                {
                    _securities.Sort(Comparison);
                }
                catch (Exception exp)
                {
                    Logger.LogException(exp);
                }
                CurrentSecurity = _securities[0];
                UpdateCurrentSecurityInfoEx();
                Logger.LogDebug($"Current Security: {CurrentSecurity.SecCode} exp: {CurrentSecurity.ExpDate} Securities {string.Join(";", _securities.Select(s => s.SecCode).ToArray())}");
                _checkTask = new Thread(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(10 * 60 * 1000);
                        if (Work)
                            UpdateCurrentSecurityInfoEx();
                    }
                })
                {
                    IsBackground = true
                };
                _checkTask.Start();
                var utilThread = new Thread(() =>
                {
                    while (true)
                    {
                        Thread.Sleep(60 * 60 * 1000);
                        if (Work)
                            UpdateSecurities();
                    }
                })
                {
                    IsBackground = true
                };
                utilThread.Start();
            }
            catch (Exception exp)
            {
                Logger.LogException(exp);
            }
        }
        public static bool SyncDateTime()
        {
            try
            {
                var serviceController = new ServiceController("w32time");

                if (serviceController.Status != ServiceControllerStatus.Running)
                {
                    serviceController.Start();
                }

                Logger.LogDebug("w32time service is running");

                Process processTime = new Process();
                processTime.StartInfo.FileName = "w32tm";
                processTime.StartInfo.Arguments = "/resync";
                processTime.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                processTime.Start();
                processTime.WaitForExit();

                Logger.LogDebug("w32time service has sync local dateTime from NTP server");

                return true;
            }
            catch (Exception exception)
            {
                Logger.LogDebug("unable to sync date time from NTP server " + exception.Message);

                return false;
            }
        }
        private void UpdateSecurities()
        {
            try
            {
                var secCodes = _classFunctions.GetClassSecurities(_classCode).Result;
                foreach (var secCode in secCodes)
                {
                    if (secCode.StartsWith(Type) && _securities.All(s => s.SecCode != secCode))
                    {
                        var newSec = _classFunctions.GetSecurityInfo(_classCode, secCode).Result;
                        if (newSec.ExpDate > CurrentSecurity.ExpDate)
                        {
                            Logger.Log($"Having new Security{secCode}");
                            _securities.Add(newSec);
                        }
                    }
                }
                try
                {
                    _securities.Sort(Comparison);
                }
                catch(Exception exp)
                {
                    Logger.LogException(exp);
                }
                while (true)
                {
                    CurrentSecurity = _securities[0];
                    if ( DateTime.Now > WhenToSwitchContractTime)
                    {
                        Logger.SendCritTelegramMessage($"Changing Security: {CurrentSecurity.SecCode} exp: {CurrentSecurity.ExpDate} Securities {string.Join(";", _securities.Select(s => s.SecCode).ToArray())}");
                        var result = _tradingFunctions.GetFuturesHolding(Account.Firmid, Account.TrdaccId, CurrentSecurity.SecCode, 0).Result;
                        int net;
                        if (result != null)
                        {
                            net = (int) result.totalNet;
                            Logger.LogDebug($"Closing Positions {net}");
                            Strategies[0].OrderHandler.IsAlive = 0;
                            Strategies[0].OrderHandler.UpdatePositions((decimal)CurrentSecurity.CurrentPrice, (decimal)Strategies[0].DeltaPrice, Strategies[0].QuotePeriod, Strategies[0]);
                            var check_count = 0;
                            while (Strategies[0].OrderHandler.GetCurrentPosition(CurrentSecurity) != 0)
                            {
                                Logger.Log($"Waiting for closing positions");
                                Thread.Sleep(2000);
                                check_count++;
                                if (check_count > 600)
                                {
                                    Strategies[0].OrderHandler.UpdatePositions((decimal)CurrentSecurity.CurrentPrice, (decimal)Strategies[0].DeltaPrice, Strategies[0].QuotePeriod, Strategies[0]);
                                    check_count = 0;
                                }
                            }
                        }
                        Logger.LogDebug("Changing secuity");
                        OnSecurityStopped?.Invoke(_nextSecTrades);
                        _nextSecTrades.Clear();
                        _securities.RemoveAt(0);
                        CurrentSecurity = _securities[0];
                        Logger.Log($"New Security {CurrentSecurity.SecCode}");
                        UpdateCurrentSecurityInfoEx();
                        Strategies.ForEach(s => s.Security = CurrentSecurity);
                        if (Strategies.Any())
                        {
                            var orderHandler = new NewOrderHandler(CurrentSecurity, Strategies[0].OrderHandler.TerminalModel, Settings.Default.ACCID);
                            Strategies.ForEach(orderHandler.RegisterStrategy);
                            Strategies[0].OrderHandler.UpdatePositions((decimal)CurrentSecurity.CurrentPrice, (decimal)Strategies[0].DeltaPrice, Strategies[0].QuotePeriod, Strategies[0]);
                        }
                    }
                    else
                        break;
                }
                Logger.LogDebug($"Securities {string.Join(";", _securities.Select(s => s.SecCode).ToArray())}");
            }
            catch (Exception exp)
            {
                Logger.LogException(exp);
            }
        }

        public void ProcessTic(AllTrade trade)
        {
            if (CurrentSecurity.SecCode == trade.SecCode)
            {
                OnTic?.Invoke(trade);
                CurrentSecurity.CurrentPrice = trade.Price;
            }
            if (trade.SecCode.Equals(_securities[1].SecCode) && trade.DateTime > WhenToCollectNextContractTradesTime)
            {
                _nextSecTrades.Add(trade);
                _securities[1].CurrentPrice = trade.Price;
                var ind = _nextSecTrades.FindIndex(t => t.DateTime > trade.DateTime - TimeSpan.FromHours(1));
                if (ind > 0)
                {
                    _nextSecTrades.RemoveRange(0, ind);
                }
            }
        }
        private static int Comparison(SecurityInfo securityInfo, SecurityInfo securityInfo1)
        {
            return int.Parse(securityInfo.MatDate) - int.Parse(securityInfo1.MatDate);
        }

        private void UpdateCurrentSecurityInfoEx()
        {
            CurrentSecurity.MaxPrice = decimal.Parse(_tradingFunctions.GetParamEx(_classCode, CurrentSecurity.SecCode, PriceMaxParamName).Result.ParamValue,
                _cci.NumberFormat);
            CurrentSecurity.MinPrice = decimal.Parse(_tradingFunctions.GetParamEx(_classCode, CurrentSecurity.SecCode, PriceMinParamName).Result.ParamValue,
                _cci.NumberFormat);
            CurrentSecurity.PriceStep = double.Parse(_tradingFunctions.GetParamEx(_classCode, CurrentSecurity.SecCode, StepSizeParamName).Result.ParamValue,
                _cci.NumberFormat);
        }
    }
}
