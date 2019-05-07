using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Events
{
    /// <summary>
    /// Fill an open order
    /// </summary>
    public sealed class Fill
    {
        public Fill(string filledId, double quantity, decimal price, TradeSide side, string filledById, DateTimeOffset timestamp)
        {
            FilledId = filledId;
            Quantity = quantity;
            Price = price;
            Side = side;
            FilledById = filledById;
            Timestamp = timestamp;
        }

        public string FilledId { get; }

        public double Quantity { get; }

        public decimal Price { get; }

        public TradeSide Side { get; }

        public string FilledById { get; }

        public DateTimeOffset Timestamp { get; }
    }
}
