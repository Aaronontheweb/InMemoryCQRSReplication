using System;
using Akka.CQRS.Matching;
using Akka.Persistence;

namespace Akka.CQRS.TradeProcessor.Actors
{
    /// <summary>
    /// Actor responsible for processing orders for a specific ticker symbol.
    /// </summary>
    public class OrderBookActor : ReceivePersistentActor
    {
        private MatchingEngine _matchingEngine;

        public OrderBookActor(string tickerSymbol)
        {
            TickerSymbol = tickerSymbol;
        }

        public string TickerSymbol { get; }
        public override string PersistenceId => TickerSymbol;
    }
}
