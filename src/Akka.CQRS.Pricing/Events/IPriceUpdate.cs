using System;

namespace Akka.CQRS.Pricing.Events
{
    /// <summary>
    /// Used to signal a change in price for a specific stock.
    /// </summary>
    public interface IPriceUpdate : IWithStockId, IComparable<IPriceUpdate>
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
}
