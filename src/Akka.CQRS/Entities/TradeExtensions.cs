// -----------------------------------------------------------------------
// <copyright file="TradeExtensions.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.CQRS.Events;

namespace Akka.CQRS
{
    /// <summary>
    /// Extension methods for working with <see cref="Bid"/>, <see cref="Ask"/>, and <see cref="Order"/>.
    /// </summary>
    public static class TradeExtensions
    {
        public static Order ToTrade(this Bid bid)
        {
            return new Order(bid.OrderId, bid.StockId, TradeSide.Buy, bid.BidQuantity, bid.BidPrice, bid.TimeIssued);
        }

        public static Order ToTrade(this Ask ask)
        {
            return new Order(ask.OrderId, ask.StockId, TradeSide.Sell, ask.AskQuantity, ask.AskPrice, ask.TimeIssued);
        }
    }
}