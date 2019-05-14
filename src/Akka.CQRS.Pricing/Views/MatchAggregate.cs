using System;
using Akka.CQRS.Events;
using Akka.CQRS.Pricing.Events;
using Akka.CQRS.Util;

namespace Akka.CQRS.Pricing.Views
{
    /// <summary>
    /// Aggregates <see cref="Akka.CQRS.Events.Match"/> trade events in order to produce price and volume estimates.
    /// </summary>
    public sealed class MatchAggregate
    {
        /// <summary>
        /// By default, average all prices and volume over the past 30 matched trades.
        /// </summary>
        public const int DefaultSampleSize = 30;

        public MatchAggregate(string tickerSymbol, decimal initialPrice = 0.0m, 
            double initialVolume = 0.0d, int sampleSize = DefaultSampleSize)
        {
            TickerSymbol = tickerSymbol;
            AvgPrice = EMWAm.Init(sampleSize, initialPrice);
            AvgVolume = EMWA.Init(sampleSize, initialVolume);
        }

        public string TickerSymbol { get; }

        public EMWA AvgVolume { get; private set; }

        public EMWAm AvgPrice { get; private set; }

        /// <summary>
        /// Fetch the current price and volume metrics.
        ///
        /// We don't do this on every single match since that could become noisy quickly.
        /// Instead we do it on a regular clock interval.
        /// </summary>
        /// <param name="timestampService">Optional - the service used for time-stamping the price and volume updates.</param>
        /// <returns>The current price and volume update events.</returns>
        (IPriceUpdate lastestPrice, IVolumeUpdate latestVolume) FetchMetrics(ITimestamper timestampService = null)
        {
            var currentTime = timestampService?.Now ?? CurrentUtcTimestamper.Instance.Now;
            return (new PriceChanged(TickerSymbol, AvgPrice.CurrentAvg, currentTime),
                new VolumeChanged(TickerSymbol, AvgVolume.CurrentAvg, currentTime));
        }

        /// <summary>
        /// Feed the most recent match for <see cref="TickerSymbol"/> to update moving price averages.
        /// </summary>
        /// <param name="latestTrade">The most recent matched trade for this symbol.</param>
        public bool WithMatch(Match latestTrade)
        {
            if (!latestTrade.StockId.Equals(TickerSymbol))
                return false; // Someone fed a match for a stock other than TickerSymbol

            // Update EMWA quantity and volume
            AvgVolume += latestTrade.Quantity;
            AvgPrice += latestTrade.SettlementPrice;
            return true;
        }
    }

    
}
