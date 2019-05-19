// -----------------------------------------------------------------------
// <copyright file="GetPriceHistory.cs" company="Petabridge, LLC">
//      Copyright (C) 2015 - 2019 Petabridge, LLC <https://petabridge.com>
// </copyright>
// -----------------------------------------------------------------------

using Akka.CQRS.Pricing.Views;

namespace Akka.CQRS.Pricing.Commands
{
    /// <summary>
    /// Fetch an entire <see cref="PriceHistory"/> for a specific stock.
    /// </summary>
    public sealed class GetPriceHistory : IWithStockId
    {
        public GetPriceHistory(string stockId)
        {
            StockId = stockId;
        }

        public string StockId { get; }
    }

    /// <summary>
    /// Fetch the latest price only for a specific stock.
    /// </summary>
    public sealed class GetLatestPrice : IWithStockId
    {
        public GetLatestPrice(string stockId)
        {
            StockId = stockId;
        }

        public string StockId { get; }
    }
}