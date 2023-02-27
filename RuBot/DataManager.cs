using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RuBot.Utils;
using RuBot.ViewModels.Strategies;
using QuikSharp.DataStructures;
using Candle = RuBot.Models.Candle;

namespace RuBot
{
    public class DataManager
    {
        private readonly string _candlesDataFolder;
        private readonly string _ticsDataFolder;
        private int[] _registeredTimeFrames;
        private int _framesCount;
        private readonly string _name;
        private readonly List<AllTrade> _ticsBuffer = new List<AllTrade>();
        public readonly List<BaseStrategy> Strategies = new List<BaseStrategy>();
        private readonly SortedDictionary<int, List<Candle>> _candlesMap = new SortedDictionary<int, List<Candle>>();
        private readonly SortedDictionary<int, Candle> _candleInProgressMap = new SortedDictionary<int, Candle>();
        private readonly SortedDictionary<int, string> _currentCandlesFileNameMap = new SortedDictionary<int, string>();
        private readonly SortedDictionary<int, DateTime> _lastProcessedCandleTimeMap = new SortedDictionary<int, DateTime>();
        private readonly SortedDictionary<int, List<BaseStrategy>> _strategiesMap = new SortedDictionary<int, List<BaseStrategy>>();
        private readonly SortedDictionary<int, HLV> _HLVMap = new SortedDictionary<int, HLV>();
        private DateTime _lastProcessedTicTime;
        private readonly NumberFormatInfo _commaNFI = new NumberFormatInfo { NumberDecimalSeparator = "," };
        protected const double Epsilon = 0.000000001;
        private bool _finalDayTicsThreadStarted;
        private int _maxTimeFrame = 1;
        private long _allTradeFlag = 0;

