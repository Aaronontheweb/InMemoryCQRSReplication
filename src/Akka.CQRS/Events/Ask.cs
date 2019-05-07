using System;

namespace Akka.CQRS.Events
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a "sell"-side event
    /// </summary>
    public sealed class Ask : IWithStockId, IWithTradeId
    {
        public Ask(string stockId, string tradeId, decimal askPrice, 
            double askQuantity, DateTimeOffset timeIssued)
        {
            StockId = stockId;
            AskPrice = askPrice;
            AskQuantity = askQuantity;
            TimeIssued = timeIssued;
            TradeId = tradeId;
        }

        public string StockId { get; }

        public decimal AskPrice { get; }

        public double AskQuantity { get; }

        public DateTimeOffset TimeIssued { get; }
        public string TradeId { get; }
    }
}