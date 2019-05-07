using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Events
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a "buy"-side event
    /// </summary>
    public sealed class Bid : IWithStockId, IWithOrderId
    {
        public Bid(string stockId, string tradeId, decimal bidPrice, 
            double bidQuantity, DateTimeOffset timeIssued)
        {
            StockId = stockId;
            BidPrice = bidPrice;
            BidQuantity = bidQuantity;
            TimeIssued = timeIssued;
            OrderId = tradeId;
        }

        public string StockId { get; }

        public decimal BidPrice { get; }

        public double BidQuantity { get; }

        public DateTimeOffset TimeIssued { get; }
        public string OrderId { get; }
    }
}
