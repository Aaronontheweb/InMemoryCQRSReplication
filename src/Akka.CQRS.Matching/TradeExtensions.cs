// -----------------------------------------------------------------------
// <copyright file="TradeExtensions.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.CQRS.Events;

namespace Akka.CQRS.Matching
{
    /// <summary>
    /// Extension methods for working with <see cref="Bid"/>, <see cref="Ask"/>, and <see cref="Trade"/>.
    /// </summary>
    public static class TradeExtensions
    {
        public static Trade ToTrade(this Bid bid)
        {
            return new Trade(bid.TradeId, bid.StockId, TradeSide.Buy, bid.BidQuantity, bid.BidPrice, bid.TimeIssued);
        }

        public static Trade ToTrade(this Ask ask)
        {
            return new Trade(ask.TradeId, ask.StockId, TradeSide.Sell, ask.AskQuantity, ask.AskPrice, ask.TimeIssued);
        }
    }
}