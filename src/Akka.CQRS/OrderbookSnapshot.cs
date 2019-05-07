using System;
using System.Collections.Generic;
using System.Text;
using Akka.CQRS.Events;

namespace Akka.CQRS
{
    /// <summary>
    /// Indicates which side of the trade this transaction occurred on.
    /// </summary>
    public enum TradeSide
    {
        Buy,
        Sell
    }

    /// <summary>
    /// The full state of the current order book for a given <see cref="IWithStockId"/>.
    /// </summary>
    public sealed class OrderbookSnapshot : IWithStockId
    {
        public OrderbookSnapshot(string stockId, DateTimeOffset timestamp, double askQuantity, double bidQuantity, IReadOnlyCollection<Ask> asks, IReadOnlyCollection<Bid> bids)
        {
            StockId = stockId;
            Timestamp = timestamp;
            AskQuantity = askQuantity;
            BidQuantity = bidQuantity;
            Asks = asks;
            Bids = bids;
        }

        public string StockId { get; }

        public DateTimeOffset Timestamp { get; }

        public double AskQuantity { get; }

        public double BidQuantity { get; }

        public IReadOnlyCollection<Ask> Asks { get; }

        public IReadOnlyCollection<Bid> Bids { get; }
    }
}
