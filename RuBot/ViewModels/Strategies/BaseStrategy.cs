using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Data;
using System.Windows.Threading;
using System.Xml.Serialization;
using RuBot.Models.Parameters;
using RuBot.Utils;
using QuikSharp.DataStructures;
using QuikSharp.DataStructures.Transaction;
using Candle = RuBot.Models.Candle;

namespace RuBot.ViewModels.Strategies
{
    public abstract class BaseStrategy : NotifyBase
    {
        #region Fields
        public static object TicsLocker = new object();
        public string TicsDataFolder;
        public const string TicsEndFileName = @"_tics.txt";
        private const int StartPeriod = 10;
        private const int StopPeriod = 200;
        private const int StartSpread = 1;
        private const int StopSpread = 6;
        private const double effThres = 0.11;
        private double _accumClosePriceDiff = 0.0;
        private int _accumVolume = 0;
        public static byte[] Password;
        public double LastDealPrice;
        public DateTime LastTimeCandle;
        public NewOrderHandler OrderHandler;
        private SecurityInfo security;
        public string SettingsFile;
        protected bool cleanChart = true;
        protected bool makeDeals;
        private const double Epsilon = 0.0000001;

        public abstract int LastValue { get; set; }
        public ICollectionView Orders { get; }
        public ICollectionView Trades { get; }
        private readonly ObservableCollection<Order> _orders = new ObservableCollection<Order>();
        private readonly ObservableCollection<Trade> _trades = new ObservableCollection<Trade>();
        public readonly StrategyParameters Parameters = new StrategyParameters();

        private readonly Dispatcher _guiDispatcher;
        public readonly string Name;

        protected abstract Dictionary<DateTime, int> GetDeals();

        public virtual bool ProcessCandle(Candle candle, bool saveParams = true)
        {
            LastTimeCandle = candle.Time;
            LastClosePrice = candle.ClosePrice;
            return true;
        }

        public abstract void SaveParams();
        protected void MakeDeal(Candle candle)
        {
            if (MakeDeals)
            {
                try
                {
                    OrderHandler.UpdatePositions(Convert.ToDecimal(candle.ClosePrice), (decimal)DeltaPrice, QuotePeriod, this);
                }
                catch (Exception exp)
                {
                    Logger.LogException(exp);
                }
            }
            LastDealPrice = LastClosePrice;
        }

        public Candle PreProcessCandle(Candle candle)
        {
            _accumClosePriceDiff += candle.ClosePriceDiff;
            _accumVolume += candle.Volume;
            Candle result = null;
            //Logger.LogDebug($"{Name} skip value: {GetParameters().SkipValue}");
            if (Math.Abs(_accumClosePriceDiff) + double.Epsilon >= candle.ClosePrice * Parameters.SkipValue)
            {
                result = new Candle
                {
                    ClosePrice = candle.ClosePrice,
                    Volume = _accumVolume,
                    ClosePriceDiff = _accumClosePriceDiff,
                    Time = candle.Time
                };
                _accumClosePriceDiff = 0.0;
                _accumVolume = 0;
            }
            else
            {
                if (makeDeals)
                    Logger.LogDebug($"{Name} skips candle: {candle.Time} {_accumClosePriceDiff} {_accumVolume} {Parameters.SkipValue}");
            }
            return result;
        }

        protected double LastClosePrice;
        #endregion
        #region Events
        public event Action<Candle> OnDraw;
        #endregion
        protected BaseStrategy(string name)
        {
            Name = name;
            Orders = CollectionViewSource.GetDefaultView(_orders);
            Trades = CollectionViewSource.GetDefaultView(_trades);
            _guiDispatcher = Dispatcher.CurrentDispatcher;
        }


        public void OrderHandler_OnNewOrder(Order order)
        {
            _guiDispatcher.BeginInvoke(new Action(() => { _orders.Add(order); Orders.Refresh(); }));
        }
        #region Properties
        public double Commission => Parameters.Commission;

