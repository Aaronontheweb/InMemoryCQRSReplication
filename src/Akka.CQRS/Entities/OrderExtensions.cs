// -----------------------------------------------------------------------
// <copyright file="OrderExtensions.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using Akka.CQRS.Events;

namespace Akka.CQRS
{
    /// <summary>
    /// Sorts open orders by their price.
    /// </summary>
    public sealed class OrderPriceComparer : IComparer<Order>
    {
        public static readonly OrderPriceComparer Instance = new OrderPriceComparer();

        private OrderPriceComparer() { }

        public int Compare(Order x, Order y)
        {
            return x.Price.CompareTo(y.Price);
        }
    }

    /// <summary>
    /// Extension methods for working with <see cref="Bid"/>, <see cref="Ask"/>, and <see cref="Order"/>.
    /// </summary>
    public static class OrderExtensions
    {
        public static Order ToOrder(this Bid bid)
        {
            return new Order(bid.OrderId, bid.StockId, TradeSide.Buy, bid.BidQuantity, bid.BidPrice, bid.TimeIssued);
        }

        public static Order ToOrder(this Ask ask)
        {
            return new Order(ask.OrderId, ask.StockId, TradeSide.Sell, ask.AskQuantity, ask.AskPrice, ask.TimeIssued);
        }
    }
}