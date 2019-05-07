using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS
{
    /// <summary>
    /// Marker interface used for routing messages for specific stock IDs
    /// </summary>
    public interface IWithStockId
    {
        /// <summary>
        /// The ticker symbol for a specific stock.
        /// </summary>
        string StockId { get; }
    }
}
