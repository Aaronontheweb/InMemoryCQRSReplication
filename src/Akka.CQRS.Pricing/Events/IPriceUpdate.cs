using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Pricing.Events
{
    /// <summary>
    /// Used to signal a change in price for a specific stock.
    /// </summary>
    public interface IPriceUpdate : IWithStockId
    {
        /// <summary>
        /// The time of this price update.
        /// </summary>
        DateTimeOffset Timestamp { get; }

        /// <summary>
        /// The current volume-weighted average price.
        /// </summary>
        decimal CurrentAvgPrice { get; }
    }

    /// <summary>
    /// Concrete 
    /// </summary>
    public sealed class PriceChanged : IPriceUpdate
    {
        public DateTimeOffset Timestamp { get; }

        public decimal CurrentAvgPrice { get; }

        public string StockId { get; }
    }
}