        public DataManager(string candleFolder, string ticsFolder, string name)
        {
            _candlesDataFolder = candleFolder;
            _ticsDataFolder = ticsFolder;
            _name = name;
            Initialize();
        }
        private void Initialize()
        {
            if (!Directory.Exists(_candlesDataFolder))
                Directory.CreateDirectory(_candlesDataFolder);
            if (!Directory.Exists(_ticsDataFolder))
                Directory.CreateDirectory(_ticsDataFolder);
            _lastProcessedTicTime = DateTime.MinValue;

            var lastTicsDate = DateTime.Now;
            var lastTicsDateFile = _ticsDataFolder + Path.DirectorySeparatorChar + lastTicsDate.Date.ToString("yyyyMMdd") +
                BaseStrategy.TicsEndFileName;

            while (!File.Exists(lastTicsDateFile))
            {
                lastTicsDate = lastTicsDate.AddDays(-1.0);
                lastTicsDateFile = _ticsDataFolder + Path.DirectorySeparatorChar + lastTicsDate.Date.ToString("yyyyMMdd") +
                    BaseStrategy.TicsEndFileName;
            }
            var fileName = Path.GetFileName(lastTicsDateFile);
            var date = DateTime.ParseExact(fileName.Substring(0, fileName.Length - fileName.IndexOf(BaseStrategy.TicsEndFileName) - 1),
                "yyyyMMdd", CultureInfo.CurrentCulture);
            var lastTics = File.ReadAllLines(lastTicsDateFile).Select(line=>
            {
                var parts = line.Split(';');
                return new BaseStrategy.Tic
                {
                    Time = date + TimeSpan.ParseExact(parts[0], "hhmmss", CultureInfo.InvariantCulture),
                    Price = double.Parse(parts[1], _commaNFI),
                    Volume = int.Parse(parts[2], CultureInfo.InvariantCulture)
                };
            }).ToList();

            var timeFrames = new List<int>();
            foreach (var file in Directory.GetFiles(_candlesDataFolder, @"*mins.txt"))
            {
                fileName = Path.GetFileName(file);
                var timeFrame = int.Parse(fileName.Substring(0, fileName.Length - 8));
                _candlesMap.Add(timeFrame, File.ReadAllLines(file).Select(line =>
                {
                    var parts = line.Split(';');
                    if (parts.Length == 6)
                    {
                        return new Candle
                        {
                            Time = DateTime.ParseExact(parts[0] + parts[1], "dd/MM/yyHHmmss", CultureInfo.InvariantCulture),
                            ClosePriceDiff = double.Parse(parts[3], _commaNFI),
                            ClosePrice = double.Parse(parts[4], _commaNFI),
                            Volume = int.Parse(parts[5])
                        };
                    }
                    else
                    {
                        return new Candle
                        {
                            Time = DateTime.ParseExact(parts[0] + parts[1], "dd/MM/yyHHmmss", CultureInfo.InvariantCulture),
                            ClosePriceDiff = double.Parse(parts[2], _commaNFI),
                            ClosePrice = double.Parse(parts[3], _commaNFI),
                            Volume = int.Parse(parts[4])
                        };
                    }
                }).ToList());
                _candleInProgressMap.Add(timeFrame, null);
                _currentCandlesFileNameMap.Add(timeFrame, file);
                _lastProcessedCandleTimeMap.Add(timeFrame, _candlesMap[timeFrame].Last().Time);
                Logger.LogDebug($"{_name}: Last Processed Candle Time for {timeFrame} mins : {_candlesMap[timeFrame].Last().Time}");
                _HLVMap.Add(timeFrame, new HLV { PrevClosePrice = double.MinValue});
                _strategiesMap.Add(timeFrame, new List<BaseStrategy>());
                timeFrames.Add(timeFrame);
            }
            _registeredTimeFrames = timeFrames.ToArray();
            _framesCount = _registeredTimeFrames.Length;
            var oldestTime = _lastProcessedCandleTimeMap.Min(t => t.Value);
            _lastProcessedTicTime = lastTics.Last().Time + TimeSpan.FromSeconds(1);
            Logger.LogDebug($"{_name}: Last Processed Tic Time: {_lastProcessedTicTime}");
            lastTics = lastTics.Where(t => t.Time >= oldestTime).ToList();
            lastTics.ForEach(t => ProcessTrade(new AllTrade { Price = t.Price, Datetime = (QuikDateTime)t.Time, Qty = t.Volume}));
        }
        public void RegisterStrategy(BaseStrategy strategy)
        {
            Strategies.Add(strategy);
            _strategiesMap[strategy.TimeFrame].Add(strategy);
            if (strategy.TimeFrame > _maxTimeFrame)
                _maxTimeFrame = strategy.TimeFrame;
            var processed = false;
            foreach (var candle in _candlesMap[strategy.TimeFrame].Where(c => c.Time > strategy.LastTimeCandle)) 
            {
                if (strategy.ProcessCandle(candle, false))
                    processed = true;
            }
            if (processed)
                strategy.SaveParams();
        }
        public void ProcessCandle(int frame, Candle c)
        {
            _candlesMap[frame].Add(c);
            _strategiesMap[frame].ForEach(s => s.ProcessCandle(c));
            _strategiesMap[frame].ForEach(s => s.Draw(c));
            var line = $"{c.Time.ToString("dd/MM/yy;HHmmss", CultureInfo.InvariantCulture)};{c.ClosePriceDiff.ToString(_commaNFI)};{c.ClosePrice.ToString(_commaNFI)};{c.Volume}{Environment.NewLine}";
            File.AppendAllText(_currentCandlesFileNameMap[frame], line, Encoding.ASCII);
        }

