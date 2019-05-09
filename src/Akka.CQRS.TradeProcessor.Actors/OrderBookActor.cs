using System;
using Akka.CQRS.Matching;
using Akka.CQRS.Subscriptions;
using Akka.Persistence;

namespace Akka.CQRS.TradeProcessor.Actors
{
    /// <summary>
    /// Actor responsible for processing orders for a specific ticker symbol.
    /// </summary>
    public class OrderBookActor : ReceivePersistentActor
    {
        private MatchingEngine _matchingEngine;
        private readonly ITradeEventPublisher _publisher;

        public OrderBookActor(string tickerSymbol, MatchingEngine matchingEngine, ITradeEventPublisher publisher)
        {
            TickerSymbol = tickerSymbol;
            _matchingEngine = matchingEngine;
            _publisher = publisher;

            Recovers();
            Commands();
        }

        public string TickerSymbol { get; }
        public override string PersistenceId => TickerSymbol;

        private void Recovers()
        {
            
        }

        private void Commands()
        {
            
        }
    }
}
