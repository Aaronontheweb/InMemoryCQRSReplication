// -----------------------------------------------------------------------
// <copyright file="Order.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.CQRS.Events;

namespace Akka.CQRS
{
    /// <summary>
    /// Represents an unfilled or partially unfilled trade inside the matching engine.
    /// </summary>
    public struct Order : IWithOrderId, IWithStockId, IEquatable<Order>, IComparable<Order>, IComparable
    {
        /// <summary>
        /// Represents an empty or completed trade.
        /// </summary>
        public static readonly Order Empty = new Order(string.Empty, string.Empty, TradeSide.Buy, 0.0D, 0.0m, DateTimeOffset.MinValue);

        /// <summary>
        /// Used to validating that orders have been totally filled using floating-point precision.
        /// </summary>
        public const double Epsilon = 0.001d;

        public Order(string tradeId, string stockId, TradeSide side, double originalQuantity, decimal price, DateTimeOffset timeIssued)
        : this(tradeId, stockId, side, originalQuantity, price, timeIssued, ImmutableList.Create<Fill>())
        {
        }

        public Order(string tradeId, string stockId, TradeSide side, double originalQuantity, decimal price, DateTimeOffset timeIssued, IImmutableList<Fill> fills)
        {
            OrderId = tradeId;
            StockId = stockId;
            Side = side;
            OriginalQuantity = originalQuantity;
            Price = price;
            TimeIssued = timeIssued;
            Fills = fills;
        }

        public string OrderId { get; }
        public string StockId { get; }

        public TradeSide Side { get; }

        public double OriginalQuantity { get; }

        public double RemainingQuantity => OriginalQuantity - Fills.Sum(x => x.Quantity);

        public decimal Price { get; }

        public DateTimeOffset TimeIssued { get; }

        public bool Completed => Math.Abs(Fills.Sum(x => x.Quantity) - OriginalQuantity) < Epsilon;

        public IImmutableList<Fill> Fills { get; }

        public Order WithFill(Fill fill)
        {
            // validate that the right fill event was sent to the right trade
            if (!fill.OrderId.Equals(OrderId))
            {
                throw new ArgumentException($"Expected fill for tradeId {OrderId}, but instead received one for {fill.OrderId}");
            }

            return new Order(OrderId, StockId, Side, OriginalQuantity, Price, TimeIssued, Fills.Add(fill));
        }

        public bool Match(Order opposite)
        {
            if(Side == opposite.Side)
                throw new InvalidOperationException($"Can't match order {OrderId} with {opposite.OrderId} for {StockId} - both trades are on the [{Side}] side!");

            switch (Side)
            {
                case TradeSide.Buy:
                    return opposite.Price <= Price;
                case TradeSide.Sell:
                    return opposite.Price >= Price;
                default:
                    throw new ArgumentException($"Unrecognized TradeSide option [{Side}]");
            }
        }

        public bool Equals(Order other)
        {
            return string.Equals(OrderId, other.OrderId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Order other && Equals(other);
        }

        public override int GetHashCode()
        {
            return OrderId.GetHashCode();
        }

        public static bool operator ==(Order left, Order right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Order left, Order right)
        {
            return !left.Equals(right);
        }

        public int CompareTo(Order other)
        {
            return Price.CompareTo(other.Price);
        }

        public int CompareTo(object obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is Order other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(Order)}");
        }

        public static bool operator <(Order left, Order right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >(Order left, Order right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <=(Order left, Order right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static bool operator >=(Order left, Order right)
        {
            return left.CompareTo(right) >= 0;
        }
    }
}