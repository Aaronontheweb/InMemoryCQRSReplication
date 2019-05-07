using System.Collections.Generic;
using System.Collections.Immutable;

namespace Akka.CQRS.Matching
{
    public sealed class MatchingEngine
    {
        public MatchingEngine(string stockId, Dictionary<string, Trade> bids, Dictionary<string, Trade> asks)
        {
            StockId = stockId;
            _bids = bids;
            _asks = asks;
        }

        /// <summary>
        /// The ticker symbol for the stock being matched
        /// </summary>
        public string StockId { get; }

        private readonly Dictionary<string, Trade> _bids;
        public IReadOnlyDictionary<string, Trade> BidTrades => _bids;

        private readonly Dictionary<string, Trade> _asks;
        public IReadOnlyDictionary<string, Trade> AskTrades => _asks;
    }
}
