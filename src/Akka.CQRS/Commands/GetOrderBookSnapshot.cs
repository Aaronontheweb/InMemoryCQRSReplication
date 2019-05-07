using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Commands
{
    /// <summary>
    /// Specifics the level of detail for an OrderBook snapshot.
    /// </summary>
    public enum DetailLevel
    {
        /// <summary>
        /// Lists all of the details
        /// </summary>
        Full,

        /// <summary>
        /// Lists only the aggregates
        /// </summary>
        Summary
    }

    /// <summary>
    /// Query the current order book snapshot
    /// </summary>
    public class GetOrderBookSnapshot : IWithStockId
    {
        public GetOrderBookSnapshot(string stockId, DetailLevel detail = DetailLevel.Summary)
        {
            StockId = stockId;
            Detail = detail;
        }

        public string StockId { get; }

        public DetailLevel Detail { get; }
    }
}
