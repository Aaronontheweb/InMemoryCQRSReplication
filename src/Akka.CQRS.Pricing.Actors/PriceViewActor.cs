using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;

namespace Akka.CQRS.Pricing.Actors
{
    /// <summary>
    /// In-memory, replicated view of the current price and volume for a specific stock.
    /// </summary>
    public sealed class PriceVolumeViewActor : ReceiveActor, IWithStockId
    {
        private readonly string _tickerSymbol;

        // the Cluster.Sharding proxy
        private readonly IActorRef _priceActorGateway;

        // the DistributedPubSub mediator
        private readonly IActorRef _mediator;

        private IActorRef _tickerEntity;

        public PriceVolumeViewActor(string tickerSymbol, IActorRef priceActorGateway, IActorRef mediator)
        {
            _tickerSymbol = tickerSymbol;
            _priceActorGateway = priceActorGateway;
            _mediator = mediator;
        }
    }
}
