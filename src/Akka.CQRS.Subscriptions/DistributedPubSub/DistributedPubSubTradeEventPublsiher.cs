using System;
using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;

namespace Akka.CQRS.Subscriptions.DistributedPubSub
{
    public sealed class DistributedPubSubTradeEventPublisher : ITradeEventPublisher
    {
        private readonly IActorRef _mediator;

        public DistributedPubSubTradeEventPublisher(IActorRef mediator)
        {
            _mediator = mediator;
        }

        public void Publish(string tickerSymbol, ITradeEvent @event)
        {
            _mediator.Tell(new Publish());
        }
    }

    /// <summary>
    /// Formats
    /// </summary>
    public static class DistributedPubSubTopicFormatter
    {
        public static string ToTopic(string tickerSymbol, TradeEventType tradeEventType)
        {
            return $"{tickerSymbol}-{nameof(tradeEventType)}";
        }
    }
}
