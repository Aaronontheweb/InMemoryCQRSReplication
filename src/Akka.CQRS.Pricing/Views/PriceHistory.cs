using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.CQRS.Pricing.Events;

namespace Akka.CQRS.Pricing.Views
{
    /// <summary>
    /// Details the price history for a specific ticker symbol.
    ///
    /// In-memory, replicated view.
    /// </summary>
    public struct PriceHistory : IWithStockId
    {
        public PriceHistory(string stockId, ImmutableSortedSet<IPriceUpdate> historicalPrices)
        {
            HistoricalPrices = historicalPrices;
            StockId = stockId;
        }

        public string StockId { get; }

        public DateTimeOffset From => HistoricalPrices[0].Timestamp;

        public DateTimeOffset Until => HistoricalPrices.Last().Timestamp;

        public decimal CurrentPrice => HistoricalPrices.Last().CurrentAvgPrice;

        public TimeSpan Range => Until - From;

        public ImmutableSortedSet<IPriceUpdate> HistoricalPrices { get; }

        public PriceHistory WithPrice(IPriceUpdate update)
        {
            if(!update.StockId.Equals(StockId))
                throw new ArgumentOutOfRangeException($"Expected ticker symbol {StockId} but found {update.StockId}", nameof(update));

            return new PriceHistory(StockId, HistoricalPrices.Add(update));
        }

        /// <summary>
        /// Purge older price entries - resetting the window for a new trading day.
        /// </summary>
        /// <param name="earliestStart">Delete any entries older than this.</param>
        /// <returns>An updated <see cref="PriceHistory"/>.</returns>
        public PriceHistory Prune(DateTimeOffset earliestStart)
        {
            return new PriceHistory(StockId, HistoricalPrices.Where(x => x.Timestamp < earliestStart).ToImmutableSortedSet());
        }
    }
}
