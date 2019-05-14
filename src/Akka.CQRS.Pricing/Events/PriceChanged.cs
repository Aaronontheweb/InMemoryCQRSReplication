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
    public sealed class PriceChanged : IPriceUpdate, IComparable<PriceChanged>, IComparable
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

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is PriceChanged other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(PriceChanged)}");
        }

        public int CompareTo(IPriceUpdate other)
        {
            throw new NotImplementedException();
        }

        public static bool operator <(PriceChanged left, PriceChanged right)
        {
            return Comparer<PriceChanged>.Default.Compare(left, right) < 0;
        }

        public static bool operator >(PriceChanged left, PriceChanged right)
        {
            return Comparer<PriceChanged>.Default.Compare(left, right) > 0;
        }

        public static bool operator <=(PriceChanged left, PriceChanged right)
        {
            return Comparer<PriceChanged>.Default.Compare(left, right) <= 0;
        }

        public static bool operator >=(PriceChanged left, PriceChanged right)
        {
            return Comparer<PriceChanged>.Default.Compare(left, right) >= 0;
        }
    }
}