        public void ResetSecurity(List<AllTrade> trades)
        {
            Logger.LogDebug($"{nameof(ResetSecurity)} {_name}");
            while (Interlocked.CompareExchange(ref _allTradeFlag, 1, 0) != 0)
            { Thread.Sleep(100); }
            Logger.LogDebug("Start reseting");
            for (var i = 0; i < _framesCount; i++)
            {
                var timeFrame = _registeredTimeFrames[i];
                Logger.LogDebug($"{timeFrame}");
                var lastCandleTime = _lastProcessedCandleTimeMap[timeFrame];
                Logger.LogDebug($"{lastCandleTime}");
                _lastProcessedCandleTimeMap[timeFrame] = lastCandleTime - TimeSpan.FromMinutes(timeFrame);
                var hlv = _HLVMap[timeFrame];
                hlv.secCode = trades[0].SecCode;
                hlv.PrevClosePrice = double.MinValue;
            }
            trades.ForEach(t => ProcessTrade(t, false));
            Interlocked.Exchange(ref _allTradeFlag, 0);
        }
        public void ProcessTrade(AllTrade trade, bool locking = true)
        {
            if (locking)
                while (Interlocked.CompareExchange(ref _allTradeFlag, 1, 0) != 0)
                { Thread.Sleep(100); }
            var tradeTime = (DateTime)trade.Datetime;
            for (var i = 0; i < _framesCount; i++)
            {
                var timeFrame = _registeredTimeFrames[i];
                var lastCandleTime = _lastProcessedCandleTimeMap[timeFrame];
                if (tradeTime < lastCandleTime)
                    continue;
                var hlv = _HLVMap[timeFrame];
                if (!trade.SecCode.Equals(hlv.secCode))
                {
                    if (string.IsNullOrEmpty(hlv.secCode))
                    {
                        hlv.secCode = trade.SecCode;
                        Logger.LogDebug($"Set security Code. This should be on startup only {trade.SecCode}");
                    }
                    else
                    {
                        if (locking)
                            Interlocked.Exchange(ref _allTradeFlag, 0);
                        return;
                    }
                }
                if (Math.Abs(hlv.PrevClosePrice - double.MinValue) < Epsilon)
                {
                    if (tradeTime >= lastCandleTime + TimeSpan.FromMinutes(timeFrame))
                    {
                        hlv.PrevClosePrice = trade.Price;
                    }
                }
                else
                {
                    var candle = _candleInProgressMap[timeFrame];
                    if (candle != null)
                    {
						var time = candle.Time + TimeSpan.FromMinutes(timeFrame) - TimeSpan.FromMilliseconds(1);
                        var prevCandleTime = candle.Time;
                        if (tradeTime > time)
                        {
                            //Logger.LogDebug($"New candle {tradeTime} {time} {candle.Time}");
                            candle.ClosePriceDiff = candle.ClosePrice - hlv.PrevClosePrice;
                            hlv.PrevClosePrice = candle.ClosePrice;
                            ProcessCandle(timeFrame, candle);
                            _lastProcessedCandleTimeMap[timeFrame] = candle.Time;
                            candle = new Candle { Time = tradeTime - TimeSpan.FromSeconds(tradeTime.Second) - TimeSpan.FromMilliseconds(tradeTime.Millisecond)};
                            if (60 % timeFrame == 0)
                                candle.Time = candle.Time.AddMinutes(candle.Time.Minute % timeFrame * -1);
                            _candleInProgressMap[timeFrame] = candle;
                            if (_ticsBuffer.Count > 0)
                            {
                                ThreadPool.QueueUserWorkItem(delegate
                                {
                                    Thread.Sleep(2000);
                                    List<AllTrade> candleTrades = null;
                                    lock (_registeredTimeFrames)
                                    {
                                        _ticsBuffer.Sort((t1,t2) => t1.DateTime.CompareTo(t2.DateTime));
                                        candleTrades = _ticsBuffer.Where(t => t.DateTime <= time && t.DateTime.Date == lastCandleTime.Date).ToList();
                                        _ticsBuffer.RemoveAll(t => t.DateTime <= time);
                                    }
                                    var sb = new StringBuilder();
                                    foreach (var allTrade in candleTrades)
                                    {
                                        sb.AppendLine(string.Format(_commaNFI, "{0};{1};{2}",
                                            allTrade.DateTime.ToString("HHmmss",
                                            CultureInfo.InvariantCulture), allTrade.Price, allTrade.Qty));
                                    }
                                    lock (BaseStrategy.TicsLocker)
                                    {
                                        File.AppendAllText(_ticsDataFolder + Path.DirectorySeparatorChar +
                                            lastCandleTime.Date.ToString("yyyyMMdd") + BaseStrategy.TicsEndFileName, sb.ToString(),
                                            Encoding.ASCII);
                                    }
                                });
                            }
                        }
                    }
                    else
                    {
                        candle = new Candle { Time = tradeTime - TimeSpan.FromSeconds(tradeTime.Second) };
                        if (60 % timeFrame == 0)
                            candle.Time = candle.Time.AddMinutes(candle.Time.Minute % timeFrame * -1);
                        _candleInProgressMap[timeFrame] = candle;
                    }
                    candle.ClosePrice = trade.Price;
                    candle.Volume += (int)trade.Qty;
                }
            }
            lock (_registeredTimeFrames)
            {
                if (tradeTime > _lastProcessedTicTime)
                {
                    _lastProcessedTicTime = tradeTime.AddSeconds(-1);
                        _ticsBuffer.Add(trade);//Append(string.Format(_commaNFI, "{0};{1};{2}{3}",
                            //tradeTime.ToString("HHmmss", CultureInfo.InvariantCulture), trade.Price, trade.Qty, Environment.NewLine));
                    foreach (var strategy in Strategies)
                    {
                        strategy.OrderHandler.CheckOrders(trade);
                    }
                }

            }
            if (locking)
                Interlocked.Exchange(ref _allTradeFlag, 0);
        }
        public void Close()
        {
            Strategies.ForEach(s => s.Serialize());
        }
        private class HLV
        {
            public double PrevClosePrice;
            public string secCode = string.Empty;
        }
    }
}