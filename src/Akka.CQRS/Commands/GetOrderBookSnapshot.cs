using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Commands
{
    /// <summary>
    /// Query the current order book snapshot
    /// </summary>
    public class GetOrderBookSnapshot : IWithStockId
    {
        public GetOrderBookSnapshot(string stockId)
        {
            StockId = stockId;
        }

        public string StockId { get; }
    }
}