        public SecurityInfo Security
        {
            get => security;
            set 
            { 
                security = value;
                RaisePropertyChanged("Security");
            }
        }

        public double DeltaPrice
        {
            get => Parameters.DeltaPrice;
            set
            {
                Parameters.DeltaPrice = value;
                RaisePropertyChanged("DeltaPrice");
            }
        }
        public int PartVolume
        {
            get => Parameters.PartVolume;
            set
            {
                Parameters.PartVolume = value;
                RaisePropertyChanged("PartVolume");
            }
        }
        public int TimeFrame
        {
            get => Parameters.TimeFrame;
            set
            {
                Parameters.TimeFrame = value;
                RaisePropertyChanged("TimeFrame");
            }
        }

        public int QuotePeriod
        {
            get => Parameters.QuotePeriod;
            set
            {
                Parameters.QuotePeriod = value;
                RaisePropertyChanged("QuotePeriod");
            }
        }
        public double RealCurrentCash
        {
            get => Parameters.RealCurrentCash;
            set
            {
                Parameters.RealCurrentCash = value;
                if (RealCurrentCash > RealMaxProfit)
                {
                    RealMaxProfit = RealCurrentCash;
                    RealMaxLastDrawDown = 0;
                    RealLastMaxTime = DateTime.Now;
                    RaisePropertyChanged("RealMaxProfit");
                    RaisePropertyChanged("RealMaxLastDrawDown");
                    RaisePropertyChanged("RealLastMaxTime");
                }
                else
                {
                    if (RealMaxProfit - RealCurrentCash > RealMaxLastDrawDown)
                    {
                        RealMaxLastDrawDown = RealMaxProfit - RealCurrentCash;
                        RaisePropertyChanged("RealMaxLastDrawDown");
                    }
                }
                RaisePropertyChanged("RealCurrentCash");
                RaisePropertyChanged("RealCurrentDrawDown");
            }
        }
        public double RealMaxLastDrawDown
        {
            get => Parameters.RealMaxLastDrawDown;
            set
            {
                Parameters.RealMaxLastDrawDown = value;
                RaisePropertyChanged("RealMaxLastDrawDown");
            }
        }

        public DateTime RealLastMaxTime
        {
            get => Parameters.RealLastMaxTime;
            set
            {
                Parameters.RealLastMaxTime = value;
                RaisePropertyChanged("RealLastMaxTime");
            }
        }
        public double RealMaxProfit
        {
            get => Parameters.RealMaxProfit;
            set
            {
                Parameters.RealMaxProfit = value;
                RaisePropertyChanged("RealMaxProfit");
                RaisePropertyChanged("RealCurrentDrawDown");
            }
        }
        
        protected int _volume;
        public virtual int InitialVolume
        {
            get => _volume;
            set
            {
                _volume = value;
                RaisePropertyChanged("InitialVolume");
            }
        }

        public bool CleanChart
        {
            get => cleanChart;
            set
            {
                if (value == cleanChart)
                    return;
                cleanChart = value;
                RaisePropertyChanged("CleanChart");
            }
        }

