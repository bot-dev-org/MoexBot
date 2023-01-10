using System;

namespace RuBot.Models.Parameters
{
    public class StrategyParameters
    {
        public double QuoteProfit;
        public double SkipValue = 0.0;
        public double RealCurrentCash;
        public DateTime RealLastMaxTime = DateTime.MinValue;
        public double RealMaxLastDrawDown;
        public double RealMaxProfit;
        public double DeltaPrice;
        public int PartVolume;
        public int QuotePeriod = 60;
        public double Commission = 0.0005;
        public int TimeFrame;

        public virtual void CopyTo(ref StrategyParameters parameters)
        {
            parameters.DeltaPrice = DeltaPrice;
            parameters.QuotePeriod = QuotePeriod;
            parameters.RealCurrentCash = RealCurrentCash;
            parameters.RealLastMaxTime = RealLastMaxTime;
            parameters.RealMaxLastDrawDown = RealMaxLastDrawDown;
            parameters.RealMaxProfit = RealMaxProfit;
            parameters.Commission = Commission;
            parameters.PartVolume = PartVolume;
            parameters.QuoteProfit = QuoteProfit;
            parameters.TimeFrame = TimeFrame;
            parameters.SkipValue = SkipValue;
        }
    }
}