using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Events
{
    /// <summary>
    /// Fill an open order
    /// </summary>
    public sealed class Fill : IWithOrderId
    {
        public Fill(string orderId, double quantity, decimal price, 
            string filledById, DateTimeOffset timestamp, bool partialFill = false)
        {
            OrderId = orderId;
            Quantity = quantity;
            Price = price;
            FilledById = filledById;
            Timestamp = timestamp;
            Partial = partialFill;
        }

        public string OrderId { get; }

        public double Quantity { get; }

        public decimal Price { get; }

        public string FilledById { get; }

        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// When <c>true</c>, indicates that the order was only partially filled.
        /// </summary>
        public bool Partial { get; }
    }
}
