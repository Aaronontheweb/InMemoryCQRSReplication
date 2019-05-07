using System.Collections.Generic;
using System.Collections.Immutable;

namespace Akka.CQRS.Matching
{
    public sealed class MatchingEngine
    {
        public MatchingEngine(string stockId, Dictionary<string, Order> bids, Dictionary<string, Order> asks)
        {
            StockId = stockId;
            _bids = bids;
            _asks = asks;
        }

        /// <summary>
        /// The ticker symbol for the stock being matched
        /// </summary>
        public string StockId { get; }

        private readonly Dictionary<string, Order> _bids;
        public IReadOnlyDictionary<string, Order> BidTrades => _bids;

        private readonly Dictionary<string, Order> _asks;
        public IReadOnlyDictionary<string, Order> AskTrades => _asks;
    }
}
