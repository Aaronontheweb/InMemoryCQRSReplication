using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Events
{
    /// <summary>
    /// Matches a buy / sell-side order
    /// </summary>
    public sealed class Match : IWithStockId
    {
        public Match(string stockId, decimal settlementPrice, double quantity, DateTimeOffset timeStamp)
        {
            StockId = stockId;
            SettlementPrice = settlementPrice;
            Quantity = quantity;
            TimeStamp = timeStamp;
        }

        public string StockId { get; }

        public decimal SettlementPrice { get; }

        public double Quantity { get; }

        public DateTimeOffset TimeStamp { get; }
    }
}
