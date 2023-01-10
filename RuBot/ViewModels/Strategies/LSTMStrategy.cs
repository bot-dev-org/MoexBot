using RuBot.Models;
using RuBot.Models.Indicators;
using System;
using System.Collections.Generic;

namespace RuBot.ViewModels.Strategies
{
    public class LSTMStrategy : BaseStrategy
    {
        private readonly LstmClient client;
        private readonly string ticker;
        private double skip_coeff;
        private int lastValue = 0;

        public LSTMStrategy(string ticker, int timeframe, double skip_coeff, LstmClient _client, string ticksDataFolder)
            : base($"{ticker} {timeframe}")
        {
            TimeFrame = timeframe;
            this.ticker = ticker;
            this.skip_coeff = skip_coeff;
            TicsDataFolder = ticksDataFolder;
            client = _client;
            LastTimeCandle = client.GetLastProcessedTime(ticker, timeframe, skip_coeff);
            Parameters.SkipValue = skip_coeff;
            InitialVolume = client.GetVolume(ticker, timeframe, skip_coeff);
            if (InitialVolume == 0)
                InitialVolume = 1;
            PartVolume = InitialVolume * 2;
            SettingsFile = Name + ".bin";
            LastValue = client.GetLastValue(ticker, timeframe, skip_coeff);
            Deserialize();
        }

        protected override Dictionary<DateTime, int> GetDeals()
        {
            return client.GetDeals(ticker, TimeFrame, Parameters.SkipValue);
        }

        public override bool ProcessCandle(Candle candle, bool saveParams = true)
        {
            candle = PreProcessCandle(candle);
            if (candle == null || candle.Time < LastTimeCandle)
                return false;
            base.ProcessCandle(candle);
            var prevState = lastValue;
            LastValue = client.Predict(candle.ClosePriceDiff, candle.ClosePrice, candle.Time, candle.Volume, ticker, TimeFrame, Parameters.SkipValue, saveParams);

            if (prevState != lastValue)
                MakeDeal(candle);
            return true;
        }

        public override void SaveParams()
        {
            client.Save(ticker, TimeFrame, Parameters.SkipValue);
        }

        public override int InitialVolume
        {
            get => _volume;
            set
            {
                _volume = value; 
                client.SetVolume(value, ticker, TimeFrame, skip_coeff);
                RaisePropertyChanged("InitialVolume");
            }
        }

        public override int LastValue { get => lastValue; set { 
                lastValue = value;
                RaisePropertyChanged("LastValue");
            } }
    }
}

