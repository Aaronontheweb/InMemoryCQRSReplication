using System;
using System.Collections.Generic;
using System.Text;

namespace Akka.CQRS.Pricing.Commands
{
    /// <summary>
    /// Fetch the N most recent price and volume updates for a specific ticker symbol.
    /// </summary>
    public sealed class FetchPriceAndVolume : IWithStockId
    {
        public FetchPriceAndVolume(string stockId)
        {
            StockId = stockId;
        }

        public string StockId { get; }
    }

    public sealed class GetCurrentPrice : IWithStockId
    {
        public string StockId { get; }
    }
}