        public bool MakeDeals
        {
            get => makeDeals;
            set
            {
                if (value == makeDeals)
                    return;
                makeDeals = value;
                RaisePropertyChanged("MakeDeals");
            }
        }
        public bool IsValid()
        {
            return Security != null;
        }
        #endregion
        #region Private Methods
        private void ProccessQouteTics(int startNum, int offset, string[] files, Dictionary<DateTime, int> deals, QuoteParams[,] results)
        {
            var temp = TicsLocker;
            var lockWasTaken = false;
            try
            {
                var volume = InitialVolume*2;
                var ticList = new List<Tic>();
                for (var i = startNum; i < files.Length; i += offset)
                {
                    ticList.Clear();
                    var file = files[i];
                    var fileName = Path.GetFileName(file);
                    if (fileName == null) return;
                    var date = DateTime.ParseExact(fileName.Substring(0, fileName.Length - fileName.IndexOf(TicsEndFileName) - 1),
                            "yyyyMMdd", CultureInfo.CurrentCulture);
                    if (date == DateTime.Now.Date)
                        Monitor.Enter(temp, ref lockWasTaken);
                    try
                    {
                        ticList = File.ReadAllLines(file).Select(line =>
                            {
                                var parts = line.Split(';');
                                return new Tic
                                {
                                    Time = date + TimeSpan.ParseExact(parts[0], "hhmmss", CultureInfo.InvariantCulture),
                                    Price = double.Parse(parts[1], NumberStyles.AllowDecimalPoint),
                                    Volume = int.Parse(parts[2], CultureInfo.InvariantCulture)
                                };
                            }).ToList();
                    }catch(Exception exp)
                    {
                        Logger.LogDebug($"Need to remove file: {fileName}");
                        ticList = null;
                    }
                    if (date == DateTime.Now.Date && lockWasTaken)
                    {
                        Monitor.Exit(temp);
                        lockWasTaken = false;
                    }
                    if (ticList == null)
                        continue;
                    foreach (var deal in deals)
                    {
                        var dealTime = deal.Key;
                        if (!dealTime.Date.Equals(date))
                            continue;
                        if (dealTime > dealTime.Date + TimeSpan.FromDays(1) - TimeSpan.FromMinutes(40))
                            continue;
                        var lastTic = ticList.LastOrDefault(t => t.Time < dealTime);
                        if (lastTic == null && i > 0)
                        {
                            fileName = Path.GetFileName(files[i - 1]);
                            date = DateTime.ParseExact(fileName.Substring(0, fileName.Length - fileName.IndexOf(TicsEndFileName) - 1),
                                "yyyyMMdd", CultureInfo.CurrentCulture);
                            lastTic = File.ReadAllLines(files[i - 1]).Select(line =>
                            {
                                var parts = line.Split(';');
                                return new Tic
                                {
                                    Time = date + TimeSpan.ParseExact(parts[0], "hhmmss", CultureInfo.InvariantCulture),
                                    Price = double.Parse(parts[1], NumberStyles.AllowDecimalPoint),
                                    Volume = int.Parse(parts[2], CultureInfo.InvariantCulture)
                                };
                            }).ToList().LastOrDefault(t => t.Time < dealTime);
                        }
                        if (lastTic == null)
                            continue;
                        var dayTics = ticList.Where(t => t.Time > dealTime && t.Time < dealTime + TimeSpan.FromMinutes(30)).ToList();
                        if (dayTics.Count < 1)
                            continue;
                        var priceStep = double.MaxValue;
                        if (Security == null)
                        {
                            var prevPrice = 0.0;
                            foreach (var tic in dayTics)
                            {
                                if (Math.Abs(tic.Price - prevPrice) < priceStep)
                                    priceStep = Math.Abs(tic.Price - prevPrice);
                                prevPrice = tic.Price;
                            }
                            Security = new SecurityInfo() { PriceStep = priceStep};
                        }
                        else
                            priceStep = Security.PriceStep;
                        for (var period = 0; period < StopPeriod - StartPeriod; period++)
                            for (var spread = 0; spread < StopSpread - StartSpread; spread++)
                            {
                                var parameters = results[period, spread];
                                var res = CalculateFailed((spread + StartSpread) * priceStep, period + StartPeriod,
                                    lastTic.Price, deal.Value > 0, dayTics, volume, PartVolume);
                                lock (parameters)
                                {
                                    if (Math.Abs(res - double.MinValue) < Epsilon)
                                        parameters.FailedDeals++;
                                    else
                                    {
                                        parameters.Result += res;
                                        parameters.DealsCount++;
                                    }
                                }
                            }
                    }
                }
            }catch(Exception exp)
            {
                Logger.LogException(exp);
            }
            finally
            {
                if (lockWasTaken)
				{
					Monitor.Exit(temp);
				}
            }
        }
        private void HandleRealProfit(double gained)
        {
            Logger.LogDebug($"HandleRealProfit:{gained}");
            RealCurrentCash += gained;
        }
        public void Serialize()
        {
            lock (SettingsFile)
            {
                try
                {
                    var xmlFormat = new XmlSerializer(Parameters.GetType());
                    using (Stream fStream = new MemoryStream())
                    {
                        xmlFormat.Serialize(fStream, Parameters);
                        fStream.Position = 0;
                        var buffer = new byte[fStream.Length];
                        fStream.Read(buffer, 0, buffer.Length);
                        lock (this)
                        {
                            CryptUtils.EncryptFile(new string(Encoding.ASCII.GetChars(buffer)), SettingsFile, Password);
                        }
                    }
                }
                catch (Exception exp)
                {
                    Logger.LogDebug($"Cannot write {SettingsFile}");
                    Logger.LogException(exp);
                }
            }
        }

