using System;
using Akka.Actor;
using Akka.Persistence;

namespace Akka.CQRS.Pricing.Actors
{
    /// <summary>
    /// Used to aggregate <see cref="Akka.CQRS.Events.Match"/> events via Akka.Persistence.Query
    /// </summary>
    public class MatchAggregator : ReceivePersistentActor
    {
        public MatchAggregator(string tickerSymbol)
        {
            TickerSymbol = tickerSymbol;
            PersistenceId = $"{tickerSymbol}-Price";
            
        }

        public readonly string TickerSymbol;
        public override string PersistenceId { get; }

        
    }
}
