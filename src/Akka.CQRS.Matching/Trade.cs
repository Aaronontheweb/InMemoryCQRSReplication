// -----------------------------------------------------------------------
// <copyright file="Trade.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.CQRS.Events;

namespace Akka.CQRS.Matching
{
    /// <summary>
    /// Represents an unfilled or partially unfilled trade inside the matching engine.
    /// </summary>
    public struct Trade : IWithTradeId, IWithStockId
    {
        /// <summary>
        /// Represents an empty or completed trade.
        /// </summary>
        public static readonly Trade Empty = new Trade(string.Empty, string.Empty, TradeSide.Buy, 0.0D, 0.0m, DateTimeOffset.MinValue);

        /// <summary>
        /// Used to validating that orders have been totally filled using floating-point precision.
        /// </summary>
        public const double Epsilon = 0.001d;

        public Trade(string tradeId, string stockId, TradeSide side, double originalQuantity, decimal price, DateTimeOffset timeIssued)
        : this(tradeId, stockId, side, originalQuantity, price, timeIssued, ImmutableList.Create<Fill>())
        {
        }

        public Trade(string tradeId, string stockId, TradeSide side, double originalQuantity, decimal price, DateTimeOffset timeIssued, IImmutableList<Fill> fills)
        {
            TradeId = tradeId;
            StockId = stockId;
            Side = side;
            OriginalQuantity = originalQuantity;
            Price = price;
            TimeIssued = timeIssued;
            Fills = fills;
        }

        public string TradeId { get; }
        public string StockId { get; }

        public TradeSide Side { get; }

        public double OriginalQuantity { get; }

        public double RemainingQuantity => OriginalQuantity - Fills.Sum(x => x.Quantity);

        public decimal Price { get; }

        public DateTimeOffset TimeIssued { get; }

        public bool Completed => Math.Abs(Fills.Sum(x => x.Quantity) - OriginalQuantity) < Epsilon;

        public IImmutableList<Fill> Fills { get; }

        public Trade WithFill(Fill fill)
        {
            // validate that the right fill event was sent to the right trade
            if (!fill.FilledId.Equals(TradeId))
            {
                throw new ArgumentException($"Expected fill for tradeId {TradeId}, but instead received one for {fill.FilledId}");
            }

            return new Trade(TradeId, StockId, Side, OriginalQuantity, Price, TimeIssued, Fills.Add(fill));
        }
    }
}