        #endregion
        #region Protected Methods
        protected void CreateOrder(decimal price)
        {

        }

        protected void HandleProfit(double gained)
        {
            HandleRealProfit(gained);
            Serialize();
        }
        protected void Deserialize()
        {
            if (!File.Exists(SettingsFile))
            {
                UpdateQuoteParams();
                Serialize();
            }
            else
            {
                lock (this)
                {
                    using (var reader = new MemoryStream(Encoding.ASCII.GetBytes(CryptUtils.DecryptFile(SettingsFile, Password))))
                    {
                        var serializer = new XmlSerializer(Parameters.GetType());
                        var result = (StrategyParameters)serializer.Deserialize(reader);
                        Parameters.Commission = result.Commission;
                        Parameters.DeltaPrice = result.DeltaPrice;
                        Parameters.PartVolume = result.PartVolume;
                        Parameters.QuotePeriod = result.QuotePeriod;
                        Parameters.QuoteProfit = result.QuoteProfit;
                        Parameters.RealCurrentCash = result.RealCurrentCash;
                        Parameters.RealLastMaxTime = result.RealLastMaxTime;
                        Parameters.RealMaxLastDrawDown = result.RealMaxLastDrawDown;
                        Parameters.RealMaxProfit = result.RealMaxProfit;
                        Parameters.SkipValue = result.SkipValue;
                        Parameters.TimeFrame = result.TimeFrame;
                    }
                }
            }
        }
        #endregion
        #region Static Methods
        private static double CalculateFailed(double spread, int period, double price, bool isBuy, IList<Tic> tics, int size, int partVolume)
        {
            var startSize = size;
            var result = 0.0;
            var startPrice = price + spread * (isBuy ? -1 : 1);
            var startTime = tics[0].Time;
            var lastDealTime = DateTime.MinValue;
            var lastDealPrice = double.MinValue;
            var dealMade = false;
            foreach (var t in tics)
            {
                if (!dealMade)
                {
                    if (isBuy && t.Price <= startPrice)
                    {
                        var tmpVolume = t.Volume > partVolume ? partVolume : t.Volume;
                        var curSize = size > tmpVolume ? tmpVolume : size;
                        size -= curSize;
                        result += (price - startPrice)*curSize;
                        startPrice = t.Price;
                        lastDealTime = t.Time;
                        lastDealPrice = t.Price;
                        dealMade = true;
                        startTime = t.Time;
                        if (size == 0)
                            return result/startSize;
                    }
                    else if (!isBuy && t.Price >= startPrice)
                    {
                        var tmpVolume = t.Volume > partVolume ? partVolume : t.Volume;
                        var curSize = size > tmpVolume ? tmpVolume : size;
                        size -= curSize;
                        result += (startPrice - price)*curSize;
                        startPrice = t.Price;
                        lastDealTime = t.Time;
                        lastDealPrice = t.Price;
                        dealMade = true;
                        startTime = t.Time;
                        if (size == 0)
                            return result/startSize;
                    }
                }
                else
                {
                    if (Math.Abs(t.Price - lastDealPrice) > Epsilon && t.Time > lastDealTime)
                    {
                        lastDealPrice = double.MinValue;
                        dealMade = false;
                    }
                }
                if (t.Time > startTime + TimeSpan.FromSeconds(period))
                {
                    startPrice = t.Price + spread * (isBuy ? -1 : 1);
                    startTime = t.Time;
                }
            }
            return double.MinValue;
        }
        #endregion
        #region Callbacks
        public void UpdateQuoteParams()
        {
            try
            {
                var deals = GetDeals();
                Logger.LogDebug("Total deals: " + deals.Count);
                var threads = new Thread[Environment.ProcessorCount];
                var results = new QuoteParams[StopPeriod - StartPeriod,StopSpread - StartSpread];
                for (var p = 0; p < StopPeriod - StartPeriod; p++)
                    for (var s = 0; s < StopSpread - StartSpread; s++)
                        results[p, s] = new QuoteParams();
                if (!Directory.Exists(TicsDataFolder))
                    return;
                for (var i = 0; i < Environment.ProcessorCount; i++)
                {
                    var startNum = i;
                    threads[i] = new Thread(() => ProccessQouteTics(startNum, Environment.ProcessorCount,
                                            Directory.GetFiles(TicsDataFolder, @"*" + TicsEndFileName), deals, results));
                    threads[i].Start();
                }
                foreach (var thread in threads)
                {
                    thread.Join();
                }
                var maxResult = double.MinValue;
                var maxPeriod = 60;
                var maxSpread = 0.0;
                var maxEfficency = 1.0;
                for (var p = 0; p < StopPeriod - StartPeriod; p++)
                    for (var s = 0; s < StopSpread - StartSpread; s++)
                    {
                        if (results[p, s].FailedDeals/(double) (results[p, s].DealsCount + results[p, s].FailedDeals) > effThres)
                        {
                            if (maxEfficency > results[p, s].FailedDeals/(double) (results[p, s].DealsCount + results[p, s].FailedDeals))
                                maxEfficency = results[p, s].FailedDeals/(double) (results[p, s].DealsCount + results[p, s].FailedDeals);
                            continue;
                        }
                        var result = results[p, s].Result/results[p, s].DealsCount;
                        if (result <= maxResult) continue;
                        Logger.LogDebug(
                            $"{GetType().Name.Substring(0, 2)} {Name}: Qoute res = {result}, spread = {s + StartSpread}, period = {p + StartPeriod}, failed = {results[p, s].FailedDeals}, deals = {results[p, s].DealsCount}, eff = {results[p, s].FailedDeals/(double) (results[p, s].DealsCount + results[p, s].FailedDeals):0.000}");
                        maxResult = result;
                        maxPeriod = p + StartPeriod;
                        maxSpread = s + StartSpread;
                    }
                if (Math.Abs(maxResult - double.MinValue) < Epsilon)
                {
                    Logger.Log($"Max Quote Efficiency = {maxEfficency}");
                    return;
                }
                maxSpread *= Security.PriceStep;
                DeltaPrice = maxSpread;
                QuotePeriod = maxPeriod;
                Logger.LogDebug(
                    $"{GetType().Name.Substring(0, 2)} {Name}: Qoute res = {maxResult}, spread = {maxSpread}, period = {maxPeriod}");
                Serialize();
            }catch(Exception exp)
            {
                Logger.LogException(exp);
            }
        }

        #endregion
        #region Public Methods
        public void NewTrade(Trade trade)
        {
            _guiDispatcher.BeginInvoke(new Action(() =>
            {
                var order = _orders.FirstOrDefault(o => o.OrderNum == trade.OrderNum);
                if (order == null) return;
                lock (_trades)
                {
                    if (_trades.FirstOrDefault(t => t.TradeNum == trade.TradeNum) != null)
                        return;
                    _trades.Add(trade);
                    Trades.Refresh();
                }
            }));
        }

        public void Draw(Candle c)
        {
            OnDraw?.Invoke(c);
        }
        #endregion
        private class QuoteParams
        {
            public double Result;
            public int DealsCount;
            public int FailedDeals;
        }

        public class Tic
        {
            public double Price;
            public DateTime Time;
            public int Volume;
        }
    }
}