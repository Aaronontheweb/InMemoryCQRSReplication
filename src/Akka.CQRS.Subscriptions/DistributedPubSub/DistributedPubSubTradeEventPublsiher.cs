using System.Collections.Generic;
using System.Text;
using Akka.Actor;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Util;
using static Akka.CQRS.Subscriptions.DistributedPubSub.DistributedPubSubTopicFormatter;

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
            var eventType = @event.ToTradeEventType();
            var topic = ToTopic(tickerSymbol, eventType);
            _mediator.Tell(new Publish(topic, @event));
        }
    }
}
