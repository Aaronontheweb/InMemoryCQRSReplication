using System;

namespace Akka.CQRS.Events
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a "sell"-side event
    /// </summary>
    public sealed class Ask : IWithStockId
    {
        public Ask(string stockId, decimal askPrice, 
            double askQuantity, DateTimeOffset timeIssued)
        {
            StockId = stockId;
            AskPrice = askPrice;
            AskQuantity = askQuantity;
            TimeIssued = timeIssued;
        }

        public string StockId { get; }

        public decimal AskPrice { get; }

        public double AskQuantity { get; }

        public DateTimeOffset TimeIssued { get; }
    }
}