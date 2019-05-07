using System;
using System.Collections.Generic;
using System.Text;
using Akka.CQRS.Events;

namespace Akka.CQRS
{
    /// <summary>
    /// The full state of the current order book for a given <see cref="IWithStockId"/>.
    /// </summary>
    public class OrderbookSnapshot : IWithStockId
    {
        public string StockId { get; }

        public DateTimeOffset Timestamp { get; }

        public double AskQuantity { get; }

        public double BidQuantity { get; }

        public IReadOnlyCollection<Ask> Asks { get; }

        public IReadOnlyCollection<Bid> Bids { get; }
    }

    /// <summary>
    /// Represents a price band, typically weighted by buy/sell volume.
    /// </summary>
    public struct PriceRange
    {
        public PriceRange(decimal min, decimal mean, decimal max)
        {
            Min = min;
            Mean = mean;
            Max = max;
        }

        public decimal Min { get; }

        public decimal Mean { get; }

        public decimal Max { get; }
    }
}
