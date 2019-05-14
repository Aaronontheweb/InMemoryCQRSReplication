// -----------------------------------------------------------------------
// <copyright file="PriceChanged.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Akka.CQRS.Pricing.Events
{
    /// <summary>
    /// Concrete <see cref="IPriceUpdate"/> implementation.
    /// </summary>
    public sealed class PriceChanged : IPriceUpdate, IComparable<PriceChanged>
    {
        public PriceChanged(string stockId, decimal currentAvgPrice, DateTimeOffset timestamp)
        {
            StockId = stockId;
            CurrentAvgPrice = currentAvgPrice;
            Timestamp = timestamp;
        }

        public DateTimeOffset Timestamp { get; }

        public decimal CurrentAvgPrice { get; }

        public string StockId { get; }

        public int CompareTo(PriceChanged other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return Timestamp.CompareTo(other.Timestamp);
        }

        public int CompareTo(IPriceUpdate other)
        {
            if (other is PriceChanged c)
            {
                return CompareTo(c);
            }
            throw new ArgumentException();
        }
    }